using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardsPanel : MonoBehaviour
{
    public enum ViewMode { SavedDeck, RuntimeDeck, Discard }

    [SerializeField] CardDatabase cardDatabase;
    [SerializeField] GameObject rootPanel;
    [SerializeField] TextMeshProUGUI titleText;

    // SavedDeck 뷰 안에서 기물을 전환하는 이전/다음 버튼. 에디터에서 만들어 연결한다.
    // UnityEngine.UI.Button으로 완전한 이름을 쓰는 이유: 이 프로젝트에 보드용 커스텀 Button 클래스가 따로 있어서
    // using UnityEngine.UI만으로는 이름이 그쪽으로 가려진다.
    [SerializeField] UnityEngine.UI.Button prevPieceButton;
    [SerializeField] UnityEngine.UI.Button nextPieceButton;

    ViewMode currentMode = ViewMode.SavedDeck;
    readonly List<GameObject> spawnedCards = new List<GameObject>();

    // SavedDeck 뷰가 어느 기물의 deckCardIDs를 보여줄지. 기물별 고유 덱 도입 이후 필요해짐.
    // prevPieceButton/nextPieceButton으로도 바꿀 수 있고, 외부 UI가 SetViewedPieceIndex를 호출해도 된다.
    int viewedPieceIndex = 0;
    public void SetViewedPieceIndex(int index)
    {
        viewedPieceIndex = index;
        if (rootPanel != null && rootPanel.activeSelf && currentMode == ViewMode.SavedDeck)
            Refresh();
    }

    public void PrevPiece() => ShiftViewedPiece(-1);
    public void NextPiece() => ShiftViewedPiece(1);

    void ShiftViewedPiece(int delta)
    {
        var pieces = DataManager.Instance.Pieces;
        if (pieces == null || pieces.Count == 0) return;
        viewedPieceIndex = (viewedPieceIndex + delta + pieces.Count) % pieces.Count;
        Refresh();
    }

    // 미리보기 상태
    GameObject dimOverlay;
    RectTransform previewCard;
    Transform previewOriginalParent;
    int previewOriginalSiblingIndex;
    Vector3 previewOriginalWorldPos;
    Vector3 previewOriginalLocalScale;
    Coroutine previewCoroutine;

    void Awake()
    {
        CardCanvas.OnPileChanged += OnPileChanged;
        prevPieceButton?.onClick.AddListener(PrevPiece);
        nextPieceButton?.onClick.AddListener(NextPiece);
    }
    void OnDestroy() => CardCanvas.OnPileChanged -= OnPileChanged;

    void OnPileChanged()
    {
        if (rootPanel != null && rootPanel.activeSelf &&
            (currentMode == ViewMode.RuntimeDeck || currentMode == ViewMode.Discard))
            Refresh();
    }

    public void ShowSavedDeck() => Toggle(ViewMode.SavedDeck);
    public void ShowDeck()      => Toggle(ViewMode.RuntimeDeck);
    public void ShowDiscard()   => Toggle(ViewMode.Discard);

    public void Close()
    {
        ClearCards();
        rootPanel?.SetActive(false);
    }

    void Toggle(ViewMode mode)
    {
        bool isOpen = rootPanel != null && rootPanel.activeSelf;
        if (isOpen && currentMode == mode) { Close(); return; }
        currentMode = mode;
        Open();
    }

    void Open()
    {
        rootPanel?.SetActive(true);
        Refresh();
    }

    void Refresh()
    {
        ClearCards();
        RectTransform container = GetComponent<RectTransform>();
        var pieces = DataManager.Instance.Pieces;

        switch (currentMode)
        {
            case ViewMode.SavedDeck:
                if (viewedPieceIndex < 0 || viewedPieceIndex >= pieces.Count) viewedPieceIndex = 0;
                List<string> ids = (viewedPieceIndex >= 0 && viewedPieceIndex < pieces.Count) ? pieces[viewedPieceIndex].deckCardIDs : null;
                if (ids != null)
                    foreach (string cardName in ids)
                    {
                        GameObject obj = cardDatabase.SpawnCard(container, cardName);
                        if (obj != null) { spawnedCards.Add(obj); RegisterPreview(obj); }
                    }
                break;

            case ViewMode.RuntimeDeck:
                if (CardCanvas.instance != null)
                    SpawnFromCards(new List<RectTransform>(CardCanvas.instance.Deckcards), container);
                break;

            case ViewMode.Discard:
                if (CardCanvas.instance != null)
                    SpawnFromCards(CardCanvas.instance.Discardcards, container);
                break;
        }

        bool showSwitcher = currentMode == ViewMode.SavedDeck && pieces != null && pieces.Count > 1;
        prevPieceButton?.gameObject.SetActive(showSwitcher);
        nextPieceButton?.gameObject.SetActive(showSwitcher);

        if (titleText != null)
        {
            string label;
            if (currentMode == ViewMode.Discard) label = "버린 카드";
            else if (currentMode == ViewMode.SavedDeck)
            {
                string pieceName = (viewedPieceIndex >= 0 && viewedPieceIndex < pieces.Count) ? pieces[viewedPieceIndex].pieceName : "";
                label = string.IsNullOrEmpty(pieceName) ? "덱" : $"{pieceName}의 덱";
            }
            else label = "덱";
            titleText.text = $"{label} ({spawnedCards.Count}장)";
        }
    }

    void SpawnFromCards(List<RectTransform> source, RectTransform container)
    {
        foreach (RectTransform rt in source)
        {
            Card card = rt.GetComponent<Card>();
            if (card == null) continue;
            GameObject obj = cardDatabase.SpawnCard(container, card.Name);
            if (obj != null) { spawnedCards.Add(obj); RegisterPreview(obj); }
        }
    }

    void ClearCards()
    {
        if (previewCard != null)
        {
            if (previewCoroutine != null) { StopCoroutine(previewCoroutine); previewCoroutine = null; }
            dimOverlay?.SetActive(false);
            previewCard = null;
        }

        foreach (GameObject obj in spawnedCards)
            if (obj != null) Destroy(obj);
        spawnedCards.Clear();
    }

    // ── 미리보기 ──────────────────────────────────────────

    void RegisterPreview(GameObject cardGO)
    {
        Card card = cardGO.GetComponent<Card>();
        if (card == null) return;
        RectTransform rt = cardGO.GetComponent<RectTransform>();
        card.onClickOverride = (_) => ShowPreview(rt);
    }

    GameObject GetOrCreateDimOverlay()
    {
        if (dimOverlay != null) return dimOverlay;

        Canvas canvas = GetComponentInParent<Canvas>();
        dimOverlay = new GameObject("DimOverlay");
        dimOverlay.transform.SetParent(canvas.transform, false);

        var rt = dimOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        var image = dimOverlay.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);

        var btn = dimOverlay.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(DismissPreview);

        dimOverlay.SetActive(false);
        return dimOverlay;
    }

    void ShowPreview(RectTransform cardRT)
    {
        if (previewCard != null) return;

        GetOrCreateDimOverlay();

        previewCard = cardRT;
        previewOriginalParent = cardRT.parent;
        previewOriginalSiblingIndex = cardRT.GetSiblingIndex();
        previewOriginalWorldPos = cardRT.position;
        previewOriginalLocalScale = cardRT.GetComponent<Card>().defaultScale; // localScale 대신 defaultScale 저장 (hover 상태 영향 없음)

        Canvas canvas = GetComponentInParent<Canvas>();
        dimOverlay.SetActive(true);
        dimOverlay.transform.SetAsLastSibling();

        cardRT.SetParent(canvas.transform, true);
        cardRT.SetAsLastSibling();

        Card cardComp = cardRT.GetComponent<Card>();
        Vector3 targetScale = previewOriginalLocalScale * 2.5f;
        cardComp.defaultScale = targetScale;
        cardRT.GetComponent<CanvasGroup>().blocksRaycasts = false;

        Vector3 centerPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        if (previewCoroutine != null) StopCoroutine(previewCoroutine);
        previewCoroutine = StartCoroutine(AnimateToPreview(cardRT, centerPos, targetScale, 0.25f));
    }

    void DismissPreview()
    {
        if (previewCard == null) return;
        if (previewCoroutine != null) StopCoroutine(previewCoroutine);

        previewCard.GetComponent<CanvasGroup>().blocksRaycasts = false;
        previewCoroutine = StartCoroutine(AnimateDismiss(previewCard, 0.2f));
    }

    IEnumerator AnimateToPreview(RectTransform card, Vector3 targetPos, Vector3 targetScale, float duration)
    {
        Vector3 startPos = card.position;
        Vector3 startScale = card.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            card.position = Vector3.Lerp(startPos, targetPos, t);
            card.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        card.position = targetPos;
        card.localScale = targetScale;
        card.GetComponent<CanvasGroup>().blocksRaycasts = true;
        previewCoroutine = null;
    }

    IEnumerator AnimateDismiss(RectTransform card, float duration)
    {
        Vector3 startPos = card.position;
        Vector3 startScale = card.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            card.position = Vector3.Lerp(startPos, previewOriginalWorldPos, t);
            card.localScale = Vector3.Lerp(startScale, previewOriginalLocalScale, t);
            yield return null;
        }

        card.SetParent(previewOriginalParent, true);
        card.SetSiblingIndex(previewOriginalSiblingIndex);
        card.localScale = previewOriginalLocalScale;

        Card cardComp = card.GetComponent<Card>();
        cardComp.defaultScale = previewOriginalLocalScale;
        card.GetComponent<CanvasGroup>().blocksRaycasts = true;

        dimOverlay.SetActive(false);
        previewCard = null;
        previewCoroutine = null;
    }
}
