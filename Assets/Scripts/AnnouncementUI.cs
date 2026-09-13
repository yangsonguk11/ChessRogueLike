using System.Collections;
using TMPro;
using UnityEngine;

public class AnnouncementUI : MonoBehaviour
{
    public static AnnouncementUI instance;

    [SerializeField] TextMeshProUGUI announcementText;
    [SerializeField] float displayDuration = 1.0f;
    [SerializeField] float fadeDuration = 0.3f;

    public Coroutine currentRoutine;

    void Awake()
    {
        if (instance == null) instance = this;
        if (announcementText != null)
        {
            var c = announcementText.color;
            announcementText.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    // isWarning: 기본 true — 대부분의 Show 호출이 "이럴 땐 못 함" 류의 경고 메시지라 기본값으로
    // invalidAction SFX를 재생한다. 턴/전투 시작 안내처럼 경고가 아닌 3곳만 false로 넘긴다.
    public void Show(string message, bool isWarning = true)
    {
        if (announcementText == null) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowRoutine(message));
        if (isWarning) AudioManager.instance?.PlayInvalidAction();
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
