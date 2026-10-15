using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보드 칸(Button) 정보창의 호스트. 칸은 3D 오브젝트라 카드처럼 UI 자식으로 정보창을 붙일 수 없어서, 화면 공간 오버레이
// 캔버스를 하나 만들고 그 아래 앵커를 호버한 칸의 화면 위치로 옮겨 TooltipView를 띄운다. Board.ShowTileTooltip이 처음 쓸 때 만든다.
public class BoardTooltipHost : MonoBehaviour
{
    // 앵커 스케일 = 호버된 손패 카드 스케일(카드 0.5 × 호버 1.3) — 창·글자 크기가 카드 정보창과 같아진다.
    const float AnchorScale = 0.65f;
    // 창을 쌓는 높이(앵커 로컬) = 카드 원래 높이 — 호버된 카드의 화면 높이만큼 쌓고 줄을 바꾼다.
    const float ColumnHeight = 600f;
    // 보드 UI·손패(MainCanvas 2, 호버 카드 3, CardFxLayer 4)보다 위.
    const int SortingOrder = 10;

    RectTransform canvasRect;
    RectTransform anchor;
    TooltipView view;
    Button shownButton;
    Piece shownPiece;

    public static BoardTooltipHost Create(TMP_FontAsset font)
    {
        var go = new GameObject("BoardTooltipCanvas", typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        CopyScalerFromCardCanvas(canvas, scaler);

        var host = go.AddComponent<BoardTooltipHost>();
        host.canvasRect = (RectTransform)go.transform;

        var anchorGo = new GameObject("Anchor", typeof(RectTransform));
        anchorGo.layer = go.layer;
        host.anchor = (RectTransform)anchorGo.transform;
        host.anchor.SetParent(go.transform, false);
        host.anchor.anchorMin = host.anchor.anchorMax = host.anchor.pivot = new Vector2(0.5f, 0.5f);
        host.anchor.sizeDelta = Vector2.zero;
        host.anchor.localScale = Vector3.one * AnchorScale;

        host.view = TooltipView.Create(host.anchor, font);
        return host;
    }

    // 손패 카드와 같은 화면 크기로 보이도록 CardCanvas 루트 캔버스의 스케일 설정·셰이더 채널을 그대로 쓴다.
    static void CopyScalerFromCardCanvas(Canvas canvas, CanvasScaler scaler)
    {
        Canvas cardRoot = CardCanvas.instance != null ? CardCanvas.instance.GetComponentInParent<Canvas>() : null;
        if (cardRoot != null) cardRoot = cardRoot.rootCanvas;
        CanvasScaler src = cardRoot != null ? cardRoot.GetComponent<CanvasScaler>() : null;
        if (src == null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return;
        }
        scaler.uiScaleMode = src.uiScaleMode;
        scaler.referenceResolution = src.referenceResolution;
        scaler.screenMatchMode = src.screenMatchMode;
        scaler.matchWidthOrHeight = src.matchWidthOrHeight;
        scaler.scaleFactor = src.scaleFactor;
        scaler.referencePixelsPerUnit = src.referencePixelsPerUnit;
        canvas.additionalShaderChannels = cardRoot.additionalShaderChannels;
    }

    public void Show(Button button, List<TooltipEntry> entries)
    {
        shownButton = button;
        shownPiece = button.GetPieceScript();
        view.Show(entries, AnchorRectFor(button), 1f);
    }

    public void Hide()
    {
        shownButton = null;
        shownPiece = null;
        view.Hide();
    }

    // 보여주던 칸이 사라졌거나 칸 위 기물이 바뀌면(이동·사망) 다시 구성한다 — 비면 숨겨진다.
    // 파괴된 기물도 "바뀐 것"으로 잡히도록 Unity null 비교가 아니라 참조 비교를 쓴다.
    void LateUpdate()
    {
        if (!view.IsShown) return;
        if (shownButton == null)
        {
            Hide();
            return;
        }
        if (!ReferenceEquals(shownButton.GetPieceScript(), shownPiece))
            Board.instance?.ShowTileTooltip(shownButton);
    }

    // 칸의 화면 영역을 앵커 로컬 좌표로 바꾼 사각형 — 좌우는 칸 폭, 윗변은 칸 윗변, 높이는 ColumnHeight.
    // 화면 위·아래로 넘치면 안쪽으로 민다.
    Rect AnchorRectFor(Button button)
    {
        Camera cam = Camera.main;
        Collider col = button.GetComponent<Collider>();
        Bounds bounds = col != null ? col.bounds : new Bounds(button.transform.position, Vector3.one);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector2 screen = cam != null ? (Vector2)cam.WorldToScreenPoint(corner) : Vector2.zero;
            min = Vector2.Min(min, screen);
            max = Vector2.Max(max, screen);
        }

        // 앵커를 칸 화면 중심으로 옮긴 뒤, 칸 화면 사각형을 앵커 로컬 좌표로 바꾼다(앵커 스케일 포함).
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, (min + max) * 0.5f, null, out Vector2 center);
        anchor.anchoredPosition = center;
        Vector2 localMin = ToAnchorLocal(min);
        Vector2 localMax = ToAnchorLocal(max);

        Rect r = Rect.MinMaxRect(localMin.x, localMax.y - ColumnHeight, localMax.x, localMax.y);
        float screenBottom = ToAnchorLocal(Vector2.zero).y;
        float screenTop = ToAnchorLocal(new Vector2(0f, Screen.height)).y;
        if (r.yMin < screenBottom) r.y += screenBottom - r.yMin;
        if (r.yMax > screenTop) r.y -= r.yMax - screenTop;
        return r;
    }

    Vector2 ToAnchorLocal(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(anchor, screen, null, out Vector2 local);
        return local;
    }
}
