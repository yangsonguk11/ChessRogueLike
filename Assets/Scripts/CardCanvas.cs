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

    // 현재 화면에 표시 중인 아군 기물. 아래 손패/덱/버림/소멸 더미는 전부 이 기물의 PieceDeck을 가리키는
    // 위임 프로퍼티다 — 기물마다 카드 자원이 완전히 분리돼 있고, CardCanvas는 그중 하나를 보여주는 뷰일 뿐이다.
    Piece activePiece;
    public Piece ActivePiece => activePiece;
    static readonly List<RectTransform> EmptyCardList = new List<RectTransform>();
    static readonly Queue<RectTransform> EmptyCardQueue = new Queue<RectTransform>();

    public List<RectTransform> cards => activePiece?.pieceDeck?.Hand ?? EmptyCardList; // 손에 든 카드들
    public List<RectTransform> Discardcards => activePiece?.pieceDeck?.Discard ?? EmptyCardList;
    public Queue<RectTransform> Deckcards
    {
        get => activePiece?.pieceDeck?.Deck ?? EmptyCardQueue;
        set { if (activePiece?.pieceDeck != null) activePiece.pieceDeck.Deck = value; }
    }
    [SerializeField] GameObject HandZone;
    [SerializeField] RectTransform CardNowUsingPos;
    [SerializeField] RectTransform DiscardZone;
    [SerializeField] RectTransform DeckZone;
    [SerializeField] RectTransform ExileZone;
    public List<RectTransform> Exilecards => activePiece?.pieceDeck?.Exile ?? EmptyCardList;
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

    // SavedDeck 패널에서 선택 확정 시 어떤 동작을 할지
    public enum SavedDeckAction { Remove }

    public static bool cardSelectionMode = false;
    List<RectTransform> panelCardPool = new List<RectTransform>();
    HashSet<RectTransform> selectedInPanel = new HashSet<RectTransform>();
    public bool IsSelectedInPanel(RectTransform card) => selectedInPanel.Contains(card);
    int panelRequiredCount;
    bool panelIsSavedDeck; // true면 panelCardPool이 deckCardIDs로부터 임시 스폰된 것 (확인 시 원래 자리로 되돌리지 않고 savedDeckAction에 따라 처리)
    SavedDeckAction panelSavedDeckAction;
    int panelPieceIndex = -1; // SavedDeck 패널이 어느 기물의 deckCardIDs를 대상으로 하는지
    Action<List<RectTransform>> panelCallback;
    Dictionary<RectTransform, (Transform parent, Vector3 worldPos)> savedCardStates
        = new Dictionary<RectTransform, (Transform, Vector3)>();
    // ────────────────────────────────────────────────────────────

    // 에너지는 기물별이 아니라 아군 전체가 공유. 최대치는 아군 기물 수에 따라 늘어난다
    // (기물 1명이면 baseMaxEnergy, 이후 1명 늘 때마다 +1 — 예: 4 → 5 → 6...).
    int _currentenergy = 3;
    public int currentenergy
    {
        get => _currentenergy;
        set { _currentenergy = value; UpdateCurrentEnergy(); UpdateCardInteractability(); }
    }
    [SerializeField] int baseMaxEnergy = 4; // 아군 1명 기준 최대 에너지
    public int maxenergy
    {
        get
        {
            int allyCount = board != null ? board.GetAllAllyPieces().Count : 1;
            return baseMaxEnergy + Mathf.Max(0, allyCount - 1);
        }
    }
    public RectTransform nowusingCard;
    public bool isCardEffecting;
    bool usingCardMoving;
    bool nowUsingCardHeld; // true면 nowusingCard가 보드 커밋 없이 마우스에 들려있는 상태
    List<RectTransform> pendingDrawCards = new List<RectTransform>();
    Coroutine batchDrawCoroutine;
    Vector2Int pendingFirstTarget = new Vector2Int(-1, -1);

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
        HandZone.GetComponent<Image>().raycastTarget = false;
    }

    // 기물 1명분의 카드를 스폰해 그 기물의 버림더미(초기 덱)에 채운다. 화면에는 활성 기물일 때만 보이도록
    // 비활성 상태로 만들어두고, SetActivePiece가 실제로 보여줄 때 활성화한다.
    public void SpawnDeckForPiece(Piece piece, List<string> deckCardIDs)
    {
        if (piece?.pieceDeck == null) return;

        foreach (string cardName in deckCardIDs)
        {
            GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardName);
            if (obj == null) continue;

            var rt = obj.GetComponent<RectTransform>();
            piece.pieceDeck.Discard.Add(rt);
            rt.position = GetZonePosition(CardPositionZone.Deck);
            obj.SetActive(piece == activePiece);
        }
    }

    // 보드에서 아군 기물을 클릭(또는 hover 미리보기)했을 때 호출: 화면에 보이는 손패/덱/버림/소멸 더미를
    // 해당 기물의 것으로 전환한다(에너지는 아군 공유라 전환 대상이 아님). 카드 사용/애니메이션이 진행 중일
    // 때는 전환을 막는다 — silent가 true면(hover 미리보기) 안내 메시지 없이 조용히 무시한다.
    public void SetActivePiece(Piece piece, bool silent = false)
    {
        if (piece == null || piece.pieceDeck == null || piece == activePiece) return;

        if (isCardEffecting || nowusingCard != null)
        {
            if (!silent)
                AnnouncementUI.instance?.Show("카드 사용 중에는 기물을 전환할 수 없습니다");
            return;
        }

        board?.SetPieceDeckIndicator(activePiece, false);
        SetPileActive(activePiece, false);
        activePiece = piece;
        SetPileActive(activePiece, true);
        board?.SetPieceDeckIndicator(activePiece, true);

        SnapPilesToZones();
        UpdateCurrentEnergy();
        RefreshAllCardViews();
        NotifyPileChanged();
    }

    static void SetPileActive(Piece piece, bool active)
    {
        if (piece?.pieceDeck == null) return;
        foreach (var rt in piece.pieceDeck.Hand) rt.gameObject.SetActive(active);
        foreach (var rt in piece.pieceDeck.Discard) rt.gameObject.SetActive(active);
        foreach (var rt in piece.pieceDeck.Deck) rt.gameObject.SetActive(active);
        foreach (var rt in piece.pieceDeck.Exile) rt.gameObject.SetActive(active);
    }

    // 새로 활성화된 기물의 더미들을 각자의 Zone 위치로 즉시 스냅(애니메이션 없음)하고 손패는 부채꼴로 정렬
    void SnapPilesToZones()
    {
        foreach (var rt in Discardcards) rt.position = GetZonePosition(CardPositionZone.Discard);
        foreach (var rt in Deckcards) rt.position = GetZonePosition(CardPositionZone.Deck);
        foreach (var rt in Exilecards) rt.position = GetZonePosition(CardPositionZone.Exile);
        AlignCards();
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
        if (activePiece != null && activePiece.IsStunned())
        {
            AnnouncementUI.instance?.Show("기절 상태입니다");
            return false;
        }
        if ((isCardEffecting && nowusingCard == null) || TurnManager.instance.CurrentState != TurnState.Player)
            return false;
        if (!card.CanUse())
        {
            AnnouncementUI.instance?.Show(card.GetCannotUseReason());
            return false;
        }
        if (nowusingCard != null)
        {
            CancelCardMove(nowusingCard);
            board.CancelCardUsage();
        }
        pendingFirstTarget = new Vector2Int(-1, -1);
        RectTransform cardToUse = cards[handnum];
        cards.RemoveAt(handnum);
        ClearnowusingCard();
        nowusingCard = cardToUse;
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
        CommitNowUsingCard();
        return true;
    }

    // nowusingCard를 보드 사용(타겟팅 카드면 board.UseCard)과 NowUsing 위치 이동 애니메이션으로 커밋.
    // 최초 사용 시와, HandZone으로 되돌아와 재커밋할 때 공통으로 쓰인다.
    void CommitNowUsingCard()
    {
        nowUsingCardHeld = false;
        Card cardComp = nowusingCard.GetComponent<Card>();
        if (cardComp.NeedsTargeting() || cardComp.effects[0].pieceSelectCount > 0
            || cardComp.dragDropTarget == DragDropTarget.Self)
            board.UseCard(cardComp);
        if (cardComp.NeedsTargeting())
            CardDragArrow.instance?.Show(nowusingCard);
        board.SetCasterIndicator(activePiece, true); // 카드 종류 상관없이 들고 있는 동안 시전자 칸 표시
        usingCardMoving = true;
        RectTransform usingCard = nowusingCard;
        EnqueueMove(usingCard, GetZonePosition(CardPositionZone.NowUsing), Quaternion.identity, 0.35f, () =>
        {
            usingCardMoving = false;
            if (nowusingCard == null) return;
            if (pendingFirstTarget.x >= 0)
            {
                Vector2Int target = pendingFirstTarget;
                pendingFirstTarget = new Vector2Int(-1, -1);
                board.ButtonClicked(target);
            }
        });
    }

    // 이미 커밋된 nowusingCard를 매 드래그 프레임 처리. HandZone의 raycastTarget은 꺼진 채로 유지해야
    // 보드가 호버를 인식할 수 있으므로, hovered 목록이 아니라 HandZone의 RectTransform 영역으로 직접 판정한다.
    public void HandleCommittedCardDrag(Vector2 screenPos)
    {
        bool overHandZone = IsScreenPointInHandZone(screenPos);

        if (!nowUsingCardHeld)
        {
            if (!overHandZone)
                RevertNowUsingCardToHeld();
        }
        else
        {
            // NowUsing 위치에서 마우스로 날아오는 애니메이션이 끝난 뒤에만 직접 마우스를 따라가게 함
            if (!usingCardMoving)
                nowusingCard.position = screenPos;
            if (overHandZone)
                CommitNowUsingCard();
        }
    }

    bool IsScreenPointInHandZone(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(HandZone.GetComponent<RectTransform>(), screenPos, null);
    }

    // nowusingCard의 보드 사용 커밋을 취소하고, cards 리스트를 거치지 않고 NowUsing 위치에서 마우스로
    // 날아오는 애니메이션을 거쳐 마우스에 들린 상태로 되돌린다.
    void RevertNowUsingCardToHeld()
    {
        if (isCardEffecting || board.EffectApplied) return;

        pendingFirstTarget = new Vector2Int(-1, -1);
        board.CancelCardUsage(); // Board.UseCard가 한 일을 그대로 반대로 되돌림
        CardDragArrow.instance?.Hide();
        nowUsingCardHeld = true;

        usingCardMoving = true;
        EnqueueMove(nowusingCard, Mouse.current.position.ReadValue(), Quaternion.identity, 0.35f, () => usingCardMoving = false);
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

    // 턴 종료 시 아군 기물 전원의 손패를 각자의 버림더미로 보낸다. 활성 기물은 기존 애니메이션 경로를
    // 그대로 쓰고, 화면에 없는(비활성) 기물은 카드 오브젝트가 비활성화돼 있으므로 데이터만 즉시 옮긴다.
    public void ResetAllAllyHands(List<Piece> allies)
    {
        foreach (var piece in allies)
        {
            if (piece?.pieceDeck == null) continue;
            if (piece == activePiece)
                HandtoDiscardAll();
            else
            {
                piece.pieceDeck.Discard.AddRange(piece.pieceDeck.Hand);
                piece.pieceDeck.Hand.Clear();
            }
        }
        NotifyPileChanged();
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
    // 턴 시작 시 아군 기물 전원을 처리: 각자 5장 드로우. 활성 기물은 기존 애니메이션 경로
    // (DrawTurnStartCards)를 그대로 쓰고, 비활성 기물은 데이터만 즉시 갱신한다.
    // 에너지는 기물별이 아니라 아군 전체가 공유하므로 루프 밖에서 한 번만 최대치로 리셋한다.
    public void ProcessTurnStartForAllAllies(List<Piece> allies)
    {
        foreach (var piece in allies)
        {
            if (piece?.pieceDeck == null) continue;
            if (piece == activePiece)
                DrawTurnStartCards();
            else
                DrawCardsDataOnly(piece.pieceDeck, 5);
        }
        GetMaxEnergy();
        NotifyPileChanged();
    }

    // 비활성 기물용: 애니메이션 없이 덱에서 count장을 손패로 옮긴다. 덱이 부족하면 버림더미를 셔플해 보충.
    void DrawCardsDataOnly(PieceDeck pd, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (pd.Deck.Count == 0) ShuffleDiscardIntoDeckDataOnly(pd);
            if (pd.Deck.Count == 0) break;
            pd.Hand.Add(pd.Deck.Dequeue());
        }
    }

    void ShuffleDiscardIntoDeckDataOnly(PieceDeck pd)
    {
        if (pd.Discard.Count == 0) return;
        var shuffled = pd.Discard.OrderBy(_ => UnityEngine.Random.value).ToList();
        foreach (var card in shuffled) pd.Deck.Enqueue(card);
        pd.Discard.Clear();
    }

    public void RefreshAllCardViews()
    {
        if (board != null)
        {
            foreach (var piece in board.GetAllAllyPieces())
            {
                if (piece?.pieceDeck == null) continue;
                foreach (var rt in piece.pieceDeck.Hand.Concat(piece.pieceDeck.Discard).Concat(piece.pieceDeck.Deck))
                    rt.GetComponent<Card>()?.RefreshView();
            }
        }
        if (nowusingCard != null) nowusingCard.GetComponent<Card>()?.RefreshView();
        UpdateCardInteractability();
    }

    public void UpdateCardInteractability()
    {
        if (cardSelectionMode) return;
        bool playerTurn = TurnManager.instance != null && TurnManager.instance.CurrentState == TurnState.Player;
        bool boardProcessing = isCardEffecting && nowusingCard == null;
        bool stunned = activePiece != null && activePiece.IsStunned();
        foreach (var rt in cards)
        {
            Card card = rt.GetComponent<Card>();
            if (card == null) continue;
            bool canUse = playerTurn && !boardProcessing && !stunned && card.Cost <= currentenergy && card.CanUse();
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
        nowUsingCardHeld = false;
        int originalIndex = card.GetComponent<Card>().handNumber;
        cards.Insert(Mathf.Clamp(originalIndex, 0, cards.Count), card);
        AlignCards();
        pendingFirstTarget = new Vector2Int(-1, -1);
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

    // 덱에서 카드가 제거됐을 때 화면 중심에 잠깐 보여준 뒤 fade out시키는 연출용 카드.
    // 실제 손패/덱/버린 더미 풀에는 들어가지 않는 시각 효과 전용 인스턴스라 애니메이션이 끝나면 파괴한다.
    public void ShowRemovedCard(string cardname)
    {
        GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardname);
        if (obj == null) return;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.position = GetZonePosition(CardPositionZone.Center);
        rt.localRotation = Quaternion.identity;

        StartCoroutine(ShowRemovedCardRoutine(rt));
    }

    IEnumerator ShowRemovedCardRoutine(RectTransform rt)
    {
        yield return new WaitForSeconds(1f);

        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        const float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (cg != null) cg.alpha = 1f - elapsed / duration;
            yield return null;
        }
        Destroy(rt.gameObject);
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

    /// <summary>카드 선택 패널을 열어 플레이어가 카드를 선택하도록 합니다.
    /// zone이 SavedDeck이면 pieceIndex로 어느 기물의 deckCardIDs를 대상으로 할지 지정해야 합니다.</summary>
    public void ShowCardSelectionPanel(CardZone zone, int count, CardEffect effect, Action<List<RectTransform>> onConfirm, int pieceIndex = -1)
    {
        panelRequiredCount = count;
        panelCallback = onConfirm;
        panelIsSavedDeck = (zone == CardZone.SavedDeck);
        panelPieceIndex = pieceIndex;
        selectedInPanel.Clear();
        savedCardStates.Clear();

        panelCardPool.Clear();
        if (zone == CardZone.Hand   || zone == CardZone.Any) panelCardPool.AddRange(cards);
        if (zone == CardZone.Discard || zone == CardZone.Any) panelCardPool.AddRange(Discardcards);
        if (zone == CardZone.Deck   || zone == CardZone.Any) panelCardPool.AddRange(Deckcards);
        if (zone == CardZone.SavedDeck)
        {
            var pieces = DataManager.Instance.Pieces;
            List<string> ids = (pieceIndex >= 0 && pieceIndex < pieces.Count) ? pieces[pieceIndex].deckCardIDs : null;
            if (ids != null)
                foreach (string cardName in ids)
                {
                    GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardName);
                    if (obj != null) panelCardPool.Add(obj.GetComponent<RectTransform>());
                }
        }

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
        string verb;
        if (panelIsSavedDeck)
        {
            switch (panelSavedDeckAction)
            {
                case SavedDeckAction.Remove:
                    verb = "영구히 제거할";
                    break;
                default:
                    verb = "선택할";
                    break;
            }
        }
        else
        {
            switch (effect.type)
            {
                case EffectType.SelectAndDiscard:
                    verb = "버릴";
                    break;
                default:
                    verb = "코스트를 변경할";
                    break;
            }
        }
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

        if (panelIsSavedDeck)
        {
            ApplySavedDeckSelection(selected);
            panelCallback?.Invoke(selected);
            foreach (var card in panelCardPool)
                if (card != null) Destroy(card.gameObject);
            return;
        }

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

    // SavedDeck 패널 확인 시 호출: panelSavedDeckAction에 따라 선택된 카드를 처리한다.
    // panelCardPool은 deckCardIDs와 같은 순서로 스폰됐으므로 IndexOf가 곧 deckCardIDs상의 인덱스다.
    void ApplySavedDeckSelection(List<RectTransform> selected)
    {
        switch (panelSavedDeckAction)
        {
            case SavedDeckAction.Remove:
            {
                // 여러 장을 한 번에 선택했을 때 먼저 지운 항목이 뒤 인덱스를 당기지 않도록 큰 인덱스부터 제거
                var indices = selected.Select(card => panelCardPool.IndexOf(card))
                                       .Where(i => i >= 0)
                                       .OrderByDescending(i => i);
                foreach (int index in indices)
                    DataManager.Instance.RemoveCardFromDeck(panelPieceIndex, index);
                break;
            }
            // 나중에 SavedDeck 관련 다른 액션이 생기면 여기에 case 추가
        }
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
        if (board == null) return;

        foreach (var piece in board.GetAllAllyPieces())
        {
            if (piece?.pieceDeck == null) continue;

            var allCards = piece.pieceDeck.Hand
                .Concat(piece.pieceDeck.Discard)
                .Concat(piece.pieceDeck.Deck)
                .Select(rt => rt.GetComponent<Card>())
                .Where(c => c != null && c.originalCost >= 0 && c.costDuration == CostDuration.ThisTurnOnly);

            foreach (var card in allCards.ToList())
            {
                card.Cost = card.originalCost;
                card.originalCost = -1;
                card.RefreshView();
            }
        }
    }

    public void OnDragCardReleased(Vector2 screenPos)
    {
        if (nowusingCard == null) return;
        if (nowUsingCardHeld)
        {
            CancelCardUsage();
            return;
        }
        Card card = nowusingCard.GetComponent<Card>();
        // pieceSelectCount 카드는 board.UseCard()가 이미 픽업(CommitNowUsingCard) 시점에 호출됐고, 이후
        // 상호작용은 드롭 위치가 아니라 보드 클릭(HandlePieceSelectionClick)으로 진행되므로 여기선 아무것도 안 한다.
        if (card.effects[0].pieceSelectCount > 0)
            return;

        // 캐스터는 항상 카드를 낸 기물(activePiece)로 고정이므로, 어떤 카드든 실제로 뭔가 실행되기 전에
        // 제일 먼저 캐스터 상태 제약을 검사한다. self 타겟 카드는 캐스터가 확정되는 순간 바로 실행되기
        // 때문에, 이 체크가 그 뒤에 있으면 이미 늦는다 — 그래서 맨 앞으로 옮겨뒀다.
        if (!CheckCasterStatusRestrictions(card))
        {
            CancelCardUsage();
            return;
        }

        // 보드 밖에 놓으면(카드 종류 상관없이) 취소 — self/targeting/command 카드가 모두 공유하는 판정.
        Vector2Int boardPos = FindBoardPosAtScreen(screenPos);
        if (boardPos.x < 0)
        {
            CancelCardUsage();
            return;
        }

        if (!card.NeedsTargeting())
        {
            if (card.dragDropTarget == DragDropTarget.Self)
            {
                // CommitNowUsingCard에서 이미 board.UseCard()로 무장 완료 — 캐스터(카드 주인)만 확정하면 즉시 실행된다.
                board.ConfirmCasterOnDrop();
            }
            else if (board.boardmode == Board.BoardMode.Inspect)
            {
                board.UseCard(card);
            }
            return;
        }

        if (!board.IsValidDragTarget(boardPos, card.dragDropTarget))
        {
            string reason = card.dragDropTarget switch
            {
                DragDropTarget.Ally => "아군 기물에 사용해야 합니다",
                DragDropTarget.Enemy => "적 기물에 사용해야 합니다",
                DragDropTarget.AnyPiece => "기물이 있는 칸에 사용해야 합니다",
                _ => "올바른 위치가 아닙니다"
            };
            AnnouncementUI.instance?.Show(reason);
            CancelCardUsage();
            return;
        }

        board.ConfirmCasterOnDrop();
        if (nowusingCard == null) return;

        CardDragArrow.instance?.Hide();
        if (!usingCardMoving)
            board.ButtonClicked(boardPos);
        else
            pendingFirstTarget = boardPos;
    }

    // 캐스터는 항상 카드를 낸 기물(activePiece)이므로 드롭 위치와 무관하게 곧바로 검사할 수 있다.
    bool CheckCasterStatusRestrictions(Card card)
    {
        Piece caster = activePiece;
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

    Vector2Int FindBoardPosAtScreen(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var btn = hit.collider.GetComponentInParent<global::Button>();
            if (btn != null) return btn.GetLocation();
        }
        return new Vector2Int(-1, -1);
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