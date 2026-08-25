using TMPro;
using UnityEngine;

// 상점 카드 슬롯 프리팹에 붙는 컴포넌트. "카드 부분"(실제 Card를 CardsPanel과 동일한 방식으로 스폰)과
// "텍스트 부분"(가격 등 자유 텍스트)으로 나뉜다. ShopCanvas는 이 프리팹을 Grid Layout Group이 적용된
// 컨테이너에 스폰만 하면 되고, 개별 슬롯을 인스펙터에 미리 등록해둘 필요가 없다.
public class ShopCardSlot : MonoBehaviour
{
    [SerializeField] RectTransform cardParent;   // 실제 카드가 스폰될 자리
    [SerializeField] TextMeshProUGUI labelText;  // 가격 등 자유 텍스트

    GameObject spawnedCard;

    // cardName의 카드를 스폰하고, 클릭 시 onClick(cardName)이 호출되도록 연결한다(상점에서는 구매 처리).
    public void SetCard(string cardName, System.Action<string> onClick)
    {
        ClearCard();
        ICardDatabase cardDb = CardDatabase.instance;
        spawnedCard = cardDb.SpawnCard(cardParent, cardName);
        Card card = spawnedCard != null ? spawnedCard.GetComponent<Card>() : null;
        if (card != null) card.onClickOverride = onClick;
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
