using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// 상점 오브젝트를 클릭했을 때 ButtonInfo 대신 아래에서 올라오는 애니메이션과 함께 나타나는 상점 화면.
// 보유 기물들의 직업 카드 풀에서 카드 8장(가로 4 x 세로 2, 희귀 1장 이상 보장)과 아직 없는 유물을 진열하고,
// 우측 하단 버튼으로 카드 제거도 할 수 있다. 재고는 레벨(씬) 로드 후 처음 열 때 한 번만 만들어서
// 닫았다 다시 열어도 그대로이고, 산 물건은 "매진"으로 남는다. 골드는 구매 대상이 확정되는 순간에만 빠진다.
public class ShopCanvas : MonoBehaviour
{
    public static ShopCanvas instance;

    [Tooltip("실제로 슬라이드되는 패널. 카드 슬롯 컨테이너/카드 제거 버튼이 전부 이 안에 있어야 한다.")]
    [SerializeField] RectTransform panelRoot;

    [Tooltip("ShopCardSlot 프리팹. 카드 부분(실제 Card 스폰)과 텍스트 부분(가격 등)으로 나뉜다.")]
    [SerializeField] GameObject cardSlotPrefab;

    [Tooltip("cardSlotPrefab이 스폰될 부모. Grid Layout Group이 붙어 있어 스폰만 하면 자동 정렬된다.")]
    [SerializeField] RectTransform slotContainer;

    [SerializeField] int slotCount = 8;

    [Header("카드 가격 (희귀도별 기본가, 진열 시 ±priceVariance 비율로 무작위 변동)")]
    [SerializeField] int commonPrice = 35;
    [SerializeField] int uncommonPrice = 50;
    [SerializeField] int rarePrice = 70;
    [Range(0f, 0.5f)]
    [SerializeField] float priceVariance = 0.1f;

    [Header("카드 제거 (가격 = removeBasePrice + removePriceStep × 런 전체 제거 횟수)")]
    [SerializeField] int removeBasePrice = 50;
    [SerializeField] int removePriceStep = 25;
    [Tooltip("상점 방문 1회당 제거 가능 횟수. 0이면 무제한")]
    [SerializeField] int removeLimitPerVisit = 1;

    [Tooltip("카드 제거 버튼(선택). 연결하면 가격/한도에 따라 활성 상태가 갱신된다. 버튼의 OnClick(OnClickRemoveCard)은 그대로 둔다.")]
    [SerializeField] UnityEngine.UI.Button removeButton;

    [Tooltip("카드 제거 버튼의 라벨(선택). \"카드 제거 50G\" / \"제거 완료\"로 덮어쓴다.")]
    [SerializeField] TextMeshProUGUI removeButtonLabel;

    [Header("유물")]
    [Tooltip("ShopRelicSlot 프리팹. ShopCardSlot과 동일한 구조(아이콘 부분 + 텍스트 부분).")]
    [SerializeField] GameObject relicSlotPrefab;

    [Tooltip("relicSlotPrefab이 스폰될 부모. Grid Layout Group이 붙어 있어 스폰만 하면 자동 정렬된다.")]
    [SerializeField] RectTransform relicSlotContainer;

    [SerializeField] int relicSlotCount = 4;

    [Tooltip("유물 1개 구매 가격")]
    [SerializeField] int relicBasePrice = 90;

    [SerializeField] float slideDuration = 0.35f;

    static readonly Color UnaffordableColor = new Color(1f, 0.35f, 0.35f);

    Vector2 shownPos;
    Vector2 hiddenPos;
    Coroutine slideRoutine;
    Color removeLabelDefaultColor = Color.white;

    bool stocked;              // 이번 방문(씬)의 재고를 이미 만들었는지
    int removalsThisVisit;     // 이번 방문에서 카드 제거를 한 횟수
    // 구매/제거가 기물 선택이나 카드 선택 패널을 기다리는 중이면 true. 그 사이 다른 구매로 잔액이 바뀌어
    // "확인할 때는 충분했는데 차감할 때는 부족한" 상황이 생기지 않도록 다른 상점 조작을 막는다.
    bool interactionPending;

    readonly List<ShopCardSlot> cardSlots = new List<ShopCardSlot>();
    readonly List<ShopRelicSlot> relicSlots = new List<ShopRelicSlot>();

    void Awake()
    {
        if (instance == null) instance = this;

        shownPos = panelRoot.anchoredPosition;
        hiddenPos = shownPos - new Vector2(0f, panelRoot.rect.height);
        panelRoot.anchoredPosition = hiddenPos;
        panelRoot.gameObject.SetActive(false);

        if (removeButtonLabel != null) removeLabelDefaultColor = removeButtonLabel.color;
    }

    // DataManager는 DontDestroyOnLoad라 씬 오브젝트인 ShopCanvas가 파괴될 때 반드시 구독을 해제한다.
    void Start()
    {
        if (DataManager.Instance != null) DataManager.Instance.GoldChanged += OnGoldChanged;
    }

    void OnDestroy()
    {
        if (DataManager.Instance != null) DataManager.Instance.GoldChanged -= OnGoldChanged;
    }

    public void Show()
    {
        AudioManager.instance?.PlayPanelOpen();
        Board.instance?.HideButtonInfoForShop();

        // 레벨마다 씬을 다시 로드하므로 "씬에서 처음 열 때 한 번"이 곧 "상점 방문당 한 번"이다.
        if (!stocked)
        {
            SpawnShopCards();
            SpawnShopRelics();
            stocked = true;
        }
        RefreshPurchasability();

        panelRoot.gameObject.SetActive(true);
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        panelRoot.anchoredPosition = hiddenPos;
        slideRoutine = StartCoroutine(Slide(hiddenPos, shownPos));
    }

    public void Hide()
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideOutThenDeactivate());
    }

    IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            panelRoot.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        panelRoot.anchoredPosition = to;
    }

    // 슬롯은 파괴하지 않는다 — panelRoot 아래에 있어 패널과 함께 숨고, 다시 열면 같은 재고(매진 포함)가 보인다.
    IEnumerator SlideOutThenDeactivate()
    {
        yield return Slide(panelRoot.anchoredPosition, hiddenPos);
        panelRoot.gameObject.SetActive(false);
    }

    // ────────── 진열 ──────────

    void SpawnShopCards()
    {
        ICardDatabase cardDb = CardDatabase.instance;
        foreach (string cardName in PickShopCards(slotCount))
        {
            GameObject slotObj = Instantiate(cardSlotPrefab, slotContainer);
            ShopCardSlot slot = slotObj.GetComponent<ShopCardSlot>();
            if (slot == null) { Destroy(slotObj); continue; }

            CardRarity rarity = cardDb.GetRarity(cardName);
            slot.SetCard(cardName, BuyCard);
            slot.SetPrice(RollPrice(BasePriceFor(rarity)), RarityLabelPrefix(rarity));
            cardSlots.Add(slot);
        }
    }

    // 보유 기물들의 직업 카드 풀(시작 카드 제외)에서 count장을 뽑는다. 풀에 희귀 카드가 있으면 그중 1장을 먼저
    // 확정하고, 나머지는 희귀도 구분 없이 균등하게 뽑는다(희귀가 더 나올 수도 있다). 희귀 카드가 항상 같은 자리에
    // 오지 않도록 진열 순서를 섞는다.
    List<string> PickShopCards(int count)
    {
        if (count <= 0) return new List<string>();

        ICardDatabase cardDb = CardDatabase.instance;
        List<string> pool = BuildShopCardPool();
        if (pool.Count == 0)
            Debug.LogWarning("[ShopCanvas] 보유 기물의 직업 카드 풀이 비어 있어 진열할 카드가 없습니다.");

        List<string> picks = cardDb.PickRandomDistinctFrom(1, pool.Where(c => cardDb.GetRarity(c) == CardRarity.Rare));
        picks.AddRange(cardDb.PickRandomDistinctFrom(count - picks.Count, pool, picks));
        Shuffle(picks);
        return picks;
    }

    // 보유 기물들의 직업 보상 풀 합집합에서 시작 카드를 뺀 목록. 두 직업 풀에 같은 카드가 있어도 한 번만 넣는다
    // (PickRandomDistinctFrom은 입력의 중복을 걸러주지 않아 같은 카드가 두 칸에 진열될 수 있다).
    static List<string> BuildShopCardPool()
    {
        var pool = new List<string>();
        var seen = new HashSet<string>(CardDatabase.StarterCardNames);
        foreach (PieceData piece in DataManager.Instance.Pieces)
        {
            List<string> jobPool = GetRewardCardPool(piece.pieceName);
            if (jobPool == null) continue;
            foreach (string cardName in jobPool)
                if (seen.Add(cardName)) pool.Add(cardName);
        }
        return pool;
    }

    void SpawnShopRelics()
    {
        IRelicDatabase relicDb = RelicDatabase.instance;
        // 이미 가진 유물은 진열하지 않는다(같은 유물 중복 보유 방지).
        foreach (string relicName in relicDb.PickRandomDistinct(relicSlotCount, DataManager.Instance.OwnedRelicNames))
        {
            GameObject slotObj = Instantiate(relicSlotPrefab, relicSlotContainer);
            ShopRelicSlot slot = slotObj.GetComponent<ShopRelicSlot>();
            if (slot == null) { Destroy(slotObj); continue; }

            slot.SetRelic(relicName, BuyRelic);
            slot.SetPrice(relicBasePrice);
            relicSlots.Add(slot);
        }
    }

    int BasePriceFor(CardRarity rarity) => rarity switch
    {
        CardRarity.Rare => rarePrice,
        CardRarity.Uncommon => uncommonPrice,
        _ => commonPrice,
    };

    static string RarityLabelPrefix(CardRarity rarity) => rarity switch
    {
        CardRarity.Rare => "[희귀] ",
        CardRarity.Uncommon => "[고급] ",
        _ => "",
    };

    int RollPrice(int basePrice) =>
        Mathf.Max(1, Mathf.RoundToInt(basePrice * Random.Range(1f - priceVariance, 1f + priceVariance)));

    static void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static List<string> GetRewardCardPool(string pieceName) =>
        PieceDatabase.instance != null ? PieceDatabase.instance.GetRewardCardPool(pieceName) : null;

    // ────────── 구매 ──────────

    // 카드 슬롯 클릭 시 호출: 그 카드를 직업 풀에 가진 기물 중 받을 기물을 고르고(1명이면 자동), 고른 순간
    // 골드를 차감해 그 기물의 덱에 영구히 추가한다. 기물 선택을 취소하면 골드는 그대로다.
    void BuyCard(ShopCardSlot slot)
    {
        if (interactionPending || slot.SoldOut) return;
        if (DataManager.Instance.Gold < slot.Price)
        {
            AnnouncementUI.instance?.Show("골드가 부족합니다");
            return;
        }

        List<int> candidates = FindPieces(p => GetRewardCardPool(p.pieceName)?.Contains(slot.CardName) == true);
        // 풀 정보를 못 찾는 예외 상황(로스터 변경 등)에도 구매 자체는 막히지 않게 전체 기물로 폴백
        if (candidates.Count == 0) candidates = FindPieces(_ => true);
        if (candidates.Count == 0) return;

        PickPiece(candidates, "카드를 받을 기물을 선택하세요", pieceIndex =>
        {
            if (!DataManager.Instance.SpendGold(slot.Price))
            {
                AnnouncementUI.instance?.Show("골드가 부족합니다");
                return;
            }
            DataManager.Instance.AddCardToPieceDeck(pieceIndex, slot.CardName);
            AudioManager.instance?.PlayShopPurchase();
            slot.MarkSoldOut();
        });
    }

    // 유물 슬롯 클릭 시 호출: 가격만큼 골드를 차감한 뒤 소유 유물 목록에 영구히 추가하고 매진 처리한다.
    // 세이브에 반영한 뒤 Board.LoadOwnedRelics를 재사용해 파티 아이콘 바를 즉시 다시 그린다.
    void BuyRelic(ShopRelicSlot slot)
    {
        if (interactionPending || slot.SoldOut) return;
        if (!DataManager.Instance.SpendGold(slot.Price))
        {
            AnnouncementUI.instance?.Show("골드가 부족합니다");
            return;
        }

        DataManager.Instance.AddRelic(slot.RelicName);
        AudioManager.instance?.PlayShopPurchase();
        slot.MarkSoldOut();
        Board.instance?.LoadOwnedRelics();
    }

    // ────────── 카드 제거 ──────────

    int CurrentRemovePrice => removeBasePrice + removePriceStep * DataManager.Instance.CardRemoveCount;
    bool RemoveLimitReached => removeLimitPerVisit > 0 && removalsThisVisit >= removeLimitPerVisit;

    // 우측 하단 "카드 제거" 버튼의 OnClick에 연결. 기물 선택(덱이 빈 기물 제외) → 그 기물의 덱에서 카드 1장 선택 →
    // 확정 시 골드 차감과 함께 영구 제거. 어느 단계에서 취소해도 골드와 덱은 그대로다.
    public void OnClickRemoveCard()
    {
        if (interactionPending) return;
        AudioManager.instance?.PlayButtonClick();

        if (RemoveLimitReached)
        {
            AnnouncementUI.instance?.Show("이번 상점에서는 더 이상 카드를 제거할 수 없습니다");
            return;
        }
        int price = CurrentRemovePrice;
        if (DataManager.Instance.Gold < price)
        {
            AnnouncementUI.instance?.Show("골드가 부족합니다");
            return;
        }
        List<int> candidates = FindPieces(p => p.deckCardIDs != null && p.deckCardIDs.Count > 0);
        if (candidates.Count == 0)
        {
            AnnouncementUI.instance?.Show("제거할 카드가 없습니다");
            return;
        }

        PickPiece(candidates, "제거할 카드를 가진 기물을 선택하세요", pieceIndex =>
        {
            // 카드 선택 패널이 닫힐 때까지 다른 상점 조작을 계속 막는다(PickPiece가 콜백 직후 해제하므로 다시 건다).
            interactionPending = true;
            CardCanvas.instance.ShowCardSelectionPanel(
                CardZone.SavedDeck, 1, null,
                selected => OnRemoveCardSelected(selected, price),
                pieceIndex, allowCancel: true);
        });
    }

    // 카드 선택 패널이 닫힐 때 호출. 선택한 카드는 이미 CardCanvas.ApplySavedDeckSelection이 덱에서 지운 상태다.
    // 빈 목록이면 취소했거나 고를 카드가 없었던 것이므로 골드를 받지 않는다.
    void OnRemoveCardSelected(List<RectTransform> selected, int price)
    {
        interactionPending = false;
        if (selected == null || selected.Count == 0) return;

        // 패널이 열려 있는 동안 다른 상점 조작을 막았으므로 버튼을 누를 때 확인한 잔액이 그대로다.
        if (!DataManager.Instance.SpendGold(price))
            Debug.LogWarning($"[ShopCanvas] 카드 제거 후 골드 차감 실패(필요 {price}, 보유 {DataManager.Instance.Gold})");
        DataManager.Instance.RecordCardRemoval();
        removalsThisVisit++;
        AudioManager.instance?.PlayShopPurchase();
        RefreshPurchasability();
    }

    // ────────── 공용 ──────────

    // candidates(Pieces 인덱스) 중 한 기물을 고르게 한다. 1명뿐이면 묻지 않고 바로 onPicked를 부르고, 여럿이면
    // 취소 가능한 PieceTargetPickerUI를 띄운다. 고르거나 취소할 때까지 다른 상점 조작을 막는다.
    void PickPiece(List<int> candidates, string prompt, System.Action<int> onPicked)
    {
        if (candidates.Count == 1)
        {
            onPicked(candidates[0]);
            return;
        }

        if (PieceTargetPickerUI.instance == null)
        {
            Debug.LogError("[ShopCanvas] 씬에 PieceTargetPickerUI가 없어 기물을 고를 수 없습니다.");
            return;
        }

        IReadOnlyList<PieceData> pieces = DataManager.Instance.Pieces;
        List<PieceData> shown = candidates.Select(i => pieces[i]).ToList();
        interactionPending = true;
        PieceTargetPickerUI.instance.Show(
            shown,
            pick =>
            {
                interactionPending = false;
                onPicked(candidates[pick]);
            },
            prompt,
            onCancel: () => interactionPending = false);
    }

    static List<int> FindPieces(System.Func<PieceData, bool> match)
    {
        var result = new List<int>();
        IReadOnlyList<PieceData> pieces = DataManager.Instance.Pieces;
        for (int i = 0; i < pieces.Count; i++)
            if (match(pieces[i])) result.Add(i);
        return result;
    }

    void OnGoldChanged(int gold) => RefreshPurchasability();

    // 가격 라벨 색(살 수 없으면 빨간색)과 카드 제거 버튼 상태를 현재 골드에 맞춘다.
    void RefreshPurchasability()
    {
        int gold = DataManager.Instance != null ? DataManager.Instance.Gold : 0;
        foreach (ShopCardSlot slot in cardSlots)
            if (slot != null) slot.RefreshAffordable(gold);
        foreach (ShopRelicSlot slot in relicSlots)
            if (slot != null) slot.RefreshAffordable(gold);
        RefreshRemoveButton(gold);
    }

    void RefreshRemoveButton(int gold)
    {
        bool limitReached = RemoveLimitReached;
        if (removeButton != null) removeButton.interactable = !limitReached;
        if (removeButtonLabel == null) return;

        int price = CurrentRemovePrice;
        removeButtonLabel.text = limitReached ? "제거 완료" : $"카드 제거 {price}G";
        removeButtonLabel.color = limitReached || gold >= price ? removeLabelDefaultColor : UnaffordableColor;
    }

    // 닫기 버튼의 OnClick에 연결.
    public void OnClickClose()
    {
        if (interactionPending) return;
        AudioManager.instance?.PlayButtonClick();
        Hide();
    }
}
