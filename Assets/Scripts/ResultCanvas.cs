using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultCanvas : MonoBehaviour
{
    public static ResultCanvas Instance;
    CanvasGroup cg;
    List<GameObject> spawnedCards = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0;
    }

    // 시작 덱 카드(AttackCard/DefenseCard/MoveCard)는 이미 누구나 갖고 있으므로 보상 후보에서 제외한다.
    static readonly string[] ExcludedFromRewards = { "AttackCard", "DefenseCard", "MoveCard" };

    void SpawnRandomCards()
    {
        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        ICardDatabase cardDb = CardDatabase.instance;
        List<string> picks = cardDb.PickRandomDistinct(3, ExcludedFromRewards);

        foreach (string cardName in picks)
        {
            GameObject spawned = cardDb.SpawnCard(GetComponent<RectTransform>(), cardName);
            if (spawned == null) continue;

            Card card = spawned.GetComponent<Card>();
            if (card == null) { Destroy(spawned); continue; }

            card.onClickOverride = (name) => GetCardOnDeck(name);
            spawnedCards.Add(spawned);
        }
    }

    public void EnableCanvas()
    {
        SpawnRandomCards();
        StartCoroutine(OnActive());
    }

    public void DisableCanvas()
    {
        StartCoroutine(OffActive());
    }

    IEnumerator OnActive()
    {
        cg.alpha = 0;
        float t = 0;
        while (t <= 1)
        {
            cg.alpha = t;
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        cg.alpha = 1;
        cg.blocksRaycasts = true;
    }

    IEnumerator OffActive()
    {
        cg.blocksRaycasts = false;
        float t = 1;
        while (t > 0)
        {
            cg.alpha = t;
            t -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        cg.alpha = 0;

        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();
    }

    public void GetCardOnDeck(string cardname)
    {
        PieceTargetPickerUI.instance.Show(DataManager.Instance.Pieces, pieceIndex =>
        {
            DataManager.Instance.AddCardToPieceDeck(pieceIndex, cardname);
            StartCoroutine(FadeOutThenShowMap());
        });
    }

    IEnumerator FadeOutThenShowMap()
    {
        yield return StartCoroutine(OffActive());
        MapCanvas.instance.Show();
    }
}
