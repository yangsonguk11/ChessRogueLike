using System;
using System.Collections;
using System.Collections.Generic;
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
    public List<CardEffect> effects;

    public virtual void Awake()
    {
        defaultScale = transform.localScale;
        effects = new List<CardEffect>();
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
        if (!selected) { cardCanvas.GetComponent<CardCanvas>().CardSelected(handNumber); gameObject.GetComponent<CanvasGroup>().blocksRaycasts = false; }
    }
    public void MouseUp(BaseEventData data)
    {
        if (selected) SelectedFalse();
        gameObject.GetComponent<CanvasGroup>().blocksRaycasts = true;
        PointerEventData pointerData = (PointerEventData)data;

        foreach(GameObject obj in pointerData.hovered)
        {
            if(obj.name == "HandZone")
                cardCanvas.GetComponent<CardCanvas>().UseCard(handNumber);
        }
    }

    public void MouseDrag(BaseEventData data)
    {
        PointerEventData pointerData = (PointerEventData)data;

        Vector2 screenPos = pointerData.position;

        Vector2 delta = pointerData.delta;

        this.transform.position = screenPos;
    }

    public void UseCard()
    {

    }
}
public enum EffectType { Move, Damage, Buff, Heal }
public class CardEffect
{
    public Board.BoardMode requiredMode; // 이 효과를 쓰기 위해 필요한 모드 (예: targeting)
    public EffectType type;
}
