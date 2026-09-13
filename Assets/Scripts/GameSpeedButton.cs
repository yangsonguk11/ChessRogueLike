using TMPro;
using UnityEngine;

// GameSpeedManager를 클릭 한 번으로 순환시키는 버튼. 라벨에 "1x"/"2x"/"3x"를 표시한다.
public class GameSpeedButton : MonoBehaviour
{
    // 이 프로젝트에는 보드 타일용 커스텀 Button 클래스가 전역 네임스페이스에 있어서
    // UnityEngine.UI.Button을 반드시 전체 이름으로 써야 한다.
    [SerializeField] UnityEngine.UI.Button button;
    [SerializeField] TextMeshProUGUI speedLabel;

    void Awake()
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
    }

    void Start()
    {
        RefreshLabel();
    }

    public void OnClickCycleSpeed()
    {
        AudioManager.instance?.PlayButtonClick();
        GameSpeedManager.instance.CycleSpeed();
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (speedLabel != null)
            speedLabel.text = $"{GameSpeedManager.instance.CurrentSpeed:0.#}x";
    }
}
