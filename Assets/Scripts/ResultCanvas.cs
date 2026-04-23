using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultCanvas : MonoBehaviour
{
    public List<GameObject> sprites = new List<GameObject>();
    public static ResultCanvas Instance;
    CanvasGroup cg;

    List<string> spritesname = new List<string>();
    private void OnEnable()
    {
        
    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        CardButton cb;
        foreach(GameObject sp in sprites)
        {
            cb = Instantiate(sp, gameObject.GetComponent<RectTransform>()).GetComponent<CardButton>();
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
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 0;
        float t = 0;
        while(t <= 1)
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
        cg.blocksRaycasts = true;
        cg.alpha = 1;
        float t = 1;
        while (t > 0)
        {
            cg.alpha = t;
            t -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        cg.alpha = 0;
        cg.blocksRaycasts = false;
    }
    public void GetCardOnDeck(string cardname)
    {
        DataManager.Instance.AddCardOnDeck(cardname);
        DisableCanvas();
        SceneManager.LoadScene("MainScene");
    }
}
