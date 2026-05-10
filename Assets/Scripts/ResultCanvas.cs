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

        List<GameObject> pool = new List<GameObject>(CardDatabase.instance.spritesPrefabs);
        int count = Mathf.Min(3, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            CardButton cb = Instantiate(pool[idx], GetComponent<RectTransform>()).GetComponent<CardButton>();
            
            cb.OnSelected += (id) => GetCardOnDeck(id);
            spawnedCards.Add(cb.gameObject);
            pool.RemoveAt(idx);
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
