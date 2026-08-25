using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상점 오브젝트를 클릭했을 때 ButtonInfo 대신 아래에서 올라오는 애니메이션과 함께 나타나는 상점 화면.
// 카드 8장(가로 4 x 세로 2)을 중복 없이 무작위로 진열하고, 우측 하단 버튼으로 카드 제거도 할 수 있다.
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

    [Tooltip("카드 1장 구매 가격")]
    [SerializeField] int cardPrice = 0;

    [Tooltip("카드 1장 제거 가격")]
    [SerializeField] int removePrice = 0;

    [Tooltip("ShopRelicSlot 프리팹. ShopCardSlot과 동일한 구조(아이콘 부분 + 텍스트 부분).")]
    [SerializeField] GameObject relicSlotPrefab;

    [Tooltip("relicSlotPrefab이 스폰될 부모. Grid Layout Group이 붙어 있어 스폰만 하면 자동 정렬된다.")]
    [SerializeField] RectTransform relicSlotContainer;

    [SerializeField] int relicSlotCount = 4;

    [Tooltip("유물 1개 구매 가격")]
    [SerializeField] int relicPrice = 0;

    [SerializeField] float slideDuration = 0.35f;

    Vector2 shownPos;
    Vector2 hiddenPos;
    Coroutine slideRoutine;
    readonly List<GameObject> spawnedSlots = new List<GameObject>();
    readonly List<GameObject> spawnedRelicSlots = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;

        shownPos = panelRoot.anchoredPosition;
        hiddenPos = shownPos - new Vector2(0f, panelRoot.rect.height);
        panelRoot.anchoredPosition = hiddenPos;
        panelRoot.gameObject.SetActive(false);
    }

    public void Show()
    {
        Board.instance?.HideButtonInfoForShop();

        SpawnShopCards();
        SpawnShopRelics();

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

    IEnumerator SlideOutThenDeactivate()
    {
        yield return Slide(panelRoot.anchoredPosition, hiddenPos);
        panelRoot.gameObject.SetActive(false);
        ClearSpawnedCards();
        ClearSpawnedRelicSlots();
    }

    void SpawnShopCards()
    {
        ClearSpawnedCards();
        List<string> picks = PickShopCards(slotCount);

        foreach (string cardName in picks)
        {
            GameObject slotObj = Instantiate(cardSlotPrefab, slotContainer);
            ShopCardSlot slot = slotObj.GetComponent<ShopCardSlot>();
            if (slot != null)
            {
                slot.SetCard(cardName, BuyCard);
                slot.SetLabel(cardPrice.ToString());
            }
            spawnedSlots.Add(slotObj);
        }
    }

    // 상점에 진열할 카드를 뽑는 로직. CardDatabase(ICardDatabase)에 선정을 위임하므로, 희귀도 가중치나
    // 특정 카드 제외 같은 정책 변경은 여기가 아니라 CardDatabase.PickRandomDistinct 쪽에서 처리하면 된다.
    List<string> PickShopCards(int count)
    {
        ICardDatabase cardDb = CardDatabase.instance;
        return cardDb.PickRandomDistinct(count);
    }

    void SpawnShopRelics()
    {
        ClearSpawnedRelicSlots();
        List<string> picks = PickShopRelics(relicSlotCount);

        foreach (string relicName in picks)
        {
            GameObject slotObj = Instantiate(relicSlotPrefab, relicSlotContainer);
            ShopRelicSlot slot = slotObj.GetComponent<ShopRelicSlot>();
            if (slot != null)
            {
                slot.SetRelic(relicName, BuyRelic);
                slot.SetLabel(relicPrice.ToString());
            }
            spawnedRelicSlots.Add(slotObj);
        }
    }

    // 상점에 진열할 유물을 뽑는 로직. RelicDatabase(IRelicDatabase)에 선정을 위임한다.
    List<string> PickShopRelics(int count)
    {
        IRelicDatabase relicDb = RelicDatabase.instance;
        return relicDb.PickRandomDistinct(count);
    }

    // 유물 슬롯 클릭 시 호출: relicPrice만큼 gold를 차감한 뒤 소유 유물 목록에 영구히 추가한다.
    // gold가 부족하면 아무 일도 일어나지 않고 안내만 뜬다. 세이브에 반영한 뒤 Board.LoadOwnedRelics를
    // 재사용해 파티 아이콘 바를 즉시 다시 그려서, 구매 여부를 바로 알 수 있게 한다.
    void BuyRelic(string relicName)
    {
        if (!DataManager.Instance.SpendGold(relicPrice))
        {
            AnnouncementUI.instance?.Show("골드가 부족합니다");
            return;
        }

        DataManager.Instance.AddRelic(relicName);
        Board.instance?.LoadOwnedRelics();
    }

    // 카드 슬롯 클릭 시 호출: cardPrice만큼 gold를 차감한 뒤, 어느 기물의 덱에 넣을지 고르게 하고 영구히 추가한다.
    // gold가 부족하면 아무 일도 일어나지 않고 안내만 뜬다.
    void BuyCard(string cardName)
    {
        if (!DataManager.Instance.SpendGold(cardPrice))
        {
            AnnouncementUI.instance?.Show("골드가 부족합니다");
            return;
        }

        PieceTargetPickerUI.instance.Show(DataManager.Instance.Pieces, pieceIndex =>
        {
            DataManager.Instance.AddCardToPieceDeck(pieceIndex, cardName);
        });
    }

    // 우측 하단 "카드 제거" 버튼의 OnClick에 연결. removePrice만큼 gold를 차감한 뒤,
    // 기물 선택 → 그 기물의 덱 목록에서 카드 1장 선택 → 영구 제거. gold가 부족하면 안내만 뜨고 진행되지 않는다.
    public void OnClickRemoveCard()
    {
        if (!DataManager.Instance.SpendGold(removePrice))
        {
            AnnouncementUI.instance?.Show("골드가 부족합니다");
            return;
        }

        PieceTargetPickerUI.instance.Show(
            DataManager.Instance.Pieces,
            pieceIndex => CardCanvas.instance.ShowCardSelectionPanel(
                CardZone.SavedDeck, 1, null,
                _ => { },
                pieceIndex),
            "제거할 카드를 가진 기물을 선택하세요");
    }

    // 닫기 버튼의 OnClick에 연결.
    public void OnClickClose()
    {
        Hide();
    }

    void ClearSpawnedCards()
    {
        foreach (var s in spawnedSlots)
            if (s != null) Destroy(s);
        spawnedSlots.Clear();
    }

    void ClearSpawnedRelicSlots()
    {
        foreach (var s in spawnedRelicSlots)
            if (s != null) Destroy(s);
        spawnedRelicSlots.Clear();
    }
}
