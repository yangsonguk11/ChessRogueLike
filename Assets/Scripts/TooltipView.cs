using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 호버 정보창. 호스트 RectTransform의 자식 "TooltipRoot"로 만들어져(Create) 호스트에 귀속된다 — 카드 정보창은 카드가
// 호스트라 카드와 함께 움직이고 확대·파괴되고, 보드 칸 정보창은 BoardTooltipHost의 앵커가 호스트다. 창 UI도 전부 코드로
// 만든다(프리팹/씬 작업 없음).
//
// 배치는 호스트 로컬 좌표의 앵커 사각형(카드면 보이는 카드 경계) 기준이다: 창 폭은 모두 WindowWidth로 같고, 앵커 윗변부터
// 아래로 쌓다가 다음 창이 앵커 아랫변을 넘으면 바깥쪽에 새 줄을 만들어 다시 맨 위부터 쌓는다. 오른쪽이 화면 밖이면 왼쪽에 놓는다.
//
// 정보창은 GraphicRaycaster 없는 하위 Canvas + raycastTarget=false라 마우스 입력을 전혀 받지 않는다 —
// 창이 이웃 카드를 덮어도 그 카드의 호버/클릭을 가로채지 않는다.
//
// 호버 즉시 내용·배치를 끝내 두되 투명하게 두었다가, ShowDelay 뒤에 FadeDuration 동안 페이드 인한다. 지연·페이드는
// 실제 시간 기준(SetUpdate(true))이라 게임 배속이나 히트스탑(Time.timeScale)에 영향받지 않는다.
public class TooltipView : MonoBehaviour
{
    const float ShowDelay = 0.3f;
    const float FadeDuration = 0.2f;

    // 호스트 로컬 단위(카드 원본 500×600 기준). 손패에서는 카드 스케일(0.5 × 호버 1.3)이 함께 곱해져 보인다.
    const float WindowWidth = 320f;
    const float Gap = 12f;
    const int Padding = 14;
    const float Spacing = 6f;
    const float TitleFontSize = 30f;
    const float BodyFontSize = 25f;
    const float CellSize = 26f;
    const float CellSpacing = 3f;

    // 카드 이미지 창(TooltipEntry.cardFace)에 띄우는 표시용 카드의 템플릿. 카드 프리팹은 모두 같은 그림을 쓰고, 프리팹이 없는
    // 적 행동 카드도 많아서 템플릿 하나에 원래 카드의 글자만 바꿔 보여준다(Card.ShowFaceOf).
    const string CardFaceTemplateID = "AttackCard";

    static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.92f);
    static readonly Color RangeTitleColor = new Color(0.55f, 0.8f, 1f);
    static readonly Color KeywordTitleColor = new Color(1f, 0.85f, 0.4f);
    static readonly Color StatsTitleColor = Color.white;
    static readonly Color BuffTitleColor = new Color(0f, 1f, 0.53f);       // #00FF88 — 상태 텍스트의 버프 색과 같음
    static readonly Color DebuffTitleColor = new Color(1f, 0.27f, 0.27f);  // #FF4444
    static readonly Color ActionTitleColor = new Color(0.8f, 0.7f, 1f);
    static readonly Color BodyColor = new Color(0.92f, 0.92f, 0.92f);
    static readonly Color EmptyCellColor = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color InRangeCellColor = new Color(1f, 0.55f, 0.25f);
    static readonly Color CasterCellColor = new Color(0.3f, 0.6f, 1f);
    static readonly Color TargetPointCellColor = new Color(1f, 0.85f, 0.2f);

    class Window
    {
        public RectTransform rt;
        public TextMeshProUGUI title;
        public TextMeshProUGUI body;
        public RectTransform grid;
        public GridLayoutGroup gridLayout;
        public readonly List<Image> cells = new List<Image>();
        public RectTransform cardSlot;
        public LayoutElement cardSlotLayout;
        public Card cardFace; // 표시용으로 스폰한 템플릿 카드 — 창마다 하나를 만들어 재사용
    }

    RectTransform host;
    Canvas canvas;
    CanvasGroup group; // 페이드 인용 알파
    TMP_FontAsset font;
    readonly List<Window> windows = new List<Window>();

    public static TooltipView Create(RectTransform host, TMP_FontAsset font)
    {
        var go = new GameObject("TooltipRoot", typeof(RectTransform));
        go.layer = host.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(host, false);
        // 크기 0, 호스트 피벗 위치 — 이 아래의 localPosition이 곧 호스트 로컬 좌표(host.rect 기준)다.
        rt.anchorMin = rt.anchorMax = host.pivot;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        go.AddComponent<LayoutElement>().ignoreLayout = true;

        var view = go.AddComponent<TooltipView>();
        view.host = host;
        view.font = font;
        view.canvas = go.AddComponent<Canvas>();
        view.canvas.overrideSorting = true;
        view.group = go.AddComponent<CanvasGroup>();
        view.group.alpha = 0f;
        view.group.blocksRaycasts = false;
        view.group.interactable = false;
        go.SetActive(false);
        return view;
    }

    // anchor: 창을 붙일 사각형(호스트 로컬 좌표). 창은 이 사각형 오른쪽(또는 왼쪽)에 윗변부터 쌓이고, 아랫변을 넘으면 줄을 바꾼다.
    // scaleRatio: 지금 호스트 스케일 대비 최종 스케일 비율 — 카드 확대 트윈 도중에도 최종 크기 기준으로 화면 넘침을 판정한다.
    public void Show(List<TooltipEntry> entries, Rect anchor, float scaleRatio)
    {
        if (entries.Count == 0)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true); // 레이아웃은 비활성 상태에선 계산되지 않으므로 먼저 켠다
        canvas.overrideSorting = true;
        canvas.sortingOrder = Card.SortingOrderOf(host) + 1;
        canvas.additionalShaderChannels = canvas.rootCanvas.additionalShaderChannels; // TMP용 채널을 루트와 맞춤

        var heights = new List<float>();
        for (int i = 0; i < entries.Count; i++)
        {
            Window w = GetWindow(i);
            Fill(w, entries[i]);
            w.rt.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(w.rt);
            heights.Add(w.rt.rect.height);
        }
        for (int i = entries.Count; i < windows.Count; i++)
            windows[i].rt.gameObject.SetActive(false);

        Place(heights, anchor, scaleRatio);

        // 다시 호버하면 지연부터 새로 시작한다.
        DOTween.Kill(this);
        group.alpha = 0f;
        DOTween.To(() => group.alpha, a => group.alpha = a, 1f, FadeDuration)
            .SetDelay(ShowDelay).SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
    }

    public bool IsShown => gameObject.activeSelf;

    public void Hide()
    {
        DOTween.Kill(this); // 지연 중에 마우스가 나가면 아예 뜨지 않는다
        group.alpha = 0f;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    void OnDestroy() => DOTween.Kill(this);

    // ── 배치 ────────────────────────────────────────────────

    void Place(List<float> heights, Rect r, float scaleRatio)
    {
        // 줄 나누기: 줄에 이미 창이 있는데 다음 창이 앵커 아랫변을 넘으면 새 줄. 앵커보다 긴 창은 그 줄에 혼자 둔다.
        var column = new int[heights.Count];
        var top = new float[heights.Count];
        int col = 0;
        float y = r.yMax;
        for (int i = 0; i < heights.Count; i++)
        {
            if (y < r.yMax && y - heights[i] < r.yMin)
            {
                col++;
                y = r.yMax;
            }
            column[i] = col;
            top[i] = y;
            y -= heights[i] + Gap;
        }
        int columnCount = col + 1;
        float totalWidth = columnCount * WindowWidth + (columnCount - 1) * Gap;

        bool placeRight = ChooseRightSide(r, totalWidth, scaleRatio);
        for (int i = 0; i < heights.Count; i++)
        {
            float step = column[i] * (WindowWidth + Gap);
            float x = placeRight ? r.xMax + Gap + step : r.xMin - Gap - WindowWidth - step;
            windows[i].rt.localPosition = new Vector3(x, top[i], 0f);
        }
    }

    // 앵커 오른쪽에 다 들어가면 오른쪽, 아니면 왼쪽에 다 들어갈 때 왼쪽, 둘 다 안 되면 여유가 큰 쪽.
    bool ChooseRightSide(Rect r, float totalWidth, float scaleRatio)
    {
        Canvas root = host.GetComponentInParent<Canvas>()?.rootCanvas;
        if (root == null) return true;
        Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

        Vector2 origin = RectTransformUtility.WorldToScreenPoint(cam, host.TransformPoint(Vector3.zero));
        Vector2 unitX = RectTransformUtility.WorldToScreenPoint(cam, host.TransformPoint(Vector3.right));
        float pxPerUnit = Mathf.Abs(unitX.x - origin.x) * scaleRatio;
        if (pxPerUnit <= 0f) return true;

        float rightEdge = origin.x + (r.xMax + Gap + totalWidth) * pxPerUnit;
        float leftEdge = origin.x + (r.xMin - Gap - totalWidth) * pxPerUnit;
        if (rightEdge <= Screen.width) return true;
        if (leftEdge >= 0f) return false;
        return Screen.width - rightEdge >= leftEdge;
    }

    // ── 창 만들기 ────────────────────────────────────────────

    void Fill(Window w, TooltipEntry entry)
    {
        w.title.text = entry.title;
        w.title.color = entry.titleColor ?? entry.kind switch
        {
            TooltipEntry.Kind.Range  => RangeTitleColor,
            TooltipEntry.Kind.Stats  => StatsTitleColor,
            TooltipEntry.Kind.Buff   => BuffTitleColor,
            TooltipEntry.Kind.Debuff => DebuffTitleColor,
            TooltipEntry.Kind.Action => ActionTitleColor,
            _                        => KeywordTitleColor,
        };

        bool hasBody = !string.IsNullOrEmpty(entry.body);
        w.body.gameObject.SetActive(hasBody);
        if (hasBody) w.body.text = entry.body;

        w.grid.gameObject.SetActive(entry.grid != null);
        if (entry.grid != null) FillGrid(w, entry.grid);

        FillCardFace(w, entry.cardFace);
    }

    void FillGrid(Window w, RangeGrid grid)
    {
        int size = grid.Size;
        w.gridLayout.constraintCount = size;
        while (w.cells.Count < size * size)
            w.cells.Add(CreateGraphic<Image>("Cell", w.grid));

        for (int i = 0; i < w.cells.Count; i++)
        {
            bool used = i < size * size;
            w.cells[i].gameObject.SetActive(used);
            if (!used) continue;
            w.cells[i].color = grid.cells[i / size, i % size] switch
            {
                RangeGrid.Cell.InRange     => InRangeCellColor,
                RangeGrid.Cell.Caster      => CasterCellColor,
                RangeGrid.Cell.TargetPoint => TargetPointCellColor,
                _                          => EmptyCellColor,
            };
        }
    }

    // 카드 이미지: 템플릿 카드를 창 안쪽 폭에 맞게 줄여 슬롯에 띄우고, 원래 카드(source)의 글자를 보여준다.
    void FillCardFace(Window w, Card source)
    {
        if (source != null && w.cardFace == null)
        {
            GameObject obj = CardDatabase.instance != null ? CardDatabase.instance.SpawnCard(w.cardSlot, CardFaceTemplateID) : null;
            if (obj != null)
            {
                w.cardFace = obj.GetComponent<Card>();
                var rt = (RectTransform)obj.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
                float scale = (WindowWidth - 2 * Padding) / Mathf.Max(1f, rt.rect.width);
                rt.localScale = Vector3.one * scale;
                w.cardSlotLayout.preferredHeight = rt.rect.height * scale;
                // 보기 전용 — 입력을 받지 않는다(정보창 캔버스엔 레이캐스터도 없지만 확실히 막아 둔다).
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg != null) { cg.blocksRaycasts = false; cg.interactable = false; }
                EventTrigger trigger = obj.GetComponent<EventTrigger>();
                if (trigger != null) trigger.enabled = false;
            }
        }

        bool show = source != null && w.cardFace != null;
        w.cardSlot.gameObject.SetActive(show);
        if (show) w.cardFace.ShowFaceOf(source);
    }

    Window GetWindow(int index)
    {
        while (windows.Count <= index)
            windows.Add(CreateWindow());
        return windows[index];
    }

    Window CreateWindow()
    {
        var w = new Window();
        Image bg = CreateGraphic<Image>("Window", transform);
        bg.color = BackgroundColor;
        w.rt = bg.rectTransform;
        w.rt.anchorMin = w.rt.anchorMax = new Vector2(0.5f, 0.5f);
        w.rt.pivot = new Vector2(0f, 1f);
        w.rt.sizeDelta = new Vector2(WindowWidth, 0f);

        var layout = bg.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(Padding, Padding, Padding, Padding);
        layout.spacing = Spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        bg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        w.title = CreateText("Title", w.rt, TitleFontSize, FontStyles.Normal);
        w.body = CreateText("Body", w.rt, BodyFontSize, FontStyles.Normal);
        w.body.color = BodyColor;

        var gridGo = new GameObject("Grid", typeof(RectTransform));
        gridGo.layer = gameObject.layer;
        w.grid = (RectTransform)gridGo.transform;
        w.grid.SetParent(w.rt, false);
        w.gridLayout = gridGo.AddComponent<GridLayoutGroup>();
        w.gridLayout.cellSize = new Vector2(CellSize, CellSize);
        w.gridLayout.spacing = new Vector2(CellSpacing, CellSpacing);
        w.gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        w.gridLayout.childAlignment = TextAnchor.UpperCenter;

        var slotGo = new GameObject("CardSlot", typeof(RectTransform));
        slotGo.layer = gameObject.layer;
        w.cardSlot = (RectTransform)slotGo.transform;
        w.cardSlot.SetParent(w.rt, false);
        w.cardSlotLayout = slotGo.AddComponent<LayoutElement>();
        slotGo.SetActive(false);
        return w;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style)
    {
        TextMeshProUGUI text = CreateGraphic<TextMeshProUGUI>(name, parent);
        if (font != null) text.font = font; // TMP 기본 폰트엔 한글이 없어 카드/정보창 텍스트의 폰트를 쓴다
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    T CreateGraphic<T>(string name, Transform parent) where T : Graphic
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        T graphic = go.AddComponent<T>();
        graphic.raycastTarget = false;
        return graphic;
    }
}
