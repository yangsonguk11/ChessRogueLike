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
    public List<RectTransform> cards = new List<RectTransform>(); // �տ� �� ī���
    public List<RectTransform> Discardcards = new List<RectTransform>();
    public Queue<RectTransform> Deckcards = new Queue<RectTransform>();
    [SerializeField] GameObject HandZone;
    [SerializeField] RectTransform CardNowUsingPos;
    [SerializeField] RectTransform DiscardZone;
    [SerializeField] RectTransform DeckZone;
    [SerializeField] RectTransform ExileZone;
    public List<RectTransform> Exilecards = new List<RectTransform>();
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius; // ���� ������ (Ŭ���� �ϸ���)
    [SerializeField] float angleBetween;  // ī�� ������ ����
    [SerializeField] float heightOffset; // ��ä���� ���� ����

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
    Coroutine pendingCardCoroutine;
    Coroutine pendingMoveCardCoroutine;
    List<RectTransform> pendingDrawCards = new List<RectTransform>();
    Coroutine batchDrawCoroutine;
    Vector2 pendingFirstTarget = new Vector2(-1, -1);
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
                rt.position = DeckZone.position;
            }
        }
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }

    public void CardSelected(int handNum)   //���� ī�带 ������ ��
    {
        if (handNum == -1)
            return;
        cards[handNum].GetComponent<Card>().SelectedTrue();
        ExcludeAlignCards(cards[handNum].GetComponent<Card>().handNumber);
        HandZone.GetComponent<Image>().raycastTarget = true;
    }

    public void CardUnSelected()            //���� ī�带 ���� ��
    {
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }
    public void UseCard(int handnum)        //���� ī�带 ��� ���� ����� ��
    {
        Debug.Log(cards[handnum].GetComponent<Card>().Cost > currentenergy);
        Debug.Log(isCardEffecting);
        Debug.Log(TurnManager.instance.currentState != TurnState.Player);
        Card card = cards[handnum].GetComponent<Card>();
        // isCardEffecting이면서 nowusingCard가 없으면 보드가 실제 효과 처리 중 → 차단
        // nowusingCard가 있으면 아직 애니메이션/대기 중이므로 교체 허용
        if (card.Cost > currentenergy || (isCardEffecting && nowusingCard == null) || TurnManager.instance.currentState != TurnState.Player || !card.CanUse())
            return;
        if (pendingCardCoroutine != null)
        {
            StopCoroutine(pendingCardCoroutine);
            pendingCardCoroutine = null;
        }
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
        pendingCardCoroutine = StartCoroutine(AnimateCardToUsingPos(nowusingCard));
    }

    public void ClearnowusingCard()         //������� ī�� �ʱ�ȭ
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
        Debug.LogFormat("{0} ��", cards.Count);
        while(cards.Count > 0)
        {
            HandtoDiscard(0);
        }
    }

    public void HandtoDiscard(int num)
    {
        Debug.LogFormat("{0} {1}", cards.Count, num);
        if (cards.Count <= num)
            return;
        HandtoDiscard(cards[num]);
    }

    public void HandtoDiscard(RectTransform card)
    {
        Discardcards.Add(card);
        card.position = DiscardZone.position;
        cards.Remove(card);
        NotifyPileChanged();
    }
    public void DrawTurnStartCards()
    {
        StartCoroutine(DrawCardsWithDelay(5, 0.15f));
    }

    IEnumerator DrawCardsWithDelay(int count, float delay)
    {
        var newCards = new List<RectTransform>();
        for (int i = 0; i < count; i++)
        {
            if (Deckcards.Count == 0) DiscardtoDeck();
            if (Deckcards.Count == 0) break;
            var card = Deckcards.Dequeue();
            cards.Add(card);
            newCards.Add(card);
        }
        if (newCards.Count == 0) yield break;

        AlignCards();
        NotifyPileChanged();

        var targetPositions = new List<Vector3>();
        var targetRotations = new List<Quaternion>();
        foreach (var c in newCards)
        {
            targetPositions.Add(c.position);
            targetRotations.Add(c.localRotation);
            c.position = DeckZone.position;
            c.localRotation = Quaternion.identity;
        }

        for (int i = 0; i < newCards.Count; i++)
        {
            StartCoroutine(MoveCard(newCards[i], targetPositions[i], targetRotations[i], 0.3f));
            if (i < newCards.Count - 1)
                yield return new WaitForSeconds(delay);
        }
    }
    public void RefreshAllCardViews()
    {
        foreach (var rt in cards)
            rt.GetComponent<Card>()?.RefreshView();
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

        if (pendingMoveCardCoroutine != null)
        {
            StopCoroutine(pendingMoveCardCoroutine);
            pendingMoveCardCoroutine = null;
        }
        if (pendingCardCoroutine != null)
        {
            StopCoroutine(pendingCardCoroutine);
            pendingCardCoroutine = null;
        }

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
                StartCoroutine(AnimateCardToExileAndFinish(usedCard));
            }
            else
            {
                StartCoroutine(AnimateCardToDiscard(usedCard));
            }
        }
        else
        {
            isCardEffecting = false;
        }
    }

    IEnumerator AnimateCardToExileAndFinish(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, ExileZone.position, Quaternion.identity, 0.25f));
        isCardEffecting = false;
    }

    IEnumerator AnimateCardToUsingPos(RectTransform card)
    {
        var moveCor = StartCoroutine(MoveCard(card, CardNowUsingPos.position, Quaternion.identity, 0.35f));
        pendingMoveCardCoroutine = moveCor;
        yield return moveCor;
        pendingMoveCardCoroutine = null;
        pendingCardCoroutine = null;
        if (nowusingCard == null) yield break;
        if (board.boardmode == Board.BoardMode.Inspect)
            board.UseCard(card.GetComponent<Card>());
        if (pendingFirstTarget.x >= 0)
        {
            Vector2 target = pendingFirstTarget;
            pendingFirstTarget = new Vector2(-1, -1);
            board.ButtonClicked(target);
        }
    }

    IEnumerator AnimateCardToDiscard(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, DiscardZone.position, Quaternion.identity, 0.15f));
        Discardcards.Add(card);
        isCardEffecting = false;
        NotifyPileChanged();
    }

    IEnumerator MoveCard(RectTransform card, Vector3 targetWorldPos, Quaternion targetLocalRot, float duration)
    {
        Vector3 startPos = card.position;
        Quaternion startRot = card.localRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            card.position = Vector3.Lerp(startPos, targetWorldPos, smooth);
            card.localRotation = Quaternion.Lerp(startRot, targetLocalRot, smooth);
            yield return null;
        }
        card.position = targetWorldPos;
        card.localRotation = targetLocalRot;
    }
    private void UpdateCurrentEnergy()
    {
        CurrentEnergyText.text = string.Format("{0}/{1}", currentenergy, maxenergy);
    }

    public void DrawCard()
    {
        if (Deckcards.Count == 0)
        {
            DiscardtoDeck();
            Debug.Log("nodeck");
        }
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

        var targetPositions = new List<Vector3>();
        var targetRotations = new List<Quaternion>();
        foreach (var c in toDraw)
        {
            targetPositions.Add(c.position);
            targetRotations.Add(c.localRotation);
            c.position = DeckZone.position;
            c.localRotation = Quaternion.identity;
        }

        for (int i = 0; i < toDraw.Count; i++)
            StartCoroutine(MoveCard(toDraw[i], targetPositions[i], targetRotations[i], 0.3f));
    }

    void DiscardtoDeck()
    {
        if (Discardcards.Count == 0) return;

        var shuffledCards = Discardcards.OrderBy(_ => UnityEngine.Random.value).ToList();

        foreach (var card in shuffledCards)
        {
            card.position = DeckZone.position;
            Deckcards.Enqueue(card);
        }

        Discardcards.Clear();
        NotifyPileChanged();
    }

    public void GetMaxEnergy()
    {
        currentenergy = maxenergy;
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
            card.position = DeckZone.position;
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
            card.position = DeckZone.position;
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
        StartCoroutine(AnimateCardToExile(card));
        if (wasInHand)
            AlignCards();
        NotifyPileChanged();
    }

    IEnumerator AnimateCardToExile(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, ExileZone.position, Quaternion.identity, 0.25f));
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
        StartCoroutine(MoveCard(card, DiscardZone.position, Quaternion.identity, 0.3f));
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
        StartCoroutine(MoveCard(card, DeckZone.position, Quaternion.identity, 0.3f));
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
            CancelCardUsage();
            return;
        }

        CardDragArrow.instance?.Hide();
        if (pendingCardCoroutine == null)
            board.ButtonClicked(boardPos);
        else
            pendingFirstTarget = boardPos;
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

    [ContextMenu("Align Cards")] // 인스펙터 메뉴에서 바로 테스트 가능
    public void AlignCards()
    {
        int count = cards.Count;
        if (count == 0) return;

        // ��ü ���� ���� ��� (����� 0���� ����)
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + ((count - 1 - i) * angleBetween);

            // 1. ��ġ ��� (�ﰢ�Լ� ���)
            // �������� ��ȯ: Degree * (PI / 180)
            float radian = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius - radius; // ���� ���κп� ����

            // 2. ī�� ��ǥ �� ȸ�� ����
            cards[i].SetSiblingIndex(i);
            cards[i].localPosition = new Vector3(x, y + heightOffset, 0);
            cards[i].localRotation = Quaternion.Euler(0, 0, -currentAngle);

            Card cardComp = cards[i].GetComponent<Card>();
            cardComp.OnUnSelected -= CardUnSelected;
            cardComp.OnUnSelected += CardUnSelected;
            cardComp.cardCanvas = gameObject;
            cards[i].gameObject.GetComponent<Card>().handNumber = i;
        }
        UpdateCardInteractability();
    }
    public void ExcludeAlignCards(int excludeCard = -1)
    {
        int count;
        if (excludeCard < 0)
            count = cards.Count;
        else
            count = cards.Count - 1;
        if (count == 0) return;

        // ��ü ���� ���� ��� (����� 0���� ����)
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + ((count - 1 - i) * angleBetween);

            // 1. ��ġ ��� (�ﰢ�Լ� ���)
            // �������� ��ȯ: Degree * (PI / 180)
            float radian = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius - radius; // ���� ���κп� ����

            // 2. ī�� ��ǥ �� ȸ�� ����
            if (i >= excludeCard && excludeCard >= 0)
            {
                cards[i+1].SetSiblingIndex(i);
                cards[i+1].localPosition = new Vector3(x, y + heightOffset, 0);
                cards[i+1].localRotation = Quaternion.Euler(0, 0, -currentAngle);
            }
            else
            {
                cards[i].SetSiblingIndex(i);
                cards[i].localPosition = new Vector3(x, y + heightOffset, 0);
                cards[i].localRotation = Quaternion.Euler(0, 0, -currentAngle);
            }
        }
        if(excludeCard >= 0)
            cards[excludeCard].SetAsLastSibling();

    }
}