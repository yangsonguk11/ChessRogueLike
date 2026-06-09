using UnityEngine;
using UnityEngine.InputSystem;

// ──────────────────────────────────────────────────────────────────
// 설정 방법 (Unity Inspector)
//   1. CardCanvas 오브젝트의 자식으로 빈 GameObject "CardDragArrow" 생성
//   2. 이 스크립트 추가
//   3. 자식에 "Line" GameObject → Image 컴포넌트 (가로로 긴 흰색/색 스프라이트)
//      Pivot: (0.5, 0.5)
//   4. 자식에 "Head" GameObject → Image 컴포넌트 (위쪽을 향하는 삼각형/화살촉 스프라이트)
//      Pivot: (0.5, 0.5)
//   5. Inspector에서 Line/Head 필드에 위 오브젝트의 RectTransform 연결
// ──────────────────────────────────────────────────────────────────
public class CardDragArrow : MonoBehaviour
{
    public static CardDragArrow instance;

    [SerializeField] RectTransform lineRect;
    [SerializeField] RectTransform headRect;
    [SerializeField] float lineThickness = 8f;


    Canvas _canvas;
    RectTransform _canvasRect;
    RectTransform _from;

    void Awake()
    {
        instance = this;
        _canvas = GetComponentInParent<Canvas>();
        _canvasRect = _canvas.GetComponent<RectTransform>();

        // 캔버스 전체를 투명하게 덮어 좌표 기준으로 삼음
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        SetVisible(false);
    }

    /// <summary>from RectTransform에서 마우스까지 화살표 표시</summary>
    public void Show(RectTransform from)
    {
        _from = from;
        SetVisible(true);
    }

    public void Hide()
    {
        _from = null;
        SetVisible(false);
    }

    void SetVisible(bool v)
    {
        if (lineRect) lineRect.gameObject.SetActive(v);
        if (headRect) headRect.gameObject.SetActive(v);
    }

    void Update()
    {
        if (_from == null || lineRect == null || headRect == null) return;

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : _canvas.worldCamera;

        // Screen Space Overlay에서 RectTransform.position == 스크린 픽셀 좌표
        Vector2 fromScreen = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? (Vector2)_from.position
            : RectTransformUtility.WorldToScreenPoint(cam, _from.position);

        Vector2 toScreen = Mouse.current.position.ReadValue();

        // 스크린 좌표 → 캔버스 로컬 좌표 (원점 = 캔버스 중심)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, fromScreen, cam, out Vector2 start);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, toScreen, cam, out Vector2 end);

        Vector2 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 1f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.localPosition = (start + end) * 0.5f;
        lineRect.sizeDelta = new Vector2(dist, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        headRect.localPosition = end;
        headRect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
