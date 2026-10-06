using TMPro;
using UnityEngine;

// 상점 유물 슬롯 프리팹에 붙는 컴포넌트. ShopCardSlot과 동일한 구조 — "아이콘 부분"(실제 RelicIcon을
// RelicDatabase와 동일한 방식으로 스폰)과 "텍스트 부분"(가격 등 자유 텍스트)으로 나뉜다. ShopCanvas는
// 이 프리팹을 Grid Layout Group이 적용된 컨테이너에 스폰만 하면 되고, 개별 슬롯을 인스펙터에
// 미리 등록해둘 필요가 없다.
public class ShopRelicSlot : MonoBehaviour
{
    [SerializeField] RectTransform iconParent;   // 실제 유물 아이콘이 스폰될 자리
    [SerializeField] TextMeshProUGUI labelText;  // 가격 등 자유 텍스트

    static readonly Color UnaffordableColor = new Color(1f, 0.35f, 0.35f);

    GameObject spawnedIcon;
    Color defaultLabelColor = Color.white;

    public string RelicName { get; private set; }
    public int Price { get; private set; }
    public bool SoldOut { get; private set; }

    void Awake()
    {
        if (labelText != null) defaultLabelColor = labelText.color;
    }

    // relicName의 유물 아이콘을 스폰하고, 클릭 시 onClick(이 슬롯)이 호출되도록 연결한다(상점에서는 구매 처리).
    public void SetRelic(string relicName, System.Action<ShopRelicSlot> onClick)
    {
        ClearIcon();
        RelicName = relicName;
        SoldOut = false;
        spawnedIcon = RelicDatabase.instance.SpawnIcon(iconParent, relicName);
        RelicIcon icon = spawnedIcon != null ? spawnedIcon.GetComponent<RelicIcon>() : null;
        if (icon != null) icon.onClickOverride = _ => onClick?.Invoke(this);
    }

    // 가격을 정하고 라벨을 "{price}G"로 표시한다.
    public void SetPrice(int price)
    {
        Price = price;
        if (!SoldOut) SetLabel($"{price}G");
    }

    // 보유 골드로 살 수 없으면 가격 라벨을 빨간색으로 표시한다.
    public void RefreshAffordable(int gold)
    {
        if (labelText == null) return;
        labelText.color = SoldOut || gold >= Price ? defaultLabelColor : UnaffordableColor;
    }

    // 구매 완료: 아이콘을 치우고 "매진"만 남긴다.
    public void MarkSoldOut()
    {
        SoldOut = true;
        ClearIcon();
        SetLabel("매진");
        if (labelText != null) labelText.color = defaultLabelColor;
    }

    public void SetLabel(string text)
    {
        if (labelText != null) labelText.text = text;
    }

    public void ClearIcon()
    {
        if (spawnedIcon != null) Destroy(spawnedIcon);
        spawnedIcon = null;
    }

    void OnDestroy() => ClearIcon();
}
