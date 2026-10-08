using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using DG.Tweening;

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
    [Tooltip("사용이 끝나 보드 연출을 기다리는 카드를 모아 두는 자리(씬 usedCardZone). 비워 두면 CardNowUsingPos를 쓴다.")]
    [SerializeField] RectTransform UsedCardPos;
    [SerializeField] RectTransform DiscardZone;
    [SerializeField] RectTransform DeckZone;
    [SerializeField] RectTransform ExileZone;
    public List<RectTransform> Exilecards => activePiece?.pieceDeck?.Exile ?? EmptyCardList;
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius;        // 부채꼴 반지름 (클수록 더 펼쳐짐)
    [SerializeField] float angleBetween;  // 카드 사이의 각도
    [SerializeField] float heightOffset;  // 부채꼴의 수직 위치 보정
    [Tooltip("호버·드래그 중인 손패 카드를 앞으로 꺼낼 때 쓰는 sortingOrder. MainCanvas(2)보다 크고 CardFxLayer(4)보다 " +
        "작게 둔다. 카드 선택 패널(3)과 같지만 패널이 열려 있을 땐 꺼내지 않으므로 겹치지 않는다.")]
    [SerializeField] int hoveredCardSortingOrder = 3;
    public int HoveredCardSortingOrder => hoveredCardSortingOrder;

    // ── 카드 선택 패널 ──────────────────────────────────────────
    // Unity Inspector에서 연결 필요:
    //   cardSelectionPanel  : 패널 최상위 GameObject (기본 비활성화)
    //   cardSelectionContent: 카드가 표시될 RectTransform (Content 영역)
    //   selectionCountText  : "0/2 선택됨" 표시 TextMeshProUGUI
    //   selectionPromptText : 안내 문구 TextMeshProUGUI
    //   confirmSelectionBtn : 확인 Button
    //   cancelSelectionBtn  : 취소 Button (선택) — allowCancel로 연 패널에서만 보인다. OnClick은 Awake에서 코드로 등록하므로 인스펙터에서 비워 둔다.
    [Header("카드 선택 패널")]
    [SerializeField] GameObject cardSelectionPanel;
    [SerializeField] RectTransform cardSelectionContent;
    [SerializeField] TextMeshProUGUI selectionCountText;
    [SerializeField] TextMeshProUGUI selectionPromptText;
    [SerializeField] UnityEngine.UI.Button confirmSelectionBtn;
    [SerializeField] UnityEngine.UI.Button cancelSelectionBtn;

    [Tooltip("세이브 덱 카드 획득/제거 연출(ShowAddedCard/ShowRemovedCard)용 카드를 띄울 부모(선택). " +
        "Override Sorting으로 MainCanvas보다 위에 그려지는 Canvas를 붙여 두면 상점 패널 등 MainCanvas UI에 가리지 않는다. " +
        "비워 두면 CardCanvas 바로 아래에 띄운다.")]
    [SerializeField] RectTransform cardFxLayer;
    RectTransform CardFxParent => cardFxLayer != null ? cardFxLayer : GetComponent<RectTransform>();

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
            int allyCount = board != null ? board.GetAllAllyPieces().Count(p => !p.isSummon) : 1;
            return baseMaxEnergy + Mathf.Max(0, allyCount - 1);
        }
    }
    public RectTransform nowusingCard;
    // "카드가 로직 처리 중"만 의미 — FinishUseCard에서 로직이 끝나는 즉시 false로 바뀐다(더 이상
    // 버림/소멸 더미로 날아가는 연출까지 기다리지 않음). 그 연출이 남아있는지는 pendingCardFlights로 추적.
    public bool isCardEffecting;
    public int pendingCardFlights;
    public bool HasPendingCardFlights => pendingCardFlights > 0;
    bool usingCardMoving;
    bool nowUsingCardHeld; // true면 nowusingCard가 보드 커밋 없이 마우스에 들려있는 상태
    List<RectTransform> pendingDrawCards = new List<RectTransform>();
    Coroutine batchDrawCoroutine;
    Vector2Int pendingFirstTarget = new Vector2Int(-1, -1);

    // 로직은 끝났지만(FinishUseCard) 그 카드가 유발한 보드 연출은 아직 재생 중이라 UsedCardPos 자리에서
    // 대기 중인 카드들. Board.FinishCardUsage가 enqueue하는 신호(OnUsedCardAnimationsComplete)가 이
    // 카드들이 실제로 쓰인 순서와 동일한 순서(motionQueue는 FIFO)로 도착하므로, 그냥 앞에서부터 꺼내면 된다.
    Queue<(RectTransform card, bool exile)> awaitingFlyOut = new Queue<(RectTransform, bool)>();
    // 대기 중인 카드가 둘 이상일 때 UsedCardPos 자리에 완전히 포개지지 않도록 한 장씩 살짝 비켜 세운다.
    static readonly Vector3 WaitingCardStackOffset = new Vector3(-28f, -20f, 0f);

    // ── 카드 이동 애니메이션 스케줄링 ────────────────────────────
    // 여러 카드가 동시에 EnqueueMove되어도 최소 0.15초 간격으로 순차 시작하되,
    // 일단 시작되면 각자 독립적인 DOTween 트윈으로 자기 duration만큼 재생된다.
    // nextMoveStartTime은 "다음 이동이 시작될 수 있는 가장 이른 시각"만 기록한다.
    const float MoveQueueGap = 0.15f;
    float nextMoveStartTime = 0f;
    // 카드별 "지금 돌고 있는 이동 Sequence" 핸들. Sequence.Join()으로 편입된 자식 트윈은
    // 타겟 기준 DOTween.Kill(card)로 안정적으로 못 찾을 수 있어(DOTween 공식 문서: sequenced 트윈은
    // 부모 Sequence를 통해서만 제어), 반드시 이 핸들로 직접 Kill해야 한다.
    Dictionary<RectTransform, Tween> activeCardMoves = new Dictionary<RectTransform, Tween>();
    // ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (instance == null) instance = this;
        // HandZone은 레이캐스트 대상이 아니라 사각형(IsScreenPointInHandZone)으로만 판정한다 — 켜 두면 보드 호버를 가린다.
        HandZone.GetComponent<Image>().raycastTarget = false;
        if (cancelSelectionBtn != null) cancelSelectionBtn.onClick.AddListener(CancelCardSelection);
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
    }

    public void CardUnSelected()
    {
        AlignCards();
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
        if ((isCardEffecting && nowusingCard == null) || !TurnManager.instance.IsPlayerActionable)
            return false;
        if (!card.CanUse())
        {
            AnnouncementUI.instance?.Show(card.GetCannotUseReason());
            return false;
        }
        if (!card.HasGraveForFirstEffect(activePiece))
        {
            AnnouncementUI.instance?.Show("무덤이 부족합니다");
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
        CommitNowUsingCard();
        return true;
    }

    // nowusingCard를 보드 사용(타겟팅 카드면 board.UseCard)과 NowUsing 위치 이동 애니메이션(타겟팅 카드만)으로 커밋.
    // 최초 사용 시와, HandZone으로 되돌아와 재커밋할 때 공통으로 쓰인다.
    void CommitNowUsingCard()
    {
        AudioManager.instance?.PlayCardCommit();
        nowUsingCardHeld = false;
        Card cardComp = nowusingCard.GetComponent<Card>();
        if (cardComp.NeedsTargeting() || cardComp.effects[0].pieceSelectCount > 0
            || cardComp.dragDropTarget == DragDropTarget.Self)
            board.UseCard(cardComp);
        if (cardComp.NeedsTargeting())
            CardDragArrow.instance?.Show(nowusingCard);
        board.SetCasterIndicator(activePiece, true); // 카드 종류 상관없이 들고 있는 동안 시전자 칸 표시
        if (!cardComp.NeedsTargeting()) return; // 타겟팅이 필요 없는 카드는 NowUsing으로 보내지 않고 마우스를 계속 따라가게 둔다
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
        }, bypassGap: true);
    }

    // 이미 커밋된 nowusingCard를 매 드래그 프레임 처리. HandZone 진입·이탈은 IsScreenPointInHandZone으로 판정한다.
    public void HandleCommittedCardDrag(Vector2 screenPos)
    {
        bool overHandZone = IsScreenPointInHandZone(screenPos);

        if (!nowUsingCardHeld)
        {
            if (!overHandZone)
                RevertNowUsingCardToHeld();
            else if (!nowusingCard.GetComponent<Card>().NeedsTargeting())
                nowusingCard.position = screenPos; // 타겟팅이 필요 없는 카드는 HandZone 안에서도 마우스 중앙을 따라간다
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

    // 포인터가 HandZone 사각형 안인지. 들고 있는 카드가 포인터 아래에서 레이캐스트를 막고 있어도 판정되도록
    // 레이캐스트가 아니라 RectTransform 영역으로 직접 계산한다(호출 시점의 크기·앵커를 그대로 따른다).
    public bool IsScreenPointInHandZone(Vector2 screenPos)
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

        // 타겟팅이 필요 없는 카드는 이미 마우스 아래 있으므로 날아오는 연출 없이 바로 따라간다.
        if (nowusingCard.GetComponent<Card>().NeedsTargeting())
        {
            usingCardMoving = true;
            EnqueueMove(nowusingCard, Mouse.current.position.ReadValue(), Quaternion.identity, 0.35f, () => usingCardMoving = false, bypassGap: true);
        }
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
        // 리스트 소속(로직)은 즉시 바뀌고, 화면상 위치는 EnqueueMove가 현재 위치에서 버림더미까지
        // 날아가는 트윈으로 나중에 따라잡는다(카드가 순간이동하지 않고 실제로 날아가 보이게).
        cards.Remove(card);
        Discardcards.Add(card);
        NotifyPileChanged();
        EnqueueMove(card, GetZonePosition(CardPositionZone.Discard), Quaternion.identity, 0.2f);
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

        foreach (var c in newCards) HoldForEntrance(c); // 정렬이 새 카드를 덱에서 미리 끌어가지 않게
        AlignCards(); // 기존 손패는 새 자리로 미끄러지고, 새 카드는 슬롯만 기억한다
        NotifyPileChanged();
        AudioManager.instance?.PlayCardDraw();

        foreach (var c in newCards)
        {
            c.position = GetZonePosition(CardPositionZone.Deck);
            c.localRotation = Quaternion.identity;
            EnqueueMoveToHand(c, 0.3f);
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
        bool playerTurn = TurnManager.instance != null && TurnManager.instance.IsPlayerActionable;
        bool boardProcessing = isCardEffecting && nowusingCard == null;
        bool stunned = activePiece != null && activePiece.IsStunned();
        foreach (var rt in cards)
        {
            Card card = rt.GetComponent<Card>();
            if (card == null) continue;
            bool canUse = playerTurn && !boardProcessing && !stunned && card.Cost <= currentenergy && card.CanUse()
                && card.HasGraveForFirstEffect(activePiece);
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

    // 카드 로직이 실제로 시작되는 시점(Board.ExecuteEffect, 첫 효과 적용 직전)에 미리 에너지를 차감한다.
    // OneUse 코스트 복구도 여기서 함께 처리 — "실제 사용된 코스트"를 정확히 기록해야 하므로. 예전엔
    // FinishUseCard(모든 효과가 끝난 뒤)에서 차감해서, 카드 자신의 효과가 자기 Cost를 바꾸면(예:
    // WarmUpDamageCard의 ReduceCost) 이번 사용분 차감액까지 그 할인이 반영돼버리는 문제가 있었다.
    public void DeductEnergyForCard(Card card)
    {
        int costToDeduct = card.Cost;
        if (card.originalCost >= 0 && card.costDuration == CostDuration.OneUse)
        {
            card.Cost = card.originalCost;
            card.originalCost = -1;
        }
        currentenergy -= costToDeduct;
    }

    public void FinishUseCard()             //사용한 카드 처리
    {
        CardDragArrow.instance?.Hide();
        if (nowusingCard)
        {
            Card card = nowusingCard.GetComponent<Card>();
            RectTransform usedCard = nowusingCard;
            usedCard.GetComponent<Card>().handNumber = -1;
            nowusingCard = null;
            // 카드 로직은 여기서 완전히 끝난다 — 이 시점부터 다음 카드를 집을 수 있어야 한다(카드 예약).
            // 하지만 이 카드가 보드에 일으킨 연출(공격/이동 애니메이션 등)은 아직 재생 중일 수 있으므로,
            // 버림/소멸 더미로 날아가는 건 그 연출이 실제로 끝났다는 신호(OnUsedCardAnimationsComplete)를
            // 받을 때까지 미룬다 — 그때까지는 UsedCardPos 자리에 다른 대기 카드들과 함께 정렬돼 머문다.
            isCardEffecting = false;
            bool exile = card.ShouldExileOnUse();
            if (exile) Exilecards.Add(usedCard);
            else { Discardcards.Add(usedCard); NotifyPileChanged(); }

            pendingCardFlights++;
            awaitingFlyOut.Enqueue((usedCard, exile));
            // NowUsing으로 가던 이동이 아래 정렬 이동에 끊기면 그 완료 콜백이 불리지 않으므로 여기서 직접 풀어 둔다
            // (안 풀면 다음 카드가 손에 들린 상태에서 마우스를 따라가지 못한다).
            usingCardMoving = false;
            ArrangeAwaitingCards();
        }
        else
        {
            isCardEffecting = false;
        }
        RefreshAllCardViews();
    }

    // Board.FinishCardUsage가 이 카드의 마지막 효과까지 유발한 연출이 motionQueue에서 전부 끝난
    // 시점에 호출한다(FIFO라서 awaitingFlyOut의 맨 앞 = 이 신호에 대응하는 카드). 그제서야 버림/소멸
    // 더미로 실제로 날아간다.
    public void OnUsedCardAnimationsComplete()
    {
        if (awaitingFlyOut.Count == 0) return;
        var (usedCard, exile) = awaitingFlyOut.Dequeue();
        if (usedCard == null) { pendingCardFlights--; ArrangeAwaitingCards(); return; }

        if (exile)
        {
            AudioManager.instance?.PlayCardExile();
            EnqueueMove(usedCard, GetZonePosition(CardPositionZone.Exile), Quaternion.identity, 0.25f, () => pendingCardFlights--);
        }
        else
        {
            AudioManager.instance?.PlayCardDiscard();
            EnqueueMove(usedCard, GetZonePosition(CardPositionZone.Discard), Quaternion.identity, 0.15f, () => pendingCardFlights--);
        }
        ArrangeAwaitingCards(); // 남은 대기 카드를 한 칸씩 앞으로 당긴다
    }

    // 연출 대기 중인 카드(awaitingFlyOut)를 UsedCardPos 자리에 큐 순서대로 겹쳐 세운다(먼저 끝날 카드일수록
    // 기준점에 더 가깝게). 대기 목록이 바뀔 때마다(FinishUseCard로 들어올 때, OnUsedCardAnimationsComplete로
    // 나갈 때) 다시 불러서, 새로 도착한 카드가 남아 있는 카드와 같은 칸에 겹치지 않게 한다.
    // 조준 중인 카드는 CardNowUsingPos에 따로 있으므로 그 자리를 비워 둘 필요가 없다.
    // 그리는 순서도 큐 순서에 맞춘다 — 카드를 집을 때 SetAsLastSibling이 걸려 나중에 쓴 카드일수록 위에 그려지므로,
    // 그대로 두면 먼저 쓴(지금 연출 중이고 곧 날아갈) 카드가 뒤 카드에 가려진다.
    void ArrangeAwaitingCards()
    {
        Vector3 baseline = (UsedCardPos != null ? UsedCardPos : CardNowUsingPos).position;
        var waiting = awaitingFlyOut.ToArray();
        for (int i = 0; i < waiting.Length; i++)
        {
            if (waiting[i].card != null)
                EnqueueMove(waiting[i].card, baseline + WaitingCardStackOffset * i, Quaternion.identity, 0.2f, null, bypassGap: true);
        }
        // 최신 카드부터 맨 뒤 형제로 보내, 먼저 쓴 카드가 맨 위에 그려지게 한다.
        for (int i = waiting.Length - 1; i >= 0; i--)
        {
            if (waiting[i].card != null)
                waiting[i].card.SetAsLastSibling();
        }
    }

    // card를 pos/rot으로 이동시키는 DOTween 트윈을 만든다. 동시에 여러 장이 EnqueueMove되면
    // nextMoveStartTime이 최소 MoveQueueGap 간격으로 시작 시각을 예약해준다(한 번 예약되면
    // 그 카드의 이동이 취소돼도 뒤에 예약된 카드의 시작 시각은 당겨지지 않음).
    // bypassGap: 카드 예약으로 여러 장의 버림/소멸 연출이 밀려 있을 때, 지금 플레이어가 직접 조작 중인
    // 카드(NowUsing 진입/되돌아오기)까지 그 간격에 밀리면 안 되므로 그런 호출에서만 true로 넘긴다.
    void EnqueueMove(RectTransform card, Vector3 pos, Quaternion rot, float duration, Action onComplete = null, bool bypassGap = false)
    {
        StartCardMove(card, DOTween.Sequence()
            .Join(card.DOMove(pos, duration).SetEase(Ease.OutCubic))
            .Join(card.DOLocalRotateQuaternion(rot, duration).SetEase(Ease.OutCubic)), onComplete, bypassGap);
    }

    // 손패로 들어오는 등장 연출(드로우·손패 추가). 목표는 카드의 현재 슬롯을 매 프레임 따라간다(Card.HandSlotTween).
    void EnqueueMoveToHand(RectTransform card, float duration)
    {
        StartCardMove(card, card.GetComponent<Card>().HandSlotTween(duration), null, false);
    }

    // EnqueueMove/EnqueueMoveToHand 공통부 — 이전 이동을 끊고 간격 예약을 거쳐 move를 재생한다. 끝나면 손패 카드는
    // 지금 있어야 할 자세(슬롯·호버)로 마저 정착한다(날아오는 사이 정렬·호버가 바뀌었을 수 있음).
    void StartCardMove(RectTransform card, Tween move, Action onComplete, bool bypassGap)
    {
        CancelCardMove(card);
        card.GetComponent<Card>()?.CancelHoverPose(); // 손패 자세 트윈이 이동 트윈과 위치·회전을 다투지 않게

        float delay = bypassGap ? 0f : Mathf.Max(0f, nextMoveStartTime - Time.time);
        if (!bypassGap)
            nextMoveStartTime = Time.time + delay + MoveQueueGap;

        activeCardMoves[card] = DOTween.Sequence()
            .SetDelay(delay)
            .Append(move)
            .OnComplete(() =>
            {
                activeCardMoves.Remove(card);
                if (cards.Contains(card)) card.GetComponent<Card>().SettleToHandPose();
                onComplete?.Invoke();
            });
    }

    // 이동 연출이 걸려 있는지(딜레이 대기·등장 대기 포함). 정렬이 날아가는 카드를 끌어가거나 호버 자세를 걸지 않는 데 쓴다.
    public bool IsCardMoving(RectTransform card) => activeCardMoves.ContainsKey(card);

    // 등장 연출을 기다리는 카드(motionQueue 대기·화면 중앙 1초 연출) — 정렬이 미리 손패로 끌어가거나 호버 자세가
    // 걸리지 않게 이동 중으로 취급한다. 실제 이동이 시작되면 CancelCardMove가 이 자리표시(null)를 지운다.
    void HoldForEntrance(RectTransform card)
    {
        CancelCardMove(card);
        card.GetComponent<Card>()?.CancelHoverPose();
        activeCardMoves[card] = null;
    }

    // 대기 중이든(딜레이 구간) 실행 중이든, 이 카드에 걸린 이동 Sequence를 핸들로 직접 죽인다.
    // DOTween.Kill(card)(타겟 기준 검색)는 Join()으로 Sequence에 편입된 자식 트윈을 못 찾을 수 있어 쓰지 않는다.
    void CancelCardMove(RectTransform card)
    {
        if (activeCardMoves.TryGetValue(card, out Tween t) && t != null && t.IsActive())
            t.Kill();
        activeCardMoves.Remove(card);
    }

    // 순수 비주얼 이동 코루틴 — Piece*Cor(Board.Animation.cs)와 같은 패턴으로, 로직(리스트 소속)은
    // 전혀 건드리지 않고 DOTween으로 위치/회전만 옮기고 완료까지 기다린다. Board.motionQueue에 직접
    // enqueue돼서, 같은 카드효과가 유발한 다른 보드 애니메이션과 순서를 맞춘다(예: ShieldCycleCard의
    // 실드 연출이 끝난 뒤에야 카드가 덱으로 날아가는 연출이 시작됨) — EnqueueMove의 간격 스케줄링과는
    // 무관하게, motionQueue의 선입선출 순서로만 재생 시점이 결정된다.
    public IEnumerator MoveCardVisualCor(RectTransform card, Vector3 pos, Quaternion rot, float duration)
    {
        if (card == null) yield break;
        CancelCardMove(card);
        card.GetComponent<Card>()?.CancelHoverPose(); // 손패 자세 트윈을 버리고 현재 위치에서 날아간다
        yield return TrackCardMoveCor(card, DOTween.Sequence()
            .Join(card.DOMove(pos, duration).SetEase(Ease.OutCubic))
            .Join(card.DOLocalRotateQuaternion(rot, duration).SetEase(Ease.OutCubic)));
    }

    // motionQueue용 손패 등장 연출(배치 드로우). 목표는 카드의 현재 슬롯을 따라간다. 기다리는 사이 카드가 손패를
    // 떠났으면 그 이동이 카드를 맡고 있으므로 건너뛴다.
    IEnumerator MoveCardToHandCor(RectTransform card, float duration)
    {
        if (card == null || !cards.Contains(card)) yield break;
        CancelCardMove(card);
        Card cardComp = card.GetComponent<Card>();
        cardComp.CancelHoverPose();
        yield return TrackCardMoveCor(card, cardComp.HandSlotTween(duration));
    }

    // 코루틴 이동의 등록·대기·정리. 기다리는 사이 다른 이동이 이 카드를 맡았다면(기록이 바뀜) 그 기록을 지우지
    // 않는다 — 지우면 새 이동이 "이동 중 아님"으로 보여 정렬 트윈과 동시에 카드를 움직이게 된다.
    IEnumerator TrackCardMoveCor(RectTransform card, Tween t)
    {
        activeCardMoves[card] = t;
        yield return t.WaitForCompletion();
        if (activeCardMoves.TryGetValue(card, out Tween current) && current == t)
        {
            activeCardMoves.Remove(card);
            if (cards.Contains(card)) card.GetComponent<Card>().SettleToHandPose();
        }
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
            HoldForEntrance(newCard); // 등장 연출(BatchDrawAnim → motionQueue)이 시작되기 전까지 정렬이 끌어가지 않게
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

        AlignCards();  // 최종 손패 크기 기준으로 한 번만 정렬 — 새 카드는 DrawCard에서 등장 대기로 걸어 둬 슬롯만 기억한다
        AudioManager.instance?.PlayCardDraw();

        foreach (var c in toDraw)
        {
            c.position = GetZonePosition(CardPositionZone.Deck);
            c.localRotation = Quaternion.identity;
            // motionQueue로 넘겨서, 같은 카드효과 체인에서 먼저 재생 중인 보드 애니메이션(예: 이동)이
            // 끝난 뒤에야 드로우 연출이 시작되게 한다(MoveAndDrawCard 등).
            board.EnqueueBoardAnimation(MoveCardToHandCor(c, 0.3f));
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
    // 리스트 소속(로직)은 여기서 즉시 정해지고, 화면상 이동은 이후 애니메이션(EnqueueMove)이 따라잡는다.
    // Hand로 추가하는 경우도 마찬가지 — DrawCard/DrawTurnStartCards와 같은 방식으로, 화면 중앙에서 이 카드의
    // 부채꼴 슬롯(날아오는 사이 다시 정렬되면 바뀐 슬롯)까지 날아가는 걸로 보이게 한다.
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
            HoldForEntrance(rt); // 화면 중앙에서 1초 보여주는 동안 정렬이 끌어가지 않게
            AlignCards(); // 기존 손패는 새 자리로 미끄러지고, 이 카드는 슬롯만 기억한다
            NotifyPileChanged();
            StartCoroutine(ShowAddedCardToHandRoutine(rt));
            return;
        }

        if (targetZone == CardPositionZone.Deck)
            Deckcards.Enqueue(rt);
        else
            Discardcards.Add(rt);
        NotifyPileChanged();

        StartCoroutine(ShowAddedCardRoutine(rt, GetZonePosition(targetZone)));
    }

    IEnumerator ShowAddedCardToHandRoutine(RectTransform rt)
    {
        yield return new WaitForSeconds(1f);
        // 기다리는 사이 손패를 떠났으면 그 이동이 카드를 맡고 있으므로 건드리지 않는다.
        if (cards.Contains(rt))
            EnqueueMoveToHand(rt, 0.3f);
    }

    // 덱에 카드가 추가됐을 때 화면 중심에 잠깐 보여준 뒤 targetZone 위치로 이동시키는 연출용 카드.
    // 실제 손패/덱/버린 더미 풀에는 들어가지 않는 시각 효과 전용 인스턴스라 애니메이션이 끝나면 파괴한다.
    public void ShowAddedCard(string cardname, CardPositionZone targetZone)
    {
        GameObject obj = cardData.SpawnCard(CardFxParent, cardname);
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
        GameObject obj = cardData.SpawnCard(CardFxParent, cardname);
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

    // 아래 네 메서드는 로직(리스트 소속 변경)만 즉시 수행하고, 실제로 화면에서 카드를 옮기는 연출은
    // 하지 않는다 — 대신 "이동해야 할 (카드, 목표 위치, 목표 회전)" 목록을 돌려주면, 호출부
    // (Board.CardEffect.cs)가 그 목록을 MoveCardVisualCor로 감싸 motionQueue에 직접 enqueue해서
    // 같은 카드효과가 유발한 다른 보드 애니메이션과 순서를 맞춘다.
    public List<(RectTransform card, Vector3 pos, Quaternion rot)> HandtoDiscardCount(int count)
    {
        var moves = new List<(RectTransform, Vector3, Quaternion)>();
        foreach (var card in PickRandomCardsFromHand(count))
        {
            cards.Remove(card);
            Discardcards.Add(card);
            moves.Add((card, GetZonePosition(CardPositionZone.Discard), Quaternion.identity));
        }
        RefreshHandIndices();
        NotifyPileChanged();
        return moves;
    }

    public List<(RectTransform card, Vector3 pos, Quaternion rot)> HandtoDeckCount(int count)
    {
        var moves = new List<(RectTransform, Vector3, Quaternion)>();
        foreach (var card in PickRandomCardsFromHand(count))
        {
            cards.Remove(card);
            Deckcards.Enqueue(card);
            moves.Add((card, GetZonePosition(CardPositionZone.Deck), Quaternion.identity));
        }
        var list = Deckcards.ToList().OrderBy(_ => UnityEngine.Random.value).ToList();
        Deckcards = new Queue<RectTransform>(list);
        RefreshHandIndices();
        NotifyPileChanged();
        return moves;
    }

    public List<(RectTransform card, Vector3 pos, Quaternion rot)> HandtoExileCount(int count)
    {
        var moves = new List<(RectTransform, Vector3, Quaternion)>();
        foreach (var card in PickRandomCardsFromHand(count))
        {
            cards.Remove(card);
            Exilecards.Add(card);
            moves.Add((card, GetZonePosition(CardPositionZone.Exile), Quaternion.identity));
        }
        RefreshHandIndices();
        NotifyPileChanged();
        return moves;
    }

    public List<(RectTransform card, Vector3 pos, Quaternion rot)> HandtoDeckTop(int count)
    {
        var toReturn = PickRandomCardsFromHand(count);
        foreach (var card in toReturn)
            cards.Remove(card);
        var newDeck = toReturn.Concat(Deckcards.ToList()).ToList();
        Deckcards = new Queue<RectTransform>(newDeck);
        RefreshHandIndices();
        NotifyPileChanged();
        return toReturn.Select(card => (card, GetZonePosition(CardPositionZone.Deck), Quaternion.identity)).ToList();
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
    /// zone이 SavedDeck이면 pieceIndex로 어느 기물의 deckCardIDs를 대상으로 할지 지정해야 합니다.
    /// allowCancel이면 취소 버튼이 보이고, 취소 시 아무것도 처리하지 않은 채 onConfirm이 빈 목록으로 호출됩니다.</summary>
    public void ShowCardSelectionPanel(CardZone zone, int count, CardEffect effect, Action<List<RectTransform>> onConfirm, int pieceIndex = -1, bool allowCancel = false)
    {
        AudioManager.instance?.PlayPanelOpen();
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
        if (cancelSelectionBtn != null) cancelSelectionBtn.gameObject.SetActive(allowCancel);

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
            // 손패에서 걸린 호버 자세나 진행 중이던 이동을 패널로 끌고 오지 않게 끊고, 패널 자리를 슬롯으로 기억시켜
            // 즉시 놓는다(패널 안 호버도 이 자리 기준).
            CancelCardMove(panelCardPool[i]);
            Card panelCard = panelCardPool[i].GetComponent<Card>();
            panelCard.CancelHoverPose();
            panelCard.SetHandPose(new Vector3(-totalWidth / 2f + i * spacing, 0f, 0f), Quaternion.identity, snap: true);
            panelCard.ScaleDefault();
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
        CloseCardSelection(new List<RectTransform>(selectedInPanel));
    }

    // 취소 버튼(allowCancel로 연 패널에서만 보임)에서 호출: 선택한 카드를 처리하지 않고 콜백을 빈 목록으로 부른다.
    // 빈 목록으로 닫으면 ApplySavedDeckSelection도 아무것도 지우지 않는다.
    void CancelCardSelection()
    {
        AudioManager.instance?.PlayButtonClick();
        CloseCardSelection(new List<RectTransform>());
    }

    // 확인/취소 공용 닫기. selected만 처리 대상으로 넘기고, 패널 카드는 SavedDeck이면 파괴, 아니면 원래 자리로 되돌린다.
    void CloseCardSelection(List<RectTransform> selected)
    {
        if (!cardSelectionMode) return; // 버튼 중복 클릭 등으로 이미 닫힌 패널을 다시 닫지 않게
        cardSelectionMode = false;
        cardSelectionPanel.SetActive(false);

        // 콜백이 곧바로 다른 선택 패널을 열 수 있으므로 호출 전에 비워 둔다.
        var callback = panelCallback;
        panelCallback = null;

        if (panelIsSavedDeck)
        {
            ApplySavedDeckSelection(selected);
            callback?.Invoke(selected);
            foreach (var card in panelCardPool)
                if (card != null) Destroy(card.gameObject);
            return;
        }

        foreach (var card in panelCardPool)
        {
            var (originalParent, worldPos) = savedCardStates[card];
            card.GetComponent<Card>()?.CancelHoverPose(); // 패널에서 걸린 호버 자세를 손패로 끌고 오지 않게
            card.SetParent(originalParent, true);
            card.position = worldPos;
            card.localRotation = Quaternion.identity;
            card.GetComponent<Card>()?.ScaleDefault();
        }

        AlignCards(); // 손패 아크 레이아웃 복구

        callback?.Invoke(selected);
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

        if (!board.IsValidDropPos(boardPos))
        {
            AnnouncementUI.instance?.Show("사거리 밖입니다");
            CancelCardUsage();
            return;
        }

        AudioManager.instance?.PlayCardTargetConfirm();
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

            Card cardComp = cards[i].GetComponent<Card>();
            cardComp.SetHandPose(pos, rot); // 호버 중인 카드는 새 슬롯 기준으로 호버 자세(리프트·똑바로)를 유지
            cardComp.OnUnSelected -= CardUnSelected;
            cardComp.OnUnSelected += CardUnSelected;
            cardComp.cardCanvas = gameObject;
            cardComp.handNumber = i;
        }
        UpdateCardInteractability();
    }

    // AlignCards 중 handNumber/형제 인덱스 갱신만 즉시 수행 — 위치 스냅은 하지 않는다. 카드효과로
    // cards 리스트가 바뀌는 순간 handNumber도 같이 갱신돼야, 이후 위치 재정렬(AlignCards)이 애니메이션
    // 뒤로 미뤄진 동안 다른 손패 카드를 클릭해도 엉뚱한 카드가 선택되지 않는다(Card.MouseDown/MouseDrag가
    // handNumber를 cards의 인덱스로 그대로 사용함).
    void RefreshHandIndices()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card cardComp = cards[i].GetComponent<Card>();
            cardComp.OnUnSelected -= CardUnSelected;
            cardComp.OnUnSelected += CardUnSelected;
            cardComp.cardCanvas = gameObject;
            cardComp.handNumber = i;
        }
        UpdateCardInteractability();
    }

    // motionQueue에 enqueue하기 위한 래퍼 — AlignCards() 자체는 그대로 두고, 호출 "시점"만 이 코루틴이
    // 큐에서 dequeue되는 순간(=앞서 큐에 들어간 카드 애니메이션들이 다 끝난 직후)으로 옮긴다.
    public IEnumerator AlignCardsCor()
    {
        AlignCards();
        yield break;
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
            card.GetComponent<Card>().SetHandPose(pos, rot);
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