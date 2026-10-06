using TMPro;
using UnityEngine;

// 보유 골드를 TMP 텍스트에 표시한다. 메인 HUD, 상점 패널, 맵 등 원하는 곳에 텍스트와 함께 붙이면 되고,
// DataManager.GoldChanged를 구독해 골드가 바뀌는 즉시 갱신된다.
public class GoldDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;

    [Tooltip("{0} 자리에 보유 골드가 들어간다")]
    [SerializeField] string format = "{0} G";

    // DataManager는 DontDestroyOnLoad라 씬 오브젝트인 이 컴포넌트가 비활성화/파괴될 때 반드시 구독을 해제해야 한다.
    // 구독한 대상을 기억해 두고 그 대상에서 해제한다.
    DataManager subscribedTo;

    void OnEnable() => Subscribe();

    // 처음 MainScene이 로드될 때는 DataManager.Awake보다 이 OnEnable이 먼저 불릴 수 있어(Awake 순서 미정)
    // Instance가 아직 null일 수 있다. 그 경우를 위해 Start에서 한 번 더 시도한다.
    void Start() => Subscribe();

    void OnDisable()
    {
        if (subscribedTo != null) subscribedTo.GoldChanged -= Refresh;
        subscribedTo = null;
    }

    void Subscribe()
    {
        DataManager data = DataManager.Instance;
        if (data != null && subscribedTo != data)
        {
            if (subscribedTo != null) subscribedTo.GoldChanged -= Refresh;
            data.GoldChanged += Refresh;
            subscribedTo = data;
        }
        Refresh(data != null ? data.Gold : 0);
    }

    void Refresh(int gold)
    {
        if (text != null) text.text = string.Format(format, gold);
    }
}
