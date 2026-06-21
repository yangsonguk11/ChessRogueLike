using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
