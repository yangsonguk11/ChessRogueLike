using TMPro;
using UnityEngine;

// 상점 카드 슬롯 프리팹에 붙는 컴포넌트. "카드 부분"(실제 Card를 CardsPanel과 동일한 방식으로 스폰)과
// "텍스트 부분"(가격 등 자유 텍스트)으로 나뉜다. ShopCanvas는 이 프리팹을 Grid Layout Group이 적용된
// 컨테이너에 스폰만 하면 되고, 개별 슬롯을 인스펙터에 미리 등록해둘 필요가 없다.
public class ShopCardSlot : MonoBehaviour
{
    [SerializeField] RectTransform cardParent;   // 실제 카드가 스폰될 자리
    [SerializeField] TextMeshProUGUI labelText;  // 가격 등 자유 텍스트

    static readonly Color UnaffordableColor = new Color(1f, 0.35f, 0.35f);

    GameObject spawnedCard;
    Color defaultLabelColor = Color.white;

    public string CardName { get; private set; }
    public int Price { get; private set; }
    public bool SoldOut { get; private set; }

    void Awake()
    {
        if (labelText != null) defaultLabelColor = labelText.color;
    }

    // cardName의 카드를 스폰하고, 카드를 클릭하면 onClick(이 슬롯)이 호출되도록 연결한다(상점에서는 구매 처리).
    // Card.onClickOverride가 넘기는 카드 Name 대신 슬롯 자체를 넘겨, 덱에 저장할 ID를 슬롯의 CardName(프리팹 이름)으로 확정한다.
    public void SetCard(string cardName, System.Action<ShopCardSlot> onClick)
    {
        ClearCard();
        CardName = cardName;
        SoldOut = false;
        ICardDatabase cardDb = CardDatabase.instance;
        spawnedCard = cardDb.SpawnCard(cardParent, cardName);
        Card card = spawnedCard != null ? spawnedCard.GetComponent<Card>() : null;
        if (card != null) card.onClickOverride = _ => onClick?.Invoke(this);
    }

    // 가격을 정하고 라벨을 "{prefix}{price}G"로 표시한다(prefix 예: "[희귀] ").
    public void SetPrice(int price, string prefix = "")
    {
        Price = price;
        if (!SoldOut) SetLabel($"{prefix}{price}G");
    }

    // 보유 골드로 살 수 없으면 가격 라벨을 빨간색으로 표시한다.
    public void RefreshAffordable(int gold)
    {
        if (labelText == null) return;
        labelText.color = SoldOut || gold >= Price ? defaultLabelColor : UnaffordableColor;
    }

    // 구매 완료: 카드를 치우고 "매진"만 남긴다. 슬롯 자체는 남겨 진열 배치가 흐트러지지 않게 한다.
    public void MarkSoldOut()
    {
        SoldOut = true;
        ClearCard();
        SetLabel("매진");
        if (labelText != null) labelText.color = defaultLabelColor;
    }

    public void SetLabel(string text)
    {
        if (labelText != null) labelText.text = text;
    }

    public void ClearCard()
    {
        if (spawnedCard != null) Destroy(spawnedCard);
        spawnedCard = null;
    }

    void OnDestroy() => ClearCard();
}
