using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// 연출용 카메라 이동 — "이 점을 비춰라"만 알고 연출 큐·Board는 모른다. 어디서든 CameraDirector.instance.Focus...로 부른다.
// Main Camera에 CinemachineBrain을, 가상 카메라(vcam)를 코드로 만든다(씬 수정 불필요):
//  - DefaultCam: CameraZoom 리그의 자식. 리그가 줌·팬·Q/E로 움직이면 그대로 따라가므로 평소 시점은 기존과 같다.
//  - FocusCam A/B: 루트 오브젝트. 포커스할 때마다 번갈아 쓰며 우선순위를 올려, Brain이 직전 시점에서 새 시점으로 부드럽게 블렌드한다.
// 포커스 중에는 매 프레임 CameraZoom.ComputePose로 자세를 다시 계산하므로 Q/E 회전 중에도 같은 점을 비춘다.
// Release()하면 DefaultCam(사용자가 맞춰둔 줌·팬·회전)으로 블렌드해 돌아간다.
// 씬에 미리 붙여 둔다(CameraZoom이 붙은 "Camera" 리그 오브젝트에 추가하는 것을 권장) — 스스로 오브젝트를 만들지 않는다.
[DefaultExecutionOrder(-1000)] // Brain(LateUpdate)이 vcam 자세를 읽기 전에 FocusCam 자세를 갱신
[DisallowMultipleComponent]
public class CameraDirector : MonoBehaviour
{
    static CameraDirector _instance;

    // 씬에 미리 붙여둔 인스턴스(보통 CameraZoom이 붙은 "Camera" 리그). 직접 만들지 않으므로 씬에 없으면 null —
    // 호출부는 null이면 카메라 연출을 건너뛴다.
    public static CameraDirector instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<CameraDirector>();
            return _instance;
        }
    }

    [Header("블렌드")]
    [SerializeField, Tooltip("포커스 이동·복귀 시간(초)")] float blendTime = 0.6f;
    [SerializeField, Tooltip("Cinemachine 블렌드를 Time.timeScale과 무관하게 진행(true) — 끄면 배속·히트스탑을 따른다")] bool ignoreTimeScale = false;

    [Header("자동 프레이밍 (FocusBetween)")]
    [SerializeField, Tooltip("시전자 혼자(또는 같은 칸)일 때 거리 오프셋 — 음수일수록 가까이")] float soloDistanceOffset = -2f;
    [SerializeField, Tooltip("시전자와 대상이 다를 때의 기본 거리 오프셋")] float pairDistanceOffset = -1f;
    [SerializeField, Tooltip("대상이 멀수록(월드 단위) 이만큼씩 더 물러난다")] float distanceOffsetPerUnit = 0.7f;

    [Header("추적 (Follow)")]
    [SerializeField, Tooltip("움직이는 대상을 따라갈 때의 부드러움(초). 0에 가까울수록 대상에 딱 붙는다")] float followSmoothTime = 0.1f;

    [Header("대기")]
    [SerializeField, Tooltip("이미 이 정도 안에 있으면 이동 없이 통과(월드 단위)")] float sameFocusDistance = 0.3f;
    [SerializeField, Tooltip("이미 이 거리 오프셋 차이 안이면 같은 포커스로 본다")] float sameFocusOffset = 0.5f;
    [SerializeField, Tooltip("블렌드가 끝나지 않아도 이 시간(실시간 초)이 지나면 진행")] float settleTimeout = 2f;

    const int DefaultPriority = 10;
    const int FocusPriority = 30;

    sealed class FocusSlot
    {
        public CinemachineCamera cam;
        public Vector3 point;
        public float offset;
    }

    CameraZoom rig;
    Camera mainCamera;
    CinemachineBrain brain;
    CinemachineCamera defaultCam;
    readonly FocusSlot[] slots = new FocusSlot[2];
    int activeSlot = -1; // -1이면 포커스 없음(DefaultCam)
    bool ready;
    Transform followTarget;
    Vector3 followVelocity;

    public bool IsFocused => activeSlot >= 0;

    // 연출 포커스 중인지 — 첫 포커스부터 큐가 끝나 Release될 때까지 true. 이미 있는 인스턴스만 보므로(instance는 없으면 만든다)
    // CameraZoom.Update처럼 매 프레임 불러도 안전하다. 연출 중 휠 줌을 막는 데 쓴다.
    public static bool IsFocusing => _instance != null && _instance.IsFocused;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        Setup();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Setup()
    {
        rig = FindFirstObjectByType<CameraZoom>();
        mainCamera = rig != null ? rig.GetComponentInChildren<Camera>() : Camera.main;
        if (rig == null || mainCamera == null)
        {
            Debug.LogWarning("[CameraDirector] CameraZoom 리그나 카메라가 없어 연출 카메라를 쓰지 않는다.");
            return;
        }

        brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null) brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        brain.IgnoreTimeScale = ignoreTimeScale;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);

        // 평소 시점: 지금 Main Camera 자세를 그대로 복제해 리그의 자식으로 둔다.
        defaultCam = CreateCam("DefaultCam", rig.transform, DefaultPriority);
        defaultCam.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);

        for (int i = 0; i < slots.Length; i++)
            slots[i] = new FocusSlot { cam = CreateCam("FocusCam" + i, transform, 0) };
        ready = true;
    }

    CinemachineCamera CreateCam(string camName, Transform parent, int priority)
    {
        var go = new GameObject(camName);
        go.transform.SetParent(parent, false);
        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
        cam.Lens = LensSettings.FromCamera(mainCamera);
        cam.Priority.Value = priority;
        return cam;
    }

    // 보드 평면 위의 한 점을 화면 중앙에 두는 시점으로 부드럽게 이동한다. distanceOffset은 기본 거리 기준(음수 = 가까이).
    // 이미 거의 같은 포커스면 아무것도 하지 않는다. 이동이 끝나기를 기다리려면 WaitUntilSettled를 이어서 부른다.
    public void FocusPoint(Vector3 worldPoint, float distanceOffset)
    {
        if (!ready) return;
        followTarget = null; // 새 포커스가 시작되면 이전 추적은 끝난다
        worldPoint.y = 0f;
        distanceOffset = Mathf.Clamp(distanceOffset, rig.MinDistanceOffset, rig.MaxDistanceOffset);
        // 사용자가 이미 목표보다 더 확대해 뒀으면(오프셋이 더 작음) 연출이 그 확대를 되돌리지 않고 유지한다. 목표가 더 확대면 목표를 쓴다.
        distanceOffset = Mathf.Min(distanceOffset, rig.UserDistanceOffset);

        if (activeSlot >= 0)
        {
            FocusSlot current = slots[activeSlot];
            if ((current.point - worldPoint).sqrMagnitude <= sameFocusDistance * sameFocusDistance
                && Mathf.Abs(current.offset - distanceOffset) <= sameFocusOffset)
                return;
        }

        // 직전 포커스와 다른 슬롯을 새 시점으로 쓴다 — Brain이 "직전 시점 → 새 시점"으로 블렌드하고, 블렌드 중 재호출도 끊김 없이 이어진다.
        int next = activeSlot == 0 ? 1 : 0;
        FocusSlot slot = slots[next];
        slot.point = worldPoint;
        slot.offset = distanceOffset;
        ApplyPose(slot);

        if (activeSlot >= 0) slots[activeSlot].cam.Priority.Value = 0;
        slot.cam.Priority.Value = FocusPriority;
        activeSlot = next;
    }

    // 시전자와 대상들을 함께 비춘다. 대상이 없거나 시전자와 같은 자리면 시전자를 가운데에, 다르면 시전자와 대상들 전체의 중점을 가운데에 둔다
    // (멀수록 물러난다). 시전자가 없으면(전역 효과) 대상들만 기준으로 한다.
    public void FocusBetween(Vector3? casterPosition, IList<Vector3> targetPositions)
    {
        if (!ready) return;

        bool hasMin = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;
        void Include(Vector3 p)
        {
            p.y = 0f;
            if (!hasMin) { min = max = p; hasMin = true; return; }
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        if (casterPosition.HasValue) Include(casterPosition.Value);
        if (targetPositions != null)
            foreach (Vector3 p in targetPositions) Include(p);
        if (!hasMin) return;

        Vector3 center = (min + max) * 0.5f;
        float spread = Vector3.Distance(min, max);
        float offset = spread < sameFocusDistance ? soloDistanceOffset : pairDistanceOffset + spread * distanceOffsetPerUnit;
        FocusPoint(center, offset);
    }

    // 포커스 중인 시점이 target(움직이는 기물 등)을 계속 가운데에 두고 따라가게 한다. FocusPoint로 먼저 대상 근처를 비춘 뒤 부른다.
    // 새 FocusPoint·Release·StopFollow, 또는 대상이 파괴되면 끝난다.
    public void Follow(Transform target)
    {
        if (!ready || activeSlot < 0 || target == null) return;
        followTarget = target;
        followVelocity = Vector3.zero;
    }

    public void StopFollow()
    {
        followTarget = null;
    }

    // 포커스를 풀고 평소 시점(사용자가 맞춰둔 줌·팬·회전)으로 부드럽게 돌아간다.
    public void Release()
    {
        followTarget = null;
        if (!ready || activeSlot < 0) return;
        slots[activeSlot].cam.Priority.Value = 0;
        activeSlot = -1;
    }

    // 카메라가 목표에 도착할 때까지 대기한다 — 연출 코루틴이 yield return으로 이어 쓴다.
    // timeScale이 0이어도 시간 제한이 흐르도록 실시간으로 센다(블렌드가 멈춰도 연출 큐가 영구히 멈추지 않게 하는 안전장치).
    public IEnumerator WaitUntilSettled()
    {
        if (!ready) yield break;
        yield return null; // Brain이 이번 포커스를 반영해 블렌드를 시작할 때까지 한 프레임
        float waited = 0f;
        while (brain.IsBlending && waited < settleTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void LateUpdate()
    {
        if (!ready || activeSlot < 0) return;
        if (followTarget != null)
        {
            // 기물 연출과 같은 시간(scaled)으로 살짝 부드럽게 따라가 시작·정지 때 튀지 않게 한다.
            Vector3 target = followTarget.position;
            target.y = 0f;
            FocusSlot active = slots[activeSlot];
            active.point = Vector3.SmoothDamp(active.point, target, ref followVelocity, followSmoothTime);
        }
        // 블렌드 중인 이전 슬롯도 같은 yaw를 반영해야 회전 중에 어긋나지 않으므로 둘 다 갱신한다.
        foreach (FocusSlot slot in slots)
            ApplyPose(slot);
    }

    void ApplyPose(FocusSlot slot)
    {
        rig.ComputePose(slot.point, slot.offset, out Vector3 position, out Quaternion rotation);
        slot.cam.transform.SetPositionAndRotation(position, rotation);
    }
}
