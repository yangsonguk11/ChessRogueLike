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
    public bool shieldOnMoveAttack;
    public int moveAttackShieldAmount;
    public bool blocksMovementAfterUse; // 사용 후 이번 턴 이동 불가
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


    public virtual bool CanUse() => true;
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
public enum EffectType { Move, Damage, Shield, Buff, Heal, SelfDamage, Draw, ApplyStatus, ApplyTurnEffect }
public class CardEffect
{
    public Board.BoardMode requiredMode;
    public EffectType type;
    public int dmg;
    public RangeInfoSO effectRange;
    public TargetLogic targetlogic;
    public bool lockCasterForNext;
    public AreaTargetMode areaTargetMode;
    public RangeInfoSO targetingRange;      // AoE 중심 배치 가능 범위 (null = 전체 보드)
    public bool targetingUsesMovement;      // true면 캐릭터 이동 범위로 AoE 중심 제한

    // 상태이상 부여 (type이 ApplyStatus이거나 다른 효과와 함께 사용)
    public StatusEffectType statusEffectType;
    public int statusDuration;
    public int statusPower;                 // 독/화상/재생의 턴당 수치, 강화/약화의 수치

    public AnimationClip animationClip;     // null이면 기본 하드코딩 애니메이션 사용

    // ApplyTurnEffect 타입에서 사용: 턴 종료마다 실행할 CardEffect와 지속 턴 수
    public CardEffect onTurnEndEffect;
    public int turnDuration;

    public CardEffect(Board.BoardMode _requiredMode, EffectType _type, int _dmg, TargetLogic _targetlogic,
        RangeInfoSO _effectRange = null, bool _lockCasterForNext = false,
        AreaTargetMode _areaTargetMode = AreaTargetMode.Fixed,
        RangeInfoSO _targetingRange = null, bool _targetingUsesMovement = false,
        StatusEffectType _statusEffectType = StatusEffectType.None,
        int _statusDuration = 0, int _statusPower = 0,
        AnimationClip _animationClip = null)
    {
        requiredMode = _requiredMode; type = _type; dmg = _dmg; targetlogic = _targetlogic;
        effectRange = _effectRange; lockCasterForNext = _lockCasterForNext; areaTargetMode = _areaTargetMode;
        targetingRange = _targetingRange; targetingUsesMovement = _targetingUsesMovement;
        statusEffectType = _statusEffectType; statusDuration = _statusDuration; statusPower = _statusPower;
        animationClip = _animationClip;
    }
}
