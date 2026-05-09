using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultCanvas : MonoBehaviour
{
    public List<GameObject> sprites = new List<GameObject>();
    public static ResultCanvas Instance;
    CanvasGroup cg;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        foreach (GameObject sp in sprites)
        {
            CardButton cb = Instantiate(sp, GetComponent<RectTransform>()).GetComponent<CardButton>();
            cb.OnSelected += (id) => GetCardOnDeck(id);
        }
        cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0;
    }

    public void EnableCanvas()
    {
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
        cg.alpha = 1;
        float t = 1;
        while (t > 0)
        {
            cg.alpha = t;
            t -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        cg.alpha = 0;
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
