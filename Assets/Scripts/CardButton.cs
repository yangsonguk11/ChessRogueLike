using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardButton : MonoBehaviour, ISelectable
{
    public event Action<string> OnSelected;
    [SerializeField] string cardName;
    bool _selected;
    public bool selected { get { return _selected; } set { _selected = value; if (_selected) OnSelected?.Invoke(cardName); } }

    public bool IsSelectable()
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        defaultScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void MouseDown(BaseEventData data)
    {
        selected = true;
    }
    public void MouseEnter()
    {
        ScaleHover();
    }

    public void MouseExit()
    {
        if (!selected) ScaleDefault();
    }
    Coroutine ScaleCor;
    Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;
    public void ScaleDefault()
    {
        StopAllCoroutines();
        ScaleCor = StartCoroutine(ScaleTo(defaultScale));
    }
    public void ScaleHover()
    {
        StopAllCoroutines();
        ScaleCor = StartCoroutine(ScaleTo(defaultScale * hoverScale));
    }
    IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }
}
