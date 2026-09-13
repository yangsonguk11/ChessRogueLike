using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ISelectable
{
    bool selected { get; set; }
    public abstract bool IsSelectable();
    public void SelectedFalse()
    {
        selected = false;
        ScaleDefault();
    }
    public void SelectedTrue()
    {
        selected = true;
        ScaleHover();
    }
    public IEnumerator ScaleTo(Vector3 target);
    public void ScaleDefault();
    public void ScaleHover();

    // 호버 진입/이탈 동작 — RegisterEventTrigger가 이 둘을 호출하므로 모든 구현체가 반드시 정의해야 한다.
    void MouseEnter();
    void MouseExit();

    // EventTrigger에 PointerEnter/PointerExit를 등록해 MouseEnter/MouseExit로 연결한다. 구현체의
    // Awake() 등에서 한 번 호출하면, 프리팹의 인스펙터에서 손으로 이벤트를 연결할 필요가 없어진다.
    // 구현체가 Component(MonoBehaviour)여야 동작한다 — Card/CardButton/NodeButton/Button이 전부 그렇다.
    public void RegisterEventTrigger()
    {
        Component self = this as Component;
        if (self == null) return;

        EventTrigger trigger = self.GetComponent<EventTrigger>();
        if (trigger == null) trigger = self.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => MouseEnter());
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => MouseExit());
        trigger.triggers.Add(exitEntry);
    }
}

// Card, CardButton, NodeButton, Button(보드 칸)이 공유하는 호버/선택 스케일 전환 코루틴.
public static class ScaleAnimator
{
    // speed는 기존 점근적 Lerp의 "매프레임 보간 비율" 파라미터였음 — 고정 duration을 쓰는
    // DOTween으로 옮기면서 3/speed(감쇠가 대부분 끝나는 시점의 근사치)를 duration으로 역산한다.
    public static IEnumerator ScaleTo(Transform transform, Vector3 target, float speed)
    {
        float duration = Mathf.Clamp(3f / Mathf.Max(speed, 0.01f), 0.05f, 1f);
        // 호출부(Card.ScaleHover 등)가 StopAllCoroutines()로 감싸는 코루틴만 멈추는 경우가 있어,
        // DOTween 트윈 자체는 별도로 죽여야 이전 호버/선택 트윈과 겹치지 않는다.
        DOTween.Kill(transform);
        yield return transform.DOScale(target, duration).SetEase(Ease.OutBack).WaitForCompletion();
    }
}
