using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class SlideInFromBottom : MonoBehaviour
{
    [SerializeField] float duration = 0.4f;
    [SerializeField] float distance = 300f;
    [SerializeField] Ease ease = Ease.OutBack;
    [SerializeField] bool useUnscaledTime = true;

    RectTransform rect;
    Vector2 shownPos;
    Tween tween;

    void Awake()
    {
        rect = (RectTransform)transform;
        shownPos = rect.anchoredPosition;
    }

    void OnEnable()
    {
        Play();
    }

    void OnDisable()
    {
        tween?.Kill();
    }

    public void Play()
    {
        tween?.Kill();
        rect.anchoredPosition = shownPos + Vector2.down * distance;
        tween = rect.DOAnchorPos(shownPos, duration)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime);
    }
}
