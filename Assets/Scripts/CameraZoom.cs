using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// 마우스 휠로 카메라를 확대/축소하고, 확대한 상태에서는 화면 가장자리·WASD로 카메라를 옮긴다.
// Q/E는 보드 중심을 축으로 카메라를 90°씩 공전시키고, Z는 처음 시점으로 되돌린다.
// 자식 Main Camera가 아니라 부모 리그("Camera")에 붙인다 — CameraFX.Shake가 자식의 localPosition을 흔들기 때문에,
// 줌·이동·회전은 부모만 움직여야 두 연출이 충돌하지 않는다.
// 카메라는 항상 기준점(pivot, 보드 평면 위 점)을 바라본다. 확대할수록 그 점을 중심으로 카메라를 아래로 공전시켜
// 시선이 조금 더 눕게 하고, 축소 쪽은 기본 각도를 유지한다. 이동은 이 기준점을 보드 평면 위에서 옮기는 것이다.
// 회전은 이 자세 전체를 보드 중심 기준으로 마지막에 돌린다 — 이동량은 회전 전 좌표계에 있으므로 화면 기준 이동 방향이 그대로 맞는다.
public class CameraZoom : MonoBehaviour
{
    [Header("확대/축소")]
    [SerializeField, Tooltip("휠 한 칸당 거리 변화")] float zoomStep = 1f;
    [SerializeField, Tooltip("최대 확대 — 기준 거리에서 얼마나 가까이 갈 수 있는지(음수)")] float minDistanceOffset = -4f;
    [SerializeField, Tooltip("최대 축소 — 기준 거리에서 얼마나 멀어질 수 있는지(양수)")] float maxDistanceOffset = 3f;
    [SerializeField, Tooltip("최대 확대 시 고도각을 낮추는 각도(°)")] float maxTiltDown = 12f;
    [SerializeField] float smoothTime = 0.12f;

    [Header("이동 (확대했을 때만)")]
    [SerializeField, Tooltip("화면 테두리에서 안쪽으로 감지하는 띠 두께(화면 높이 비율)")] float edgeSize = 0.03f;
    [SerializeField, Tooltip("화면 끝에 붙였을 때·키 입력 시 초당 이동량(월드 단위)")] float panSpeed = 8f;

    [Header("회전 (Q/E)")]
    [SerializeField] float rotateSmoothTime = 0.25f;

    Vector3 basePivot;
    float baseDistance;
    Vector3 baseDirection; // pivot → 리그 방향(단위 벡터)
    Quaternion baseRotation;
    Vector3 tiltAxis;
    Vector3 panRight;   // 화면 오른쪽에 해당하는 보드 평면 방향
    Vector3 panForward; // 화면 위쪽에 해당하는 보드 평면 방향
    // 자식 카메라의 리그 기준 로컬 자세 — 내려보는 각도(피치)는 리그가 아니라 여기에 들어 있다. ComputePose가 카메라 월드 자세를 만들 때 쓴다.
    Vector3 viewLocalPosition;
    Quaternion viewLocalRotation = Quaternion.identity;

    float targetOffset;
    float currentOffset;
    float velocity;

    Vector3 targetPan;
    Vector3 currentPan;
    Vector3 panVelocity;

    // 보드 중심 수직축 기준 회전(도). 각도를 감싸지 않아 연타해도 같은 방향으로 계속 돈다.
    float targetYaw;
    float currentYaw;
    float yawVelocity;

    PointerEventData pointerData;
    EventSystem pointerDataOwner;
    readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    void Awake()
    {
        Camera cam = GetComponentInChildren<Camera>();
        Transform view = cam != null ? cam.transform : transform;
        if (view != transform)
        {
            viewLocalPosition = view.localPosition;
            viewLocalRotation = view.localRotation;
        }

        // 화면 중심 광선이 보드 평면(y=0)과 만나는 점. 거의 수평이면 정면 일정 거리를 쓴다.
        Vector3 origin = view.position;
        Vector3 forward = view.forward;
        basePivot = forward.y < -0.01f ? origin + forward * (-origin.y / forward.y) : origin + forward * 10f;

        Vector3 offset = transform.position - basePivot;
        baseDistance = offset.magnitude;
        baseDirection = offset / baseDistance;
        baseRotation = transform.rotation;
        tiltAxis = view.right;
        // 기울기는 right 축 회전뿐이라 yaw가 변하지 않으므로 이동 축도 한 번만 구한다.
        panRight = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
        panForward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        // 휠 한 칸 값이 Input System 버전·OS에 따라 ±120 또는 ±1이라 부호만 쓴다. 휠 위 = 확대.
        // 연출 포커스 중에는 휠 줌을 무시한다 — 포커스 카메라는 사용자 줌을 반영하지 않아, 눌러 두면 복귀 때 줌이 갑자기 바뀐다.
        if (scroll != 0f && !CameraDirector.IsFocusing && !IsPointerOverScreenUI(mousePos))
            targetOffset = Mathf.Clamp(targetOffset - Mathf.Sign(scroll) * zoomStep, minDistanceOffset, maxDistanceOffset);

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.zKey.wasPressedThisFrame) ResetView();
            // 위에서 봤을 때 Q = 카메라가 시계 방향, E = 반시계 방향으로 공전.
            if (kb.qKey.wasPressedThisFrame) targetYaw += 90f;
            if (kb.eKey.wasPressedThisFrame) targetYaw -= 90f;
        }

        Vector2 edge = mouse != null ? EdgeInput(mousePos) : Vector2.zero;
        // 둘을 동시에 써도 빨라지지 않고, 대각선도 같은 속도.
        Pan(Vector2.ClampMagnitude(edge + KeyboardInput(), 1f));

        bool zoomSettled = Mathf.Approximately(currentOffset, targetOffset);
        bool panSettled = currentPan == targetPan;
        bool yawSettled = Mathf.Approximately(currentYaw, targetYaw);
        if (zoomSettled && panSettled && yawSettled) return;

        // 히트스탑(timeScale 0)·배속과 무관하게 움직이도록 unscaled 시간.
        float dt = Time.unscaledDeltaTime;
        if (!zoomSettled)
        {
            currentOffset = Mathf.SmoothDamp(currentOffset, targetOffset, ref velocity, smoothTime, Mathf.Infinity, dt);
            if (Mathf.Abs(currentOffset - targetOffset) < 0.0001f)
            {
                currentOffset = targetOffset;
                velocity = 0f;
            }
        }
        if (!panSettled)
        {
            currentPan = Vector3.SmoothDamp(currentPan, targetPan, ref panVelocity, smoothTime, Mathf.Infinity, dt);
            if ((currentPan - targetPan).sqrMagnitude < 0.0001f * 0.0001f)
            {
                currentPan = targetPan;
                panVelocity = Vector3.zero;
            }
        }
        if (!yawSettled)
        {
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, rotateSmoothTime, Mathf.Infinity, dt);
            if (Mathf.Abs(currentYaw - targetYaw) < 0.01f)
            {
                currentYaw = targetYaw;
                yawVelocity = 0f;
            }
        }
        ApplyPose();
    }

    // 초기 시점(기본 줌·이동 없음·처음 방향)으로 돌아간다. 목표만 되돌리므로 부드럽게 돌아온다.
    // 회전은 가장 가까운 처음 방향(360° 배수)으로 — 270°에서 누르면 거꾸로 270°가 아니라 90°만 돈다.
    public void ResetView()
    {
        targetOffset = 0f;
        targetPan = Vector3.zero;
        targetYaw = Mathf.Round(targetYaw / 360f) * 360f;
    }

    // 확대 비율 0(기본 줌 이하) ~ 1(최대 확대).
    float ZoomInRatio(float offset) => minDistanceOffset < 0f ? Mathf.Clamp01(offset / minDistanceOffset) : 0f;

    // 엣지 스크롤과 WASD가 공유하는 카메라 이동. input은 화면 기준 방향(x = 오른쪽, y = 위쪽, 각 -1..1).
    // 이동 가능 범위는 확대 비율에 비례해 기준점에서 보드 끝까지 넓어진다 — 매 프레임 클램프하므로 축소하면 중앙으로 돌아온다.
    void Pan(Vector2 input)
    {
        float t = ZoomInRatio(targetOffset);
        if (t > 0f && input != Vector2.zero)
            targetPan += (panRight * input.x + panForward * input.y) * (panSpeed * Time.unscaledDeltaTime);

        if (!TryGetBoardRect(out Vector2 boardMin, out Vector2 boardMax))
        {
            targetPan = Vector3.zero;
            return;
        }
        // 이동량은 회전 전 좌표계라 보드 사각형도 회전 전으로 되돌린다. 90° 단위라 홀수 번 돌았으면 가로·세로 반폭만 맞바꾸면 된다.
        if ((Mathf.RoundToInt(targetYaw / 90f) & 1) != 0)
        {
            Vector2 boardCenter = (boardMin + boardMax) * 0.5f;
            Vector2 half = (boardMax - boardMin) * 0.5f;
            half = new Vector2(half.y, half.x);
            boardMin = boardCenter - half;
            boardMax = boardCenter + half;
        }
        Vector2 center = new Vector2(basePivot.x, basePivot.z);
        Vector2 min = Vector2.Lerp(center, boardMin, t);
        Vector2 max = Vector2.Lerp(center, boardMax, t);
        Vector3 pivot = basePivot + targetPan;
        pivot.x = Mathf.Clamp(pivot.x, Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x));
        pivot.z = Mathf.Clamp(pivot.z, Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y));
        targetPan = pivot - basePivot;
    }

    // 마우스가 화면 테두리 띠 안에 있으면 끝으로 갈수록 0 → 1. 화면 공간 UI 위·창 포커스 없음·화면 밖이면 0.
    Vector2 EdgeInput(Vector2 p)
    {
        if (!Application.isFocused) return Vector2.zero;
        float w = Screen.width, h = Screen.height;
        if (p.x < 0f || p.y < 0f || p.x > w || p.y > h) return Vector2.zero;

        float e = h * edgeSize;
        Vector2 input = new Vector2(
            Mathf.InverseLerp(w - e, w, p.x) - Mathf.InverseLerp(e, 0f, p.x),
            Mathf.InverseLerp(h - e, h, p.y) - Mathf.InverseLerp(e, 0f, p.y));
        if (input == Vector2.zero || IsPointerOverScreenUI(p)) return Vector2.zero;
        return input;
    }

    static Vector2 KeyboardInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
    }

    // 보드 칸이 차지하는 XZ 사각형. Board.LoadLevel이 칸을 놓을 때와 같은 grid.CellToWorld로 양 끝 칸을 구하고 칸 절반만큼 넓힌다.
    static bool TryGetBoardRect(out Vector2 min, out Vector2 max)
    {
        min = max = Vector2.zero;
        Board board = Board.instance;
        if (board == null || board.grid == null) return false;

        Vector3 a = board.grid.CellToWorld(new Vector3Int(0, 0, 0));
        Vector3 b = board.grid.CellToWorld(new Vector3Int(board.N - 1, 0, board.M - 1));
        Vector2 half = new Vector2(board.grid.cellSize.x, board.grid.cellSize.y) * 0.5f;
        min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z)) - half;
        max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)) + half;
        return true;
    }

    void ApplyPose()
    {
        CalcPose(currentPan, currentOffset, currentYaw, out Vector3 position, out Quaternion rotation);
        transform.position = position;
        transform.rotation = rotation;
    }

    // 이동량(pan)·거리 오프셋·회전(yaw)으로 리그의 월드 자세를 계산한다. ApplyPose(사용자 조작)와 ComputePose(연출 포커스)가 공유한다.
    void CalcPose(Vector3 pan, float distanceOffset, float yawDegrees, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        float t = ZoomInRatio(distanceOffset);
        // 카메라 right 축 기준 음의 회전 = 시선이 위로 들리고 위치는 pivot 아래쪽으로 내려간다.
        Quaternion q = Quaternion.AngleAxis(-t * maxTiltDown, tiltAxis);
        Vector3 pivot = basePivot + pan;
        Vector3 position = pivot + q * baseDirection * (baseDistance + distanceOffset);
        Quaternion rotation = q * baseRotation;

        // 보드 중심을 지나는 수직축 기준으로 자세 전체를 돌린다.
        Quaternion yaw = Quaternion.AngleAxis(yawDegrees, Vector3.up);
        Vector3 axisPoint = RotationAxisPoint();
        worldPosition = axisPoint + yaw * (position - axisPoint);
        worldRotation = yaw * rotation;
    }

    Vector3 RotationAxisPoint()
    {
        if (TryGetBoardRect(out Vector2 boardMin, out Vector2 boardMax))
        {
            Vector2 c = (boardMin + boardMax) * 0.5f;
            return new Vector3(c.x, 0f, c.y);
        }
        return new Vector3(basePivot.x, 0f, basePivot.z);
    }

    // 연출용 최소·최대 거리 오프셋(기본 거리 기준). CameraDirector가 프레이밍 거리를 이 범위로 제한한다.
    public float MinDistanceOffset => minDistanceOffset;
    public float MaxDistanceOffset => maxDistanceOffset;
    // 사용자가 휠로 맞춰 둔 확대 목표(음수일수록 더 확대). 보간 중 값(currentOffset)이 아니라 사용자의 의도를 돌려준다.
    // 연출 포커스가 "사용자가 이미 더 확대했으면 그 확대를 유지"할 때 기준으로 쓴다.
    public float UserDistanceOffset => targetOffset;

    // focusWorldPoint(보드 평면 위 점)를 화면 중앙에 두는 **카메라(자식)의 월드 자세**를 계산한다 — 카메라를 실제로 옮기지는 않는다.
    // 리그 자세에 자식 카메라의 로컬 자세(내려보는 각도)를 합성해 반환하므로, 리그 자세가 아니라 실제 화면 시점과 같은 각도가 된다.
    // 현재 yaw(Q/E로 보간 중인 값)를 반영하므로 회전 중에도 같은 점을 계속 비춘다. 사용자 팬·줌 상태와 보드 범위 제한은 무시한다.
    // 이동량은 회전 전 좌표계 값이라, 회전 후 포커스 점이 되도록 보드 중심축 기준 역회전한 점을 기준점으로 쓴다.
    public void ComputePose(Vector3 focusWorldPoint, float distanceOffset, out Vector3 cameraPosition, out Quaternion cameraRotation)
    {
        distanceOffset = Mathf.Clamp(distanceOffset, minDistanceOffset, maxDistanceOffset);
        Vector3 axisPoint = RotationAxisPoint();
        Vector3 unrotated = axisPoint + Quaternion.AngleAxis(-currentYaw, Vector3.up) * (focusWorldPoint - axisPoint);
        Vector3 pan = unrotated - basePivot;
        pan.y = 0f;
        CalcPose(pan, distanceOffset, currentYaw, out Vector3 rigPosition, out Quaternion rigRotation);
        cameraPosition = rigPosition + rigRotation * viewLocalPosition;
        cameraRotation = rigRotation * viewLocalRotation;
    }

    // 화면 공간 UI(맵, 덱 목록, 카드 선택·상점 패널 등) 위면 휠·엣지 스크롤을 UI에 양보한다.
    // EventSystem.IsPointerOverGameObject는 Main Camera의 PhysicsRaycaster 때문에 보드 칸 위에서도 true라 쓰지 않는다.
    bool IsPointerOverScreenUI(Vector2 screenPos)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        if (pointerData == null || pointerDataOwner != es)
        {
            pointerData = new PointerEventData(es);
            pointerDataOwner = es;
        }
        pointerData.position = screenPos;

        raycastResults.Clear();
        es.RaycastAll(pointerData, raycastResults);
        foreach (RaycastResult r in raycastResults)
        {
            if (!(r.module is UnityEngine.UI.GraphicRaycaster gr)) continue;
            Canvas canvas = gr.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return true;
        }
        return false;
    }
}
