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

    GameObject spawnedIcon;

    // relicName의 유물 아이콘을 스폰하고, 클릭 시 onClick(relicName)이 호출되도록 연결한다(상점에서는 구매 처리).
    public void SetRelic(string relicName, System.Action<string> onClick)
    {
        ClearIcon();
        spawnedIcon = RelicDatabase.instance.SpawnIcon(iconParent, relicName);
        RelicIcon icon = spawnedIcon != null ? spawnedIcon.GetComponent<RelicIcon>() : null;
        if (icon != null) icon.onClickOverride = onClick;
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
