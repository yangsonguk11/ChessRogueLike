using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardsPanel : MonoBehaviour
{
    public enum ViewMode { SavedDeck, RuntimeDeck, Discard }

    [SerializeField] CardDatabase cardDatabase;
    [SerializeField] GameObject rootPanel;        // 열고 닫을 최상위 CardsPanel 오브젝트
    [SerializeField] TextMeshProUGUI titleText;   // optional — "덱 (10장)" 표시용

    ViewMode currentMode = ViewMode.SavedDeck;
    readonly List<GameObject> spawnedCards = new List<GameObject>();

    void Awake()    => CardCanvas.OnPileChanged += OnPileChanged;
    void OnDestroy() => CardCanvas.OnPileChanged -= OnPileChanged;

    void OnPileChanged()
    {
        if (rootPanel != null && rootPanel.activeSelf &&
            (currentMode == ViewMode.RuntimeDeck || currentMode == ViewMode.Discard))
            Refresh();
    }

    // ── 외부 버튼 OnClick에 연결 ──────────────────────

    public void ShowSavedDeck() => Toggle(ViewMode.SavedDeck);
    public void ShowDeck()      => Toggle(ViewMode.RuntimeDeck);
    public void ShowDiscard()   => Toggle(ViewMode.Discard);

    public void Close()
    {
        ClearCards();
        rootPanel?.SetActive(false);
    }

    // ─────────────────────────────────────────────────

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

        switch (currentMode)
        {
            case ViewMode.SavedDeck:
                foreach (string cardName in DataManager.Instance.currentData.deckCardIDs)
                {
                    GameObject obj = cardDatabase.SpawnCard(container, cardName);
                    if (obj != null) spawnedCards.Add(obj);
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

        if (titleText != null)
        {
            string label = currentMode == ViewMode.Discard ? "버린 카드" : "덱";
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
            if (obj != null) spawnedCards.Add(obj);
        }
    }

    void ClearCards()
    {
        foreach (GameObject obj in spawnedCards)
            if (obj != null) Destroy(obj);
        spawnedCards.Clear();
    }
}
