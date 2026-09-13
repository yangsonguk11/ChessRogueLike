using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 카메라 흔들림/히트스탑/화면 플래시 등 "타격감" 관련 전역 이펙트를 모아둔 싱글턴.
// GameManager와 달리 씬에 미리 배치할 필요 없이 최초 접근 시 스스로 GameObject를 만들어 DontDestroyOnLoad로 유지한다.
public class CameraFX : MonoBehaviour
{
    static CameraFX _instance;
    public static CameraFX instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CameraFX");
                _instance = go.AddComponent<CameraFX>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    Camera _cam;
    Image _flashImage;
    bool _hitStopRunning;
    float _hitStopEndTime;

    // GameSpeedManager가 히트스탑 중엔 Time.timeScale을 건드리지 않고 기다렸다가, 이 값이 true인 동안엔
    // 복원을 HitStopRoutine에 맡긴다.
    public bool IsHitStopping => _hitStopRunning;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        BuildFlashOverlay();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        _cam = Camera.main;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        _cam = Camera.main;
    }

    void BuildFlashOverlay()
    {
        GameObject canvasObj = new GameObject("CameraFX_FlashCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FlashImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        _flashImage = imgObj.AddComponent<Image>();
        _flashImage.raycastTarget = false;
        _flashImage.color = new Color(1f, 1f, 1f, 0f);

        RectTransform rt = _flashImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // 재발동 시 이전 흔들림을 죽이고 새로 시작 — 동시다발 공격에서 흔들림이 무한 누적되는 것 방지.
    public void Shake(float duration = 0.2f, float magnitude = 0.15f, int vibrato = 10)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        DOTween.Kill(_cam.transform);
        _cam.transform.DOShakePosition(duration, magnitude, vibrato, 90f, false, true);
    }

    // Time.timeScale을 잠깐 0으로 내려 motionQueue/moveQueue/Animator를 한꺼번에 얼린다.
    // 겹쳐 호출되면 더 긴 요청 쪽으로 갱신하고, 짧은 쪽은 무시.
    public void HitStop(float duration = 0.05f)
    {
        float requestedEnd = Time.unscaledTime + duration;
        if (requestedEnd <= _hitStopEndTime) return;
        _hitStopEndTime = requestedEnd;

        if (_hitStopRunning) return; // 이미 도는 루틴이 매 프레임 _hitStopEndTime을 다시 읽으므로 연장만 되면 충분
        StartCoroutine(HitStopRoutine());
    }

    System.Collections.IEnumerator HitStopRoutine()
    {
        _hitStopRunning = true;
        Time.timeScale = 0f;
        while (Time.unscaledTime < _hitStopEndTime)
            yield return null;
        // 히트스탑 도중 배속이 바뀌었을 수 있으니, 멈추기 직전 값이 아니라 GameSpeedManager가 들고 있는
        // "현재 목표 배속"으로 복원한다.
        Time.timeScale = GameSpeedManager.instance.CurrentSpeed;
        _hitStopRunning = false;
    }

    // 짧게 화면을 색으로 물들였다가 되돌아오는 플래시. 작은 타격엔 호출하지 않는 걸 권장.
    public void Flash(Color color, float duration = 0.15f, float maxAlpha = 0.4f)
    {
        if (_flashImage == null) return;
        DOTween.Kill(_flashImage);
        _flashImage.color = new Color(color.r, color.g, color.b, 0f);
        _flashImage.DOFade(maxAlpha, duration * 0.5f).SetLoops(2, LoopType.Yoyo);
    }
}
