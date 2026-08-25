using System.Collections;
using System.Collections.Generic;
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
    public static IEnumerator ScaleTo(Transform transform, Vector3 target, float speed)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }
}
