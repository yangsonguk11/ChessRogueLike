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

public enum User
{
    Ally,
    Enemy
}

public enum TargetLogic
{
    NearestEnemy,
    LowestHP,
    self,
    AllEnemiesInRange,
    AllAlliesInRange
}

public enum AreaTargetMode
{
    Fixed,          // 기존 방식 — 클릭한 칸 기준 고정 범위
    MouseCentered,  // 마우스를 올린 칸을 중심으로 범위 미리보기
    Directional4,   // 시전자 기준 4방향으로 패턴 회전
    Directional8    // 시전자 기준 8방향으로 패턴 회전
}
public abstract class Card : MonoBehaviour, ISelectable
{
    public List<RangeInfoSO> effectRange;
    public string Name, Description;
    public int Cost;
    CardType type;
    TargetType target;
    public List<CardEffect> effects;
    public User user;
    public bool shieldOnMoveAttack;   // 이동공격 발생 시 시전자에게 방어도 부여
    public int moveAttackShieldAmount;
    public virtual void Awake()
    {
        defaultScale = transform.localScale;
        effects = new List<CardEffect>();
        cardCanvas = GameObject.Find("CardCanvas");
    }

    public event Action OnSelected;
    public event Action OnUnSelected;

    bool _selected;
    public bool selected { get { return _selected; } set { _selected = value; if (_selected) OnSelected?.Invoke(); else OnUnSelected?.Invoke(); }}


    public abstract bool CanUse();
    public abstract void Execute();

    public void Init()
    {

    }
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

    public IEnumerator ScaleTo(Vector3 target)
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
        if (handNumber == -1)
            return;
        PointerEventData pointerData = (PointerEventData)data;

        Vector2 screenPos = pointerData.position;

        Vector2 delta = pointerData.delta;

        this.transform.position = screenPos;
    }

}
public enum EffectType { Move, Damage, Shield, Buff, Heal, SelfDamage, Draw }
public class CardEffect
{
    public Board.BoardMode requiredMode;
    public EffectType type;
    public int dmg;
    public RangeInfoSO effectRange;
    public TargetLogic targetlogic;
    public bool lockCasterForNext;
    public AreaTargetMode areaTargetMode;

    public CardEffect(Board.BoardMode _requiredMode, EffectType _type, int _dmg, TargetLogic _targetlogic, RangeInfoSO _effectRange = null, bool _lockCasterForNext = false, AreaTargetMode _areaTargetMode = AreaTargetMode.Fixed)
    {
        requiredMode = _requiredMode; type = _type; dmg = _dmg; targetlogic = _targetlogic; effectRange = _effectRange; lockCasterForNext = _lockCasterForNext; areaTargetMode = _areaTargetMode;
    }
}
