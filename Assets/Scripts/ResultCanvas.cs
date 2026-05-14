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

    void SpawnRandomCards()
    {
        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        int excludeStart = 0, excludeEnd = 2; // 제외할 범위 [excludeStart, excludeEnd)
        var source = CardDatabase.instance.spritesPrefabs;
        List<GameObject> pool = new List<GameObject>();
        for (int i = 0; i < source.Count; i++)
            if (i < excludeStart || i >= excludeEnd) pool.Add(source[i]);
        int count = Mathf.Min(3, pool.Count);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            GameObject prefab = pool[idx];
            pool.RemoveAt(idx);

            if (prefab == null) { i--; continue; }

            GameObject spawned = Instantiate(prefab, GetComponent<RectTransform>());
            CardButton cb = spawned.GetComponent<CardButton>();
            if (cb == null) { Destroy(spawned); i--; continue; }

            cb.OnSelected += (id) => GetCardOnDeck(id);
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
        DataManager.Instance.AddCardOnDeck(cardname);
        StartCoroutine(FadeOutThenShowMap());
    }

    IEnumerator FadeOutThenShowMap()
    {
        yield return StartCoroutine(OffActive());
        MapCanvas.instance.Show();
    }
}
