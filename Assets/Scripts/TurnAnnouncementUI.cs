using System.Collections;
using TMPro;
using UnityEngine;

public class TurnAnnouncementUI : MonoBehaviour
{
    public static TurnAnnouncementUI instance;

    [SerializeField] TextMeshProUGUI announcementText;
    [SerializeField] float displayDuration = 1.0f;
    [SerializeField] float fadeDuration = 0.3f;

    Coroutine currentRoutine;

    void Awake()
    {
        if (instance == null) instance = this;
        if (announcementText != null)
        {
            var c = announcementText.color;
            announcementText.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    public void Show(string message)
    {
        if (announcementText == null) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowRoutine(message));
    }

    public IEnumerator ShowRoutine(string message)
    {
        announcementText.text = message;

        yield return StartCoroutine(Fade(0f, 1f));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(Fade(1f, 0f));
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        Color c = announcementText.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            announcementText.color = c;
            yield return null;
        }
        c.a = to;
        announcementText.color = c;
    }
}
