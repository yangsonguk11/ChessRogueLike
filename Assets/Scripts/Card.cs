using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

enum CardType
{
    Attack,
    Action,
}

enum TargetType
{
    Self,
    Enemy,
    Ally,
}
public abstract class Card : MonoBehaviour, ISelectable
{
    string Name, Description;
    int Cost;
    CardType type;
    TargetType target;
    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    public event Action OnSelected;
    public event Action OnUnSelected;

    bool _selected;
    public bool selected { get { return _selected; } set { _selected = value; if (_selected) OnSelected?.Invoke(); else OnUnSelected?.Invoke(); }}


    public abstract bool CanUse();
    public abstract void Execute();

    public bool IsSelectable()
    {
        return true;
    }
    public void SelectedFalse()
    {
        selected = false;
        ScaleDefault();
    }
    public void SelectedTrue()
    {
        selected = true;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        ScaleHover();
    }
    Coroutine ScaleCor;
    Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;

    public GameObject cardCanvas;
    public int handNumber;
    IEnumerator ScaleTo(Vector3 target)
    {
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
        ScaleCor = StartCoroutine(ScaleTo(defaultScale));
    }
    public void ScaleHover()
    {
        StopAllCoroutines();
        ScaleCor = StartCoroutine(ScaleTo(defaultScale * hoverScale));
    }
    public void MouseEnter()
    {
        ScaleHover();
    }

    public void MouseExit()
    {
        if (!selected) ScaleDefault();
    }
    public void MouseDown(BaseEventData data)
    {
        if (!selected) { cardCanvas.GetComponent<CardCanvas>().CardSelected(handNumber); }
    }
    public void MouseUp()
    {
        if (selected) SelectedFalse();
    }

    public void MouseDrag(BaseEventData data)
    {
        PointerEventData pointerData = (PointerEventData)data;

        Vector2 screenPos = pointerData.position;

        Vector2 delta = pointerData.delta;

        Debug.Log($"마우스 위치: {screenPos}, 움직임 정도: {delta}");

        this.transform.position = screenPos;
    }
}
