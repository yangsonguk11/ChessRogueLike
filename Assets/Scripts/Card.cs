using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    Self,       // 대상이 항상 카드를 낸 기물 자신이라 드롭 위치가 의미 없음 — 화살표도, 위치 검증도 생략
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
    AllAlliesInRange,
    AllPiecesInRange
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
    [Tooltip("필수 입력 — 대부분의 구체 카드가 Awake()에서 effectRange[0]으로 읽어서 씀. 비워두면 인덱스 접근에서 바로 예외 발생.")]
    public List<RangeInfoSO> effectRange;
    [Tooltip("거의 모든 카드가 Awake()에서 직접 덮어씀 — 여기 채워도 대부분 무시됨(해당 카드 클래스의 Awake() 확인).")]
    public string Name;
    public string Description; // 대부분 코드가 안 건드림 — 실제 플레이버 텍스트로 채워도 됨(일부 카드는 Awake에서 덮어쓰기도 하니 확인)
    public virtual string EffectDescription => "";
    [Tooltip("거의 모든 카드가 Awake()에서 직접 덮어씀 — 여기 채워도 대부분 무시됨(해당 카드 클래스의 Awake() 확인).")]
    public int Cost;
    [Tooltip("거의 모든 카드가 Awake()에서 직접 덮어씀 — 여기 채워도 대부분 무시됨(해당 카드 클래스의 Awake() 확인).")]
    public CardType type;
    public List<CardEffect> effects = new List<CardEffect>();
    [Tooltip("필수 입력 — 코드가 거의 안 건드림(예외: Enemy 전용으로만 쓰는 일부 카드는 Awake()에서 User.Enemy로 직접 고정). 적이 자동으로 쓰게 하려면 여기서 Enemy로 바꿔야 함.")]
    public User user;
    public bool shieldOnMoveAttack;
    public int moveAttackShieldAmount;
    public bool blocksMovementAfterUse;    // 사용 후 이번 턴 이동 불가
    public bool requiresCasterNotMoved;   // 사용자가 이번 턴에 이동하지 않았어야 사용 가능
    public bool exileOnUse;             // 사용 후 소멸
    [Tooltip("대부분의 카드가 Awake()에서 직접 덮어씀 — 여기 채워도 대부분 무시됨(해당 카드 클래스의 Awake() 확인).")]
    public DragDropTarget dragDropTarget = DragDropTarget.Ally;

    // CardDatabase.SpawnCard가 스폰 직후 채워주는 원본 프리팹 이름. 기물의 덱 구성을 저장 데이터로
    // 되돌릴 때(Piece.GetPieceData) 이 값으로 어떤 카드였는지 복원한다.
    [ReadOnlyInInspector] public string cardID; // 스폰 시점에 외부(CardDatabase)가 채워줌 — 프리팹엔 항상 비워둠

    // 코스트 임시 변경 추적 (-1이면 미변경)
    [ReadOnlyInInspector] public int originalCost = -1; // 콘텐츠 값이 아니라 런타임 추적용 — 프리팹에서 건드릴 필요 없음
    [ReadOnlyInInspector] public CostDuration costDuration = CostDuration.Permanent; // 위와 동일, 런타임 추적용

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
    public bool selected
    {
        get { return _selected; }
        set
        {
            _selected = value;
            if (CardCanvas.cardSelectionMode) return;
            if (_selected) OnSelected?.Invoke(); else OnUnSelected?.Invoke();
        }
    }


    protected string EffectiveDmg(CardEffect effect)
    {
        int dmg = effect switch
        {
            { type: not EffectType.Damage }          => effect.dmg,
            { useColDamageAsDmg: true }               => Mathf.Max(0, Board.instance?.CasterFullColDamage ?? 0),
            { ignoreCasterColDamageBonus: true }      => effect.dmg,
            _                                          => Mathf.Max(0, effect.dmg + (Board.instance?.CasterColDamage ?? 0)),
        };

        if (dmg == effect.dmg) return dmg.ToString();
        string color = dmg > effect.dmg ? "#4444FF" : "#FF4444";
        return $"<color={color}>{dmg}</color>";
    }

    protected string EffectiveShield(CardEffect effect)
    {
        int amount = effect.type != EffectType.Shield || effect.ignoreCasterShieldBonus
            ? effect.dmg
            : Mathf.Max(0, effect.dmg + (Board.instance?.CasterShieldBonus ?? 0));

        if (amount == effect.dmg) return amount.ToString();
        string color = amount > effect.dmg ? "#4444FF" : "#FF4444";
        return $"<color={color}>{amount}</color>";
    }

    public virtual bool CanUse() => true;
    public virtual string GetCannotUseReason() => "사용할 수 없습니다";
    public virtual void Execute() { }

    // 첫 번째 CardEffect의 requiredMode로 보드/기물 타겟팅이 필요한 카드인지 판단.
    // self 타겟 카드는 대상이 항상 카드를 낸 기물 자신이라 실질적으로 타겟팅할 게 없으므로 제외한다.
    public bool NeedsTargeting() => effects.Count > 0 && effects[0].requiredMode != Board.BoardMode.Inspect
        && dragDropTarget != DragDropTarget.Self;

    // ISelectable 계약 — 실제로 지금 손패 리스트(CardCanvas.cards)에 들어있는 카드인지로 판정한다.
    // handNumber는 카드가 손패를 떠나는 모든 경로(HandtoDiscardCount/HandtoDeckTop/HandtoExileCount 등,
    // 카드효과로 인한 이동)에서 항상 -1로 갱신된다는 보장이 없고(FinishUseCard만 명시적으로 갱신함),
    // 그 이동 연출이 보드 애니메이션 뒤로 밀려 몇 초씩 늦게 재생될 수도 있어(motionQueue 통합) 더더욱
    // 신뢰할 수 없다. 리스트 소속은 로직 단계에서 항상 즉시 정확하므로 이걸 직접 확인한다 — 연출이 아직
    // 시작되지 않아 화면엔 손패 자리에 남아있어도, 로직상 이미 빠진 카드는 선택되지 않는다.
    public bool IsSelectable() =>
        CardCanvas.instance != null && CardCanvas.instance.cards.Contains(GetComponent<RectTransform>());
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
        ClearHoverLift(); // 잡는 순간엔 리프트 없이 원래처럼 스케일만
    }

    [HideInInspector] public Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;
    [SerializeField] float hoverLift = 20f;
    bool isHoverLifted;
    float restY; // 리프트 안 걸렸을 때의 "바닥" localPosition.y

    public GameObject cardCanvas;
    public int handNumber;

    // ISelectable 인터페이스 계약이라 유지 — 폴리모픽하게 호출하는 곳은 없지만 다른 구현체와 형태를 맞춰둔다.
    public IEnumerator ScaleTo(Vector3 target) => ScaleAnimator.ScaleTo(transform, target, speed);

    float ScaleDuration => Mathf.Clamp(3f / Mathf.Max(speed, 0.01f), 0.05f, 1f);

    public void ScaleDefault()
    {
        DOTween.Kill(transform);
        transform.DOScale(defaultScale, ScaleDuration).SetEase(Ease.OutBack);
    }
    public void ScaleHover()
    {
        DOTween.Kill(transform);
        transform.DOScale(defaultScale * hoverScale, ScaleDuration).SetEase(Ease.OutBack);
    }

    // 호버 리프트용 — 선택(드래그) 중인 카드는 리프트를 걸지 않는다는 조건만 IsSelectable에 추가.
    bool IsHandCard() => !selected && IsSelectable();

    // 리프트는 SetId(this)로 관리되는 제네릭 트윈이라(target이 transform이 아님) ScaleHover/ScaleDefault의
    // DOTween.Kill(transform)에 걸리지 않는다 — 그래서 호출 순서를 신경 쓸 필요가 없다.
    // restY 갱신은 DOTween.IsTweening(this)로 "이전 리프트 트윈이 이미 끝났는지"를 확인해서만 한다 —
    // 아직 살아있는 도중(반복 호버로 중간에 다시 걸린 경우)엔 갱신하지 않아야 드리프트가 안 생긴다.
    const float LiftDuration = 0.12f;

    void ApplyHoverLift()
    {
        if (isHoverLifted) return;
        isHoverLifted = true;
        if (!DOTween.IsTweening(this))
            restY = transform.localPosition.y;
        DOTween.Kill(this);
        DOTween.To(() => transform.localPosition.y,
            y => { Vector3 p = transform.localPosition; p.y = y; transform.localPosition = p; },
            restY + hoverLift, LiftDuration).SetEase(Ease.OutQuad).SetId(this);
    }

    void ClearHoverLift()
    {
        if (!isHoverLifted) return;
        isHoverLifted = false;
        DOTween.Kill(this);
        DOTween.To(() => transform.localPosition.y,
            y => { Vector3 p = transform.localPosition; p.y = y; transform.localPosition = p; },
            restY, LiftDuration).SetEase(Ease.OutQuad).SetId(this);
    }

    public void MouseEnter()
    {
        // 카드 선택 패널(cardSelectionMode)은 손패가 아닌 덱/버림더미 카드도 보여주므로 그때는
        // IsSelectable(cards 소속 여부)과 무관하게 호버를 허용한다. 그 외(평소 손패 화면)에는
        // 로직상 이미 손패를 떠났지만 연출 대기 중이라 화면에 남아있는 카드는 호버 자체가 되면 안 된다.
        if (!CardCanvas.cardSelectionMode && !IsSelectable()) return;
        AudioManager.instance?.PlayCardHover();
        ScaleHover();
        if (IsHandCard())
            ApplyHoverLift();
    }

    public void MouseExit()
    {
        bool keepScale = selected
            || (CardCanvas.cardSelectionMode && CardCanvas.instance.IsSelectedInPanel(GetComponent<RectTransform>()));
        if (!keepScale)
            ScaleDefault();
        ClearHoverLift(); // selected여도 리프트는 항상 풀어준다 — 선택 중엔 스케일만 유지되는 게 맞음
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
        // 로직상 이미 손패를 떠난 카드(연출 대기 중이라 화면엔 아직 손패 자리에 남아있을 수 있음)는
        // 집을 수 없다.
        if (!selected && IsSelectable())
        {
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
        if (handNumber < 0 || !selected)
            return;
        PointerEventData pointerData = (PointerEventData)data;

        if (CardCanvas.instance.nowusingCard == GetComponent<RectTransform>())
        {
            // 이미 커밋된(보드에서 사용 확정된) 카드는 손패 리스트를 떠나있는 게 정상이라 IsSelectable
            // 검사 대상이 아니다.
            CardCanvas.instance.HandleCommittedCardDrag(pointerData.position);
            return;
        }

        // 아직 커밋 전(손패에서 막 집기만 한) 카드인데, 그 사이 다른 카드효과가 손패에서 빼갔다면 중단.
        if (!IsSelectable()) return;

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
public enum CardZone { Hand, Deck, Discard, Any, SavedDeck }

/// <summary>코스트 변경 효과의 지속 시간</summary>
public enum CostDuration { Permanent, ThisTurnOnly, OneUse }

public enum EffectType { Move, Damage, Shield, Heal, SelfDamage, Draw, ApplyStatus, ApplyTurnEffect, ColDamageUp, BaseColDamageUp, ShieldBonusUp, BaseShieldBonusUp, DiscardHand, ShuffleHandToDeck, ExileHand, HandToDeckTop, SelectAndDiscard, SelectAndChangeCost, SelectAndReturnToDeck, AddCard, RestoreEnergy, Cleanse, Charge, Stun, Summon }
public record CardEffect
{
    public Board.BoardMode requiredMode { get; init; }
    public EffectType type { get; init; }
    public int dmg { get; init; }
    public RangeInfoSO effectRange { get; init; }
    public TargetLogic targetlogic { get; init; }
    public bool lockCasterForNext { get; init; }
    public AreaTargetMode areaTargetMode { get; init; } = AreaTargetMode.Fixed;
    public RangeInfoSO targetingRange { get; init; }      // AoE 중심 배치 가능 범위 (null = 전체 보드)
    public bool targetingUsesMovement { get; init; }      // true면 캐릭터 이동 범위로 AoE 중심 제한

    // 상태이상 부여 (type이 ApplyStatus이거나 다른 효과와 함께 사용)
    public StatusEffectType statusEffectType { get; init; } = StatusEffectType.None;
    public int statusDuration { get; init; }
    public int statusPower { get; init; }                 // 독/화상/재생의 턴당 수치, 강화/약화의 수치

    // Cleanse 타입에서 사용: false면 디버프 전체 제거(정화), true면 버프 전체 제거(디스펠)
    public bool cleanseBuffs { get; init; } = false;

    public bool useColDamageAsDmg { get; init; }           // true면 dmg 대신 시전자의 colDamage 사용
    public bool noRangeLimit { get; init; }               // true면 캐스터 위치/이동범위와 무관하게 보드 전체가 유효 대상(dragDropTarget 기준으로만 필터링)
    public bool ignoreCasterColDamageBonus { get; init; } // true면 시전자의 colDamage 보너스를 데미지에 더하지 않음
    public bool ignoreCasterShieldBonus { get; init; }    // true면 시전자의 shieldBonus 보너스를 실드량에 더하지 않음
    public bool noMoveAttack { get; init; }               // true면 이동 시 충돌 공격 불가
    public bool healOnHit { get; init; }                  // true면 적중 시 입힌 피해만큼 시전자 회복 (일반 공격/이동공격 모두 적용)
    public string animTrigger { get; init; }              // 효과 시전 시 재생할 Animator 트리거 (null이면 기본 코루틴 애니메이션 사용)

    // ApplyTurnEffect 타입에서 사용: 지정한 타이밍에 실행할 CardEffect와 지속 턴 수
    public CardEffect onTurnEndEffect { get; init; }
    public int turnDuration { get; init; }
    public TurnPhase turnPhase { get; init; } = TurnPhase.OwnTurnEnd;

    // 이 CardEffect가 TurnEffect.cardEffect로 감싸질 때(= onTurnEndEffect로 쓰이거나, CreateStatusEffect의
    // TurnDamageStart/End·TurnAoEDamageStart/End가 즉석으로 만드는 내부 CardEffect일 때) 그 상태가
    // 버프인지 디버프인지를 미리 명시한다 — type만으로는 판단할 수 없음(예: Damage는 자기 자신 대상이면
    // 디버프지만 적 대상이면 버프). TurnEffect.IsBuff가 이 값을 그대로 읽는다. 그 외의 곳에서는 안 쓰임.
    public bool isBuff { get; init; }

    // Damage 계열 효과가 대상을 처치했을 때 시전자를 대상으로 실행할 CardEffect (예: 처치 시 ColDamageUp)
    public CardEffect onKillEffect { get; init; }

    // SelectAndDiscard / SelectAndChangeCost 타입에서 사용
    public CardZone cardZone { get; init; } = CardZone.Hand;   // 선택 대상 존
    public int selectCount { get; init; } = 1;                 // 선택할 카드 수 (0 = 제한 없음)

    // 0보다 크면 (requiredMode는 보통 Inspect) UseCard 이후 카드 효과 처리 중에 보드에서 아군 기물을
    // 이 수치만큼 직접 클릭해 고르게 한다(Board.RequestPieceSelection). 고른 기물 각각에게 이 CardEffect
    // 자신이 그대로 적용된다 (ExecuteCardEffectOnPiece가 지원하는 타입만: Heal/Shield/ColDamageUp류 등).
    public int pieceSelectCount { get; init; }
    public bool excludeCasterFromPieceSelection { get; init; } // true면 pieceSelectCount 선택 대상에서 카드를 낸 기물 자신을 제외
    public int costChange { get; init; } = 0;                  // 코스트 변화량 (SelectAndChangeCost용)
    public CostDuration costDuration { get; init; } = CostDuration.Permanent; // 코스트 지속 시간

    // AddCard 타입에서 사용: 추가할 카드와 추가될 위치 (CardCanvas.CardPositionZone 재사용)
    public string addCardID { get; init; }
    public CardPositionZone addCardZone { get; init; } = CardPositionZone.Discard;

    // Summon 타입에서 사용: 소환할 기물의 템플릿 (PieceName/스탯/기본덱 등을 담은 SO).
    // PieceInfo.TeamID가 소환된 기물의 진영을 결정하므로 적/아군 카드 양쪽에서 재사용 가능.
    public PieceInfo summonPieceInfo { get; init; }
}
