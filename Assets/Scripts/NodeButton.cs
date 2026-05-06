using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeButton : MonoBehaviour, ISelectable
{

    public bool selected { get; set; }

    public bool IsSelectable()
    {
        throw new System.NotImplementedException();
    }
    void Awake()
    {
        defaultScale = transform.localScale;
    }
    
    Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;
    public IEnumerator ScaleTo(Vector3 target)
    {
        Debug.LogFormat("{0}, {1}", transform.localScale, target);
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }
    public void ScaleDefault()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale));
    }
    public void ScaleHover()
    {
        StopAllCoroutines();
        
        StartCoroutine(ScaleTo(defaultScale * hoverScale));
    }
    public void MouseEnter()
    {
        ScaleHover();
    }

    public void MouseExit()
    {
        if (!selected) ScaleDefault();
    }
    public void MouseDown()
    {
    }
}
