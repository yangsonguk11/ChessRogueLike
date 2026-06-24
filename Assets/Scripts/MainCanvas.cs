using UnityEngine;

// MainCanvas 직속 UI(덱/버린 카드/턴 종료 버튼 등) 표시 여부를 관리
public class MainCanvas : MonoBehaviour
{
    public static MainCanvas instance;

    [SerializeField] GameObject UnSeenEvent; // 이벤트 레벨에서 숨길 전투용 UI(덱/버린 카드/턴 종료 버튼)를 모아둔 부모

    void Awake()
    {
        instance = this;
    }

    // 이벤트 레벨처럼 카드를 쓰지 않는 레벨에서는 전투용 UI를 숨김
    public void SetCombatUIVisible(bool visible)
    {
        UnSeenEvent?.SetActive(visible);
    }
}
