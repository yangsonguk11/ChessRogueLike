using UnityEngine;

// 전역 게임 배속 싱글턴. Time.timeScale을 갈아끼워서 코루틴(WaitForSeconds)·DOTween(기본값이 scaled time)·
// Animator가 전부 같은 비율로 빨라지게 한다. GameManager/CameraFX와 동일하게 씬에 미리 배치할 필요 없이
// 최초 접근 시 스스로 GameObject를 만들어 DontDestroyOnLoad로 유지한다.
public class GameSpeedManager : MonoBehaviour
{
    static GameSpeedManager _instance;
    public static GameSpeedManager instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameSpeedManager");
                _instance = go.AddComponent<GameSpeedManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    static readonly float[] SpeedSteps = { 1f, 2f, 3f };
    int _speedIndex;

    public float CurrentSpeed => SpeedSteps[_speedIndex];

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        ApplyTimeScale();
    }

    // 버튼 OnClick에 바로 연결해서 쓰는 진입점: 1x -> 2x -> 3x -> 1x 순환.
    public void CycleSpeed()
    {
        _speedIndex = (_speedIndex + 1) % SpeedSteps.Length;
        ApplyTimeScale();
    }

    void ApplyTimeScale()
    {
        // CameraFX.HitStop이 진행 중(Time.timeScale이 0으로 눌려있는 구간)이면 지금 덮어쓰지 않는다 —
        // 히트스탑이 끝날 때 CameraFX가 CurrentSpeed를 읽어 복원하므로 결국 반영된다.
        if (CameraFX.instance.IsHitStopping) return;
        Time.timeScale = CurrentSpeed;
    }
}
