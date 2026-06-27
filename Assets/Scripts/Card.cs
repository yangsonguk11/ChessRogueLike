using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum CardType
{
    Attack,
    Skill,
    Move,
}

public enum DragDropTarget
{
    Ally,       // teamID == 0 인 아군 기물
    Enemy,      // teamID != 0 인 적 기물
    AnyPiece,   // 아군/적 관계없이 기물이 있는 칸
    AnyTile,    // 보드 위 어느 칸이든
}

public static class CardTypeExtensions
{
    public static string ToDisplayString(this CardType type) => type switch
    {
        CardType.Attack => "공격",
        CardType.Skill  => "스킬",
        CardType.Move   => "이동",
        _ => type.ToString()
    };
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
    public virtual string EffectDescription => "";
    public int Cost;
    public CardType type;
    public List<CardEffect> effects = new List<CardEffect>();
    public User user;
    public bool shieldOnMoveAttack;
    public int moveAttackShieldAmount;
    public bool blocksMovementAfterUse;    // 사용 후 이번 턴 이동 불가
    public bool requiresCasterNotMoved;   // 사용자가 이번 턴에 이동하지 않았어야 사용 가능
    public bool exileOnUse;             // 사용 후 소멸
    public DragDropTarget dragDropTarget = DragDropTarget.Ally;

    // 코스트 임시 변경 추적 (-1이면 미변경)
    public int originalCost = -1;
    public CostDuration costDuration = CostDuration.Permanent;

    [Header("Card View")]
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI typeText;
    [SerializeField] TextMeshProUGUI effectText;

    CanvasGroup _canvasGroup;

    public virtual void Awake()
    {
        defaultScale = transform.localScale;
        cardCanvas = GameObject.Find("CardCanvas");
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        costText?.SetText(Cost.ToString());
        nameText?.SetText(Name);
        descriptionText?.SetText(Description);
        typeText?.SetText(type.ToDisplayString());
        effectText?.SetText(EffectDescription);
    }

    public event Action OnSelected;
    public event Action OnUnSelected;

    bool _selected;
    public bool selected { get { return _selected; } set { _selected = value; if (_selected) OnSelected?.Invoke(); else OnUnSelected?.Invoke(); }}


    protected string EffectiveDmg(CardEffect effect)
    {
        int dmg = (effect.type != EffectType.Damage || effect.useColDamageAsDmg)
            ? effect.dmg
            : Mathf.Max(0, effect.dmg + (Board.instance?.CasterColDamage ?? 0));

        if (dmg == effect.dmg) return dmg.ToString();
        string color = dmg > effect.dmg ? "#4444FF" : "#FF4444";
        return $"<color={color}>{dmg}</color>";
    }

    public virtual bool CanUse() => true;
    public virtual string GetCannotUseReason() => "사용할 수 없습니다";
    public virtual void Execute() { }

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

    [HideInInspector] public Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;

    public GameObject cardCanvas;
    public int handNumber;

    public IEnumerator ScaleTo(Vector3 target) => ScaleAnimator.ScaleTo(transform, target, speed);
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
    public System.Action<string> onClickOverride;

    public void MouseDown(BaseEventData data)
    {
        if (onClickOverride != null)
        {
            onClickOverride.Invoke(Name);
            return;
        }
        if (CardCanvas.cardSelectionMode)
        {
            CardCanvas.instance.ToggleCardInPanel(GetComponent<RectTransform>());
            return;
        }
        if (CardCanvas.instance.nowusingCard == GetComponent<RectTransform>() || handNumber < 0)
        {
            CardCanvas.instance.CancelCardUsage();
            return;
        }
        if (!selected)
        {
            CardDragArrow.instance?.Show(GetComponent<RectTransform>());
            CardCanvas.instance.CardSelected(handNumber);
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void MouseUp(BaseEventData data)
    {
        bool clearAfterDragUse = selected && CardCanvas.instance.nowusingCard == GetComponent<RectTransform>();
        if (selected) SelectedFalse();
        _canvasGroup.blocksRaycasts = true;
        if (clearAfterDragUse)
            CardCanvas.instance.OnDragCardReleased(((PointerEventData)data).position);
        else
            CardDragArrow.instance?.Hide(); // 카드를 사용하지 않고 드래그 취소
    }

    public void MouseDrag(BaseEventData data)
    {
        if (CardCanvas.instance.nowusingCard == GetComponent<RectTransform>() || handNumber < 0 || !selected)
            return;
        PointerEventData pointerData = (PointerEventData)data;

        this.transform.position = pointerData.position;

        foreach (GameObject obj in pointerData.hovered)
        {
            if (obj.name == "HandZone")
            {
                if (!CardCanvas.instance.UseCard(handNumber))
                {
                    SelectedFalse();
                    _canvasGroup.blocksRaycasts = true;
                    CardCanvas.instance.CardUnSelected();
                    CardDragArrow.instance?.Hide();
                }
                return;
            }
        }
    }

}
/// <summary>카드 선택 패널에서 선택할 존</summary>
public enum CardZone { Hand, Deck, Discard, Any }

/// <summary>코스트 변경 효과의 지속 시간</summary>
public enum CostDuration { Permanent, ThisTurnOnly, OneUse }

public enum EffectType { Move, Damage, Shield, Buff, DeBuff, Heal, SelfDamage, Draw, ApplyStatus, ApplyTurnEffect, ColDamageUp, DiscardHand, ShuffleHandToDeck, ExileHand, HandToDeckTop, SelectAndDiscard, SelectAndChangeCost, SelectAndReturnToDeck, AddCard }
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

    public bool useColDamageAsDmg;           // true면 dmg 대신 시전자의 colDamage 사용
    public bool noMoveAttack;               // true면 이동 시 충돌 공격 불가
    public int healOnHit;                   // 적을 공격할 때마다 시전자 회복량
    public string animTrigger;              // 효과 시전 시 재생할 Animator 트리거 (null이면 기본 코루틴 애니메이션 사용)

    // ApplyTurnEffect 타입에서 사용: 지정한 타이밍에 실행할 CardEffect와 지속 턴 수
    public CardEffect onTurnEndEffect;
    public int turnDuration;
    public TurnPhase turnPhase = TurnPhase.OwnTurnEnd;

    // SelectAndDiscard / SelectAndChangeCost 타입에서 사용
    public CardZone cardZone = CardZone.Hand;   // 선택 대상 존
    public int selectCount = 1;                 // 선택할 카드 수 (0 = 제한 없음)
    public int costChange = 0;                  // 코스트 변화량 (SelectAndChangeCost용)
    public CostDuration costDuration = CostDuration.Permanent; // 코스트 지속 시간

    // AddCard 타입에서 사용: 추가할 카드와 추가될 위치 (CardCanvas.CardPositionZone 재사용)
    public string addCardID;
    public CardPositionZone addCardZone = CardPositionZone.Discard;

    public CardEffect(Board.BoardMode _requiredMode, EffectType _type, int _dmg, TargetLogic _targetlogic,
        RangeInfoSO _effectRange = null, bool _lockCasterForNext = false,
        AreaTargetMode _areaTargetMode = AreaTargetMode.Fixed,
        RangeInfoSO _targetingRange = null, bool _targetingUsesMovement = false,
        StatusEffectType _statusEffectType = StatusEffectType.None,
        int _statusDuration = 0, int _statusPower = 0)
    {
        requiredMode = _requiredMode; type = _type; dmg = _dmg; targetlogic = _targetlogic;
        effectRange = _effectRange; lockCasterForNext = _lockCasterForNext; areaTargetMode = _areaTargetMode;
        targetingRange = _targetingRange; targetingUsesMovement = _targetingUsesMovement;
        statusEffectType = _statusEffectType; statusDuration = _statusDuration; statusPower = _statusPower;
    }
}
