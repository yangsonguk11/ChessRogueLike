using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class CardCanvas : MonoBehaviour
{
    public static CardCanvas instance;

    [SerializeField] CardDatabase cardData;
    [SerializeField] Board board;
    [SerializeField] GameObject UnSeenEvent; // 이벤트 레벨에서 숨길 카드 관련 UI를 모아둔 부모
    public List<RectTransform> cards = new List<RectTransform>(); // 손에 든 카드들
    public List<RectTransform> Discardcards = new List<RectTransform>();
    public Queue<RectTransform> Deckcards = new Queue<RectTransform>();
    [SerializeField] GameObject HandZone;
    [SerializeField] RectTransform CardNowUsingPos;
    [SerializeField] RectTransform DiscardZone;
    [SerializeField] RectTransform DeckZone;
    [SerializeField] RectTransform ExileZone;
    public List<RectTransform> Exilecards = new List<RectTransform>();
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius;        // 부채꼴 반지름 (클수록 더 펼쳐짐)
    [SerializeField] float angleBetween;  // 카드 사이의 각도
    [SerializeField] float heightOffset;  // 부채꼴의 수직 위치 보정

    // ── 카드 선택 패널 ──────────────────────────────────────────
    // Unity Inspector에서 연결 필요:
    //   cardSelectionPanel  : 패널 최상위 GameObject (기본 비활성화)
    //   cardSelectionContent: 카드가 표시될 RectTransform (Content 영역)
    //   selectionCountText  : "0/2 선택됨" 표시 TextMeshProUGUI
    //   selectionPromptText : 안내 문구 TextMeshProUGUI
    //   confirmSelectionBtn : 확인 Button
    [Header("카드 선택 패널")]
    [SerializeField] GameObject cardSelectionPanel;
    [SerializeField] RectTransform cardSelectionContent;
    [SerializeField] TextMeshProUGUI selectionCountText;
    [SerializeField] TextMeshProUGUI selectionPromptText;
    [SerializeField] UnityEngine.UI.Button confirmSelectionBtn;

    public static event Action OnPileChanged;
    void NotifyPileChanged() => OnPileChanged?.Invoke();

    public static bool cardSelectionMode = false;
    List<RectTransform> panelCardPool = new List<RectTransform>();
    HashSet<RectTransform> selectedInPanel = new HashSet<RectTransform>();
    int panelRequiredCount;
    Action<List<RectTransform>> panelCallback;
    Dictionary<RectTransform, (Transform parent, Vector3 worldPos)> savedCardStates
        = new Dictionary<RectTransform, (Transform, Vector3)>();
    // ────────────────────────────────────────────────────────────

    int _currentenergy;
    public int currentenergy { get { return _currentenergy; } set { _currentenergy = value; UpdateCurrentEnergy(); UpdateCardInteractability(); } }
    public int maxenergy = 4;
    public RectTransform nowusingCard;
    public bool isCardEffecting;
    bool usingCardMoving;
    List<RectTransform> pendingDrawCards = new List<RectTransform>();
    Coroutine batchDrawCoroutine;
    Vector2 pendingFirstTarget = new Vector2(-1, -1);

    // ── 카드 이동 애니메이션 큐 ──────────────────────────────────
    // 큐는 0.15초마다 다음 이동을 하나씩 꺼내 시작시킨다. 각 이동은 시작되면
    // 독립적으로 재생되며(자기 duration만큼 걸림), 큐가 빼는 속도와는 무관하다.
    struct CardMoveRequest
    {
        public RectTransform card;
        public Vector3 pos;
        public Quaternion rot;
        public float duration;
        public Action onComplete;
    }
    const float MoveQueueGap = 0.15f;
    Queue<CardMoveRequest> moveQueue = new Queue<CardMoveRequest>();
    Coroutine moveQueueRoutine;
    Dictionary<RectTransform, Coroutine> activeMoves = new Dictionary<RectTransform, Coroutine>();
    // ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (instance == null) instance = this;
        currentenergy = 3;
        foreach(string cardName in DataManager.Instance.currentData.deckCardIDs)
        {
            GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardName);
            if(obj != null)
            {
                var rt = obj.GetComponent<RectTransform>();
                Discardcards.Add(rt);
                rt.position = GetZonePosition(CardPositionZone.Deck);
            }
        }
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }

    public void CardSelected(int handNum)   
    {
        if (handNum == -1)
            return;
        cards[handNum].GetComponent<Card>().SelectedTrue();
        ExcludeAlignCards(cards[handNum].GetComponent<Card>().handNumber);
        HandZone.GetComponent<Image>().raycastTarget = true;
    }

    public void CardUnSelected()           
    {
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }
    public bool UseCard(int handnum)
    {
        Card card = cards[handnum].GetComponent<Card>();
        // isCardEffecting이면서 nowusingCard가 없으면 보드가 실제 효과 처리 중 → 차단
        // nowusingCard가 있으면 아직 애니메이션/대기 중이므로 교체 허용
        if (card.Cost > currentenergy)
        {
            AnnouncementUI.instance?.Show("코스트가 부족합니다");
            return false;
        }
        if ((isCardEffecting && nowusingCard == null) || TurnManager.instance.currentState != TurnState.Player)
            return false;
        if (!card.CanUse())
        {
            AnnouncementUI.instance?.Show(card.GetCannotUseReason());
            return false;
        }
        if (nowusingCard != null)
            CancelCardMove(nowusingCard);
        pendingFirstTarget = new Vector2(-1, -1);
        RectTransform cardToUse = cards[handnum];
        cards.RemoveAt(handnum);
        ClearnowusingCard();
        nowusingCard = cardToUse;
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
        Card cardComp = nowusingCard.GetComponent<Card>();
        if (cardComp.effects.Any(e => e.requiredMode == Board.BoardMode.command || e.requiredMode == Board.BoardMode.targeting))
            board.UseCard(cardComp);
        CardDragArrow.instance?.Show(nowusingCard);
        usingCardMoving = true;
        RectTransform usingCard = nowusingCard;
        EnqueueMove(usingCard, GetZonePosition(CardPositionZone.NowUsing), Quaternion.identity, 0.35f, () =>
        {
            usingCardMoving = false;
            if (nowusingCard == null) return;
            if (board.boardmode == Board.BoardMode.Inspect)
                board.UseCard(usingCard.GetComponent<Card>());
            if (pendingFirstTarget.x >= 0)
            {
                Vector2 target = pendingFirstTarget;
                pendingFirstTarget = new Vector2(-1, -1);
                board.ButtonClicked(target);
            }
        });
        return true;
    }

    public void ClearnowusingCard()         
    {
        if (nowusingCard)
        {
            cards.Add(nowusingCard);
        }
        nowusingCard = null;
        AlignCards();
    }

    public void HandtoDiscardAll()
    {
        while(cards.Count > 0)
        {
            HandtoDiscard(0);
        }
    }

    public void HandtoDiscard(int num)
    {
        if (cards.Count <= num)
            return;
        HandtoDiscard(cards[num]);
    }

    public void HandtoDiscard(RectTransform card)
    {
        CancelCardMove(card);
        Discardcards.Add(card);
        card.position = GetZonePosition(CardPositionZone.Discard);
        cards.Remove(card);
        NotifyPileChanged();
    }
    public void DrawTurnStartCards()
    {
        var newCards = new List<RectTransform>();
        for (int i = 0; i < 5; i++)
        {
            if (Deckcards.Count == 0) DiscardtoDeck();
            if (Deckcards.Count == 0) break;
            var card = Deckcards.Dequeue();
            cards.Add(card);
            newCards.Add(card);
        }
        if (newCards.Count == 0) return;

        AlignCards();
        NotifyPileChanged();

        foreach (var c in newCards)
        {
            Vector3 targetPos = c.position;
            Quaternion targetRot = c.localRotation;
            c.position = GetZonePosition(CardPositionZone.Deck);
            c.localRotation = Quaternion.identity;
            EnqueueMove(c, targetPos, targetRot, 0.3f);
        }
    }
    public void RefreshAllCardViews()
    {
        foreach (var rt in cards.Concat(Discardcards).Concat(Deckcards))
            rt.GetComponent<Card>()?.RefreshView();
        if (nowusingCard != null) nowusingCard.GetComponent<Card>()?.RefreshView();
        UpdateCardInteractability();
    }

    public void UpdateCardInteractability()
    {
        if (cardSelectionMode) return;
        bool playerTurn = TurnManager.instance != null && TurnManager.instance.currentState == TurnState.Player;
        bool boardProcessing = isCardEffecting && nowusingCard == null;
        foreach (var rt in cards)
        {
            Card card = rt.GetComponent<Card>();
            if (card == null) continue;
            bool canUse = playerTurn && !boardProcessing && card.Cost <= currentenergy && card.CanUse();
            rt.GetComponent<CanvasGroup>().interactable = canUse;
        }
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
            CancelCardUsage();
    }

    public void CancelCardUsage()
    {
        CardDragArrow.instance?.Hide();
        if (nowusingCard == null || isCardEffecting || board.EffectApplied) return;

        CancelCardMove(nowusingCard);
        usingCardMoving = false;

        RectTransform card = nowusingCard;
        nowusingCard = null;
        int originalIndex = card.GetComponent<Card>().handNumber;
        cards.Insert(Mathf.Clamp(originalIndex, 0, cards.Count), card);
        AlignCards();
        pendingFirstTarget = new Vector2(-1, -1);
        board.CancelCardUsage();
    }

    public void FinishUseCard()             //사용한 카드 처리
    {
        CardDragArrow.instance?.Hide();
        RefreshAllCardViews();
        if (nowusingCard)
        {
            Card card = nowusingCard.GetComponent<Card>();
            int costToDeduct = card.Cost;
            // OneUse 코스트 임시 변경 복구 (복구 전 실제 사용 코스트를 저장)
            if (card.originalCost >= 0 && card.costDuration == CostDuration.OneUse)
            {
                card.Cost = card.originalCost;
                card.originalCost = -1;
            }
            currentenergy -= costToDeduct;
            RectTransform usedCard = nowusingCard;
            usedCard.GetComponent<Card>().handNumber = -1;
            nowusingCard = null;
            if (card.exileOnUse)
            {
                Exilecards.Add(usedCard);
                EnqueueMove(usedCard, GetZonePosition(CardPositionZone.Exile), Quaternion.identity, 0.25f, () => isCardEffecting = false);
            }
            else
            {
                EnqueueMove(usedCard, GetZonePosition(CardPositionZone.Discard), Quaternion.identity, 0.15f, () =>
                {
                    Discardcards.Add(usedCard);
                    isCardEffecting = false;
                    NotifyPileChanged();
                });
            }
        }
        else
        {
            isCardEffecting = false;
        }
    }

    void EnqueueMove(RectTransform card, Vector3 pos, Quaternion rot, float duration, Action onComplete = null)
    {
        CancelCardMove(card);
        moveQueue.Enqueue(new CardMoveRequest { card = card, pos = pos, rot = rot, duration = duration, onComplete = onComplete });
        if (moveQueueRoutine == null)
            moveQueueRoutine = StartCoroutine(ProcessMoveQueue());
    }

    void CancelCardMove(RectTransform card)
    {
        if (moveQueue.Count > 0 && moveQueue.Any(r => r.card == card))
            moveQueue = new Queue<CardMoveRequest>(moveQueue.Where(r => r.card != card));
        if (activeMoves.TryGetValue(card, out var cor) && cor != null)
        {
            StopCoroutine(cor);
            activeMoves.Remove(card);
        }
    }

    IEnumerator ProcessMoveQueue()
    {
        while (moveQueue.Count > 0)
        {
            CardMoveRequest req = moveQueue.Dequeue();
            activeMoves[req.card] = StartCoroutine(RunCardMove(req));

            // 큐가 비어 있어도 무조건 대기한다. 이미 다음 while 검사 전에
            // 같은 프레임에서 EnqueueMove가 더 호출될 수 있기 때문에, 여기서
            // 바로 큐가 비었다고 끝내버리면 0.15초 간격이 무시된다.
            yield return new WaitForSeconds(MoveQueueGap);
        }
        moveQueueRoutine = null;
    }

    IEnumerator RunCardMove(CardMoveRequest req)
    {
        Vector3 startPos = req.card.position;
        Quaternion startRot = req.card.localRotation;
        float elapsed = 0f;
        while (elapsed < req.duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / req.duration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            req.card.position = Vector3.Lerp(startPos, req.pos, smooth);
            req.card.localRotation = Quaternion.Lerp(startRot, req.rot, smooth);
            yield return null;
        }
        req.card.position = req.pos;
        req.card.localRotation = req.rot;
        activeMoves.Remove(req.card);
        req.onComplete?.Invoke();
    }
    private void UpdateCurrentEnergy()
    {
        CurrentEnergyText.text = string.Format("{0}/{1}", currentenergy, maxenergy);
    }

    public void DrawCard()
    {
        if (Deckcards.Count == 0)
            DiscardtoDeck();
        if (Deckcards.Count != 0)
        {
            RectTransform newCard = Deckcards.Dequeue();
            cards.Add(newCard);
            pendingDrawCards.Add(newCard);
            NotifyPileChanged();
            batchDrawCoroutine ??= StartCoroutine(BatchDrawAnim());
        }
    }

    IEnumerator BatchDrawAnim()
    {
        yield return null;  // 같은 프레임의 모든 DrawCard() 호출이 끝날 때까지 대기

        var toDraw = new List<RectTransform>(pendingDrawCards);
        pendingDrawCards.Clear();
        batchDrawCoroutine = null;

        AlignCards();  // 최종 손패 크기 기준으로 한 번만 정렬

        foreach (var c in toDraw)
        {
            Vector3 targetPos = c.position;
            Quaternion targetRot = c.localRotation;
            c.position = GetZonePosition(CardPositionZone.Deck);
            c.localRotation = Quaternion.identity;
            EnqueueMove(c, targetPos, targetRot, 0.3f);
        }
    }

    void DiscardtoDeck()
    {
        if (Discardcards.Count == 0) return;

        var shuffledCards = Discardcards.OrderBy(_ => UnityEngine.Random.value).ToList();

        foreach (var card in shuffledCards)
        {
            card.position = GetZonePosition(CardPositionZone.Deck);
            Deckcards.Enqueue(card);
        }

        Discardcards.Clear();
        NotifyPileChanged();
    }

    public void GetMaxEnergy()
    {
        currentenergy = maxenergy;
    }

    // 이벤트 레벨처럼 카드를 쓰지 않는 레벨에서는 카드 관련 UI를 숨김
    public void SetCombatUIVisible(bool visible)
    {
        UnSeenEvent?.SetActive(visible);
    }

    // 전투 중 새 카드를 실제로 추가한다. 화면 중심에 나타나 1초 머물다 targetZone으로 날아가는
    // 카드 자체가 그대로 targetZone의 더미/손패에 들어가는 실제 카드이며, 별도의 연출용 인스턴스는 만들지 않는다.
    // Hand로 추가하는 경우는 부채꼴 슬롯에 바로 맞춰 넣어야 해서 AlignCards로 즉시 배치한다.
    public void AddCardDuringCombat(string cardname, CardPositionZone targetZone = CardPositionZone.Discard)
    {
        GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardname);
        if (obj == null) return;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.position = GetZonePosition(CardPositionZone.Center);
        rt.localRotation = Quaternion.identity;

        if (targetZone == CardPositionZone.Hand)
        {
            cards.Add(rt);
            AlignCards();
            NotifyPileChanged();
            return;
        }

        if (targetZone == CardPositionZone.Deck)
            Deckcards.Enqueue(rt);
        else
            Discardcards.Add(rt);
        NotifyPileChanged();

        StartCoroutine(ShowAddedCardRoutine(rt, GetZonePosition(targetZone)));
    }

    // 덱에 카드가 추가됐을 때 화면 중심에 잠깐 보여준 뒤 targetZone 위치로 이동시키는 연출용 카드.
    // 실제 손패/덱/버린 더미 풀에는 들어가지 않는 시각 효과 전용 인스턴스라 애니메이션이 끝나면 파괴한다.
    public void ShowAddedCard(string cardname, CardPositionZone targetZone)
    {
        GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardname);
        if (obj == null) return;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.position = GetZonePosition(CardPositionZone.Center);
        rt.localRotation = Quaternion.identity;

        StartCoroutine(ShowAddedCardRoutine(rt, GetZonePosition(targetZone), () => Destroy(rt.gameObject)));
    }

    IEnumerator ShowAddedCardRoutine(RectTransform rt, Vector3 targetPos, Action onComplete = null)
    {
        yield return new WaitForSeconds(1f);
        EnqueueMove(rt, targetPos, Quaternion.identity, 0.3f, onComplete);
    }

    // count장을 손패에서 무작위로 뽑아 반환 (count <= 0이면 전부)
    List<RectTransform> PickRandomCardsFromHand(int count)
    {
        int n = (count <= 0) ? cards.Count : Mathf.Min(count, cards.Count);
        var snapshot = new List<RectTransform>(cards);
        var result = new List<RectTransform>();
        for (int i = 0; i < n && snapshot.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, snapshot.Count);
            result.Add(snapshot[idx]);
            snapshot.RemoveAt(idx);
        }
        return result;
    }

    public void HandtoDiscardCount(int count)
    {
        foreach (var card in PickRandomCardsFromHand(count))
            HandtoDiscard(card);
        AlignCards();
    }

    public void HandtoDeckCount(int count)
    {
        foreach (var card in PickRandomCardsFromHand(count))
        {
            cards.Remove(card);
            card.position = GetZonePosition(CardPositionZone.Deck);
            Deckcards.Enqueue(card);
        }
        var list = Deckcards.ToList().OrderBy(_ => UnityEngine.Random.value).ToList();
        Deckcards = new Queue<RectTransform>(list);
        AlignCards();
        NotifyPileChanged();
    }

    public void HandtoExileCount(int count)
    {
        foreach (var card in PickRandomCardsFromHand(count))
            ExileCard(card);
    }

    public void HandtoDeckTop(int count)
    {
        var toReturn = PickRandomCardsFromHand(count);
        foreach (var card in toReturn)
        {
            cards.Remove(card);
            card.position = GetZonePosition(CardPositionZone.Deck);
        }
        var newDeck = toReturn.Concat(Deckcards.ToList()).ToList();
        Deckcards = new Queue<RectTransform>(newDeck);
        AlignCards();
        NotifyPileChanged();
    }

    // 어느 존에서든 카드를 제거. 손패에 있었으면 true 반환
    bool RemoveCardFromAnyZone(RectTransform card)
    {
        if (cards.Remove(card)) return true;
        if (Discardcards.Remove(card)) return false;
        var deckList = Deckcards.ToList();
        if (deckList.Remove(card))
            Deckcards = new Queue<RectTransform>(deckList);
        return false;
    }

    public void ExileHandCard(int handnum)
    {
        if (handnum < 0 || handnum >= cards.Count) return;
        ExileCard(cards[handnum]);
    }

    public void ExileCard(RectTransform card)
    {
        bool wasInHand = RemoveCardFromAnyZone(card);
        if (nowusingCard == card)
            nowusingCard = null;
        Exilecards.Add(card);
        EnqueueMove(card, GetZonePosition(CardPositionZone.Exile), Quaternion.identity, 0.25f);
        if (wasInHand)
            AlignCards();
        NotifyPileChanged();
    }

    // ────────── 카드 선택 패널 ──────────

    /// <summary>카드 선택 패널을 열어 플레이어가 카드를 선택하도록 합니다.</summary>
    public void ShowCardSelectionPanel(CardZone zone, int count, CardEffect effect, Action<List<RectTransform>> onConfirm)
    {
        panelRequiredCount = count;
        panelCallback = onConfirm;
        selectedInPanel.Clear();
        savedCardStates.Clear();

        panelCardPool.Clear();
        if (zone == CardZone.Hand   || zone == CardZone.Any) panelCardPool.AddRange(cards);
        if (zone == CardZone.Discard || zone == CardZone.Any) panelCardPool.AddRange(Discardcards);
        if (zone == CardZone.Deck   || zone == CardZone.Any) panelCardPool.AddRange(Deckcards);

        // 선택 가능한 카드가 없으면 즉시 빈 목록으로 콜백
        if (panelCardPool.Count == 0)
        {
            onConfirm?.Invoke(new List<RectTransform>());
            return;
        }

        cardSelectionPanel.SetActive(true);

        foreach (var card in panelCardPool)
        {
            savedCardStates[card] = (card.parent, card.position);
            card.SetParent(cardSelectionContent, true);
        }

        LayoutCardsInPanel();
        cardSelectionMode = true;

        // 안내 문구
        string verb = effect.type == EffectType.SelectAndDiscard ? "버릴" : "코스트를 변경할";
        int max = count > 0 ? Mathf.Min(count, panelCardPool.Count) : panelCardPool.Count;
        if (selectionPromptText != null)
            selectionPromptText.text = $"{verb} 카드를 {max}장 선택하세요";

        UpdatePanelUI();
    }

    void LayoutCardsInPanel()
    {
        const float spacing = 180f;
        float totalWidth = (panelCardPool.Count - 1) * spacing;
        for (int i = 0; i < panelCardPool.Count; i++)
        {
            panelCardPool[i].localPosition = new Vector3(-totalWidth / 2f + i * spacing, 0f, 0f);
            panelCardPool[i].localRotation = Quaternion.identity;
            panelCardPool[i].GetComponent<Card>()?.ScaleDefault();
        }
    }

    /// <summary>패널 내 카드 클릭 시 선택/해제 토글</summary>
    public void ToggleCardInPanel(RectTransform card)
    {
        if (!panelCardPool.Contains(card)) return;

        if (selectedInPanel.Contains(card))
        {
            selectedInPanel.Remove(card);
            card.GetComponent<Card>()?.ScaleDefault();
        }
        else
        {
            int maxAllowed = panelRequiredCount > 0
                ? Mathf.Min(panelRequiredCount, panelCardPool.Count)
                : panelCardPool.Count;
            if (selectedInPanel.Count < maxAllowed)
            {
                selectedInPanel.Add(card);
                card.GetComponent<Card>()?.ScaleHover();
            }
        }
        UpdatePanelUI();
    }

    void UpdatePanelUI()
    {
        int selected = selectedInPanel.Count;
        int required = panelRequiredCount > 0
            ? Mathf.Min(panelRequiredCount, panelCardPool.Count)
            : panelCardPool.Count;

        if (selectionCountText != null)
            selectionCountText.text = $"{selected}/{required} 선택됨";

        if (confirmSelectionBtn != null)
            confirmSelectionBtn.interactable = (panelRequiredCount == 0) || (selected >= required);
    }

    /// <summary>Inspector의 확인 버튼 OnClick에 연결하세요.</summary>
    public void ConfirmCardSelection()
    {
        cardSelectionMode = false;
        cardSelectionPanel.SetActive(false);

        var selected = new List<RectTransform>(selectedInPanel);

        foreach (var card in panelCardPool)
        {
            var (originalParent, worldPos) = savedCardStates[card];
            card.SetParent(originalParent, true);
            card.position = worldPos;
            card.localRotation = Quaternion.identity;
            card.GetComponent<Card>()?.ScaleDefault();
        }

        AlignCards(); // 손패 아크 레이아웃 복구

        panelCallback?.Invoke(selected);
    }

    // ────────────────────────────────────

    /// <summary>카드를 어느 존에서든 버린 카드 더미로 이동합니다.</summary>
    public void MoveCardToDiscard(RectTransform card)
    {
        bool wasInHand = RemoveCardFromAnyZone(card);
        card.SetParent(GetComponent<RectTransform>(), true);
        Discardcards.Add(card);
        EnqueueMove(card, GetZonePosition(CardPositionZone.Discard), Quaternion.identity, 0.3f);
        if (wasInHand) AlignCards();
        NotifyPileChanged();
    }

    /// <summary>카드를 어느 존에서든 덱으로 이동합니다 (덱 맨 아래에 추가).</summary>
    public void MoveCardToDeck(RectTransform card)
    {
        bool wasInHand = RemoveCardFromAnyZone(card);
        card.SetParent(GetComponent<RectTransform>(), true);
        var newDeckList = Deckcards.ToList();
        newDeckList.Add(card);
        Deckcards = new Queue<RectTransform>(newDeckList);
        EnqueueMove(card, GetZonePosition(CardPositionZone.Deck), Quaternion.identity, 0.3f);
        if (wasInHand) AlignCards();
        NotifyPileChanged();
    }

    /// <summary>코스트가 ThisTurnOnly로 변경된 카드들을 원래 코스트로 복구합니다.</summary>
    public void RestoreThisTurnCosts()
    {
        var allCards = cards
            .Concat(Discardcards)
            .Concat(Deckcards)
            .Select(rt => rt.GetComponent<Card>())
            .Where(c => c != null && c.originalCost >= 0 && c.costDuration == CostDuration.ThisTurnOnly);

        foreach (var card in allCards.ToList())
        {
            card.Cost = card.originalCost;
            card.originalCost = -1;
            card.RefreshView();
        }
    }

    public void OnDragCardReleased(Vector2 screenPos)
    {
        if (nowusingCard == null) return;
        Card card = nowusingCard.GetComponent<Card>();
        bool needsTargeting = card.effects.Any(e =>
            e.requiredMode == Board.BoardMode.command || e.requiredMode == Board.BoardMode.targeting);
        if (!needsTargeting) return;

        Vector2 boardPos = FindBoardPosAtScreen(screenPos);

        // 보드 밖이거나 카드의 dragDropTarget 조건을 만족하지 않으면 즉시 취소
        if (boardPos.x < 0 || !board.IsValidDragTarget(boardPos, card.dragDropTarget))
        {
            if (boardPos.x >= 0)
            {
                string reason = card.dragDropTarget switch
                {
                    DragDropTarget.Ally => "아군 기물에 사용해야 합니다",
                    DragDropTarget.Enemy => "적 기물에 사용해야 합니다",
                    DragDropTarget.AnyPiece => "기물이 있는 칸에 사용해야 합니다",
                    _ => "올바른 위치가 아닙니다"
                };
                AnnouncementUI.instance?.Show(reason);
            }
            CancelCardUsage();
            return;
        }

        if (!CheckCasterStatusRestrictions(boardPos, card))
        {
            CancelCardUsage();
            return;
        }

        CardDragArrow.instance?.Hide();
        if (!usingCardMoving)
            board.ButtonClicked(boardPos);
        else
            pendingFirstTarget = boardPos;
    }

    bool CheckCasterStatusRestrictions(Vector2 casterPos, Card card)
    {
        Piece caster = board.GetPieceAt(casterPos);
        if (caster == null) return true;

        if (card.requiresCasterNotMoved && caster.movedThisTurn)
        {
            AnnouncementUI.instance?.Show("이동 전에만 사용할 수 있습니다");
            return false;
        }

        foreach (var effect in caster.activeEffects)
        {
            switch (effect)
            {
                case MovementDisabledEffect:
                    if (card.effects.Count > 0 && card.effects[0].type == EffectType.Move)
                    {
                        AnnouncementUI.instance?.Show("이동 불가 상태입니다");
                        return false;
                    }
                    break;
            }
        }
        return true;
    }

    Vector2 FindBoardPosAtScreen(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var btn = hit.collider.GetComponentInParent<global::Button>();
            if (btn != null) return btn.GetLocation();
        }
        return new Vector2(-1, -1);
    }

    // 부채꼴 배치에서 count장 중 i번째 카드의 로컬 위치/회전을 계산
    (Vector3 pos, Quaternion rot) ComputeCardTransform(int i, int count)
    {
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;
        float currentAngle = startAngle + ((count - 1 - i) * angleBetween);

        float radian = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Sin(radian) * radius;
        float y = Mathf.Cos(radian) * radius - radius;

        return (new Vector3(x, y + heightOffset, 0), Quaternion.Euler(0, 0, -currentAngle));
    }

    [ContextMenu("Align Cards")] // 인스펙터 메뉴에서 바로 테스트 가능
    public void AlignCards()
    {
        int count = cards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var (pos, rot) = ComputeCardTransform(i, count);
            cards[i].SetSiblingIndex(i);
            cards[i].localPosition = pos;
            cards[i].localRotation = rot;

            Card cardComp = cards[i].GetComponent<Card>();
            cardComp.OnUnSelected -= CardUnSelected;
            cardComp.OnUnSelected += CardUnSelected;
            cardComp.cardCanvas = gameObject;
            cardComp.handNumber = i;
        }
        UpdateCardInteractability();
    }
    public void ExcludeAlignCards(int excludeCard = -1)
    {
        int count = excludeCard < 0 ? cards.Count : cards.Count - 1;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var (pos, rot) = ComputeCardTransform(i, count);
            RectTransform card = (i >= excludeCard && excludeCard >= 0) ? cards[i + 1] : cards[i];
            card.SetSiblingIndex(i);
            card.localPosition = pos;
            card.localRotation = rot;
        }
        if (excludeCard >= 0)
            cards[excludeCard].SetAsLastSibling();
    }

    // 카드가 화면상에 존재할 수 있는 위치. enum으로 받아 GetZonePosition으로 실제 Vector3를 얻는다.
    Vector3 GetZonePosition(CardPositionZone zone) => zone switch
    {
        CardPositionZone.Deck => DeckZone.position,
        CardPositionZone.Discard => DiscardZone.position,
        CardPositionZone.Exile => ExileZone.position,
        CardPositionZone.NowUsing => CardNowUsingPos.position,
        CardPositionZone.Center => GetComponent<RectTransform>().position,
        CardPositionZone.Hand => HandZone.GetComponent<RectTransform>().position,
        _ => DiscardZone.position
    };
}

public enum CardPositionZone { Deck, Discard, Exile, NowUsing, Center, Hand }