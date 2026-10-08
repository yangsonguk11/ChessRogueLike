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

// 카드 희귀도. 상점 가격(ShopCanvas)과 진열 보장(희귀 1장 이상)에 쓰인다.
public enum CardRarity
{
    Common,
    Uncommon,
    Rare,
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
    // 필드가 아닌 프로퍼티라 Awake 없이 프리팹 컴포넌트에서 바로 읽힌다(상점이 스폰 전에 진열을 고를 때 사용).
    // 기본은 일반이고, 희귀 카드만 오버라이드한다.
    public virtual CardRarity Rarity => CardRarity.Common;
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

    public virtual void Awake()
    {
        defaultScale = transform.localScale;
        cardCanvas = GameObject.Find("CardCanvas");
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
        // 이동공격 판정 공격은 카드를 낸 기물에 걸린 다음 이동공격 버프(가산·배율)까지 반영해 보여준다.
        if (effect.type == EffectType.Damage && effect.countsAsMoveAttack)
        {
            MoveAttackBonus bonus = CardCanvas.instance?.ActivePiece?.PeekNextMoveAttackBonus();
            if (bonus != null) dmg = bonus.Apply(dmg);
        }

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

    // 이 카드가 이번 사용 후 소멸(Exile)할지 판단한다. 기본은 정적 플래그 exileOnUse 그대로 반환.
    // 사용 결과(예: 코스트가 0이 됐는지)에 따라 동적으로 결정하고 싶은 카드만 오버라이드한다.
    public virtual bool ShouldExileOnUse() => exileOnUse;

    // 첫 번째 CardEffect의 무덤 비용을 caster가 감당할 수 있는지 — 부족하면 카드 사용 자체가 막힌다.
    // 2번째 이후 효과의 비용은 여기서 보지 않는다(실행 시점에 부족하면 그 효과만 스킵).
    public bool HasGraveForFirstEffect(Piece caster) =>
        effects.Count == 0 || effects[0].graveCost <= 0 || (caster != null && caster.HasGrave(effects[0].graveCost));

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
        CancelHoverPose(); // 잡은 뒤엔 마우스가 위치를 정하므로 호버 자세를 되돌리는 트윈 없이 버린다
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        ScaleHover();
        SetFront(true);    // 호버로 꺼낸 상태를 드래그 중에도 유지 — 잡자마자 MainCanvas UI 뒤로 숨지 않게
    }

    [HideInInspector] public Vector3 defaultScale;
    float hoverScale = 1.3f;
    float speed = 10f;
    [SerializeField] float hoverLift = 20f;
    bool inHoverPose;         // 호버 의도 — 이동 연출 중에 들어온 호버도 기록해 두고, 연출이 끝나 정착할 때 반영한다
    Vector3 restPos;          // 정렬(AlignCards 등)이 정해 준 슬롯 localPosition
    Quaternion restRotation;  // 정렬이 정해 준 슬롯 기울기

    public GameObject cardCanvas;
    public int handNumber;

    // ISelectable 인터페이스 계약이라 유지 — 폴리모픽하게 호출하는 곳은 없지만 다른 구현체와 형태를 맞춰둔다.
    public IEnumerator ScaleTo(Vector3 target) => ScaleAnimator.ScaleTo(transform, target, speed);

    float ScaleDuration => Mathf.Clamp(3f / Mathf.Max(speed, 0.01f), 0.05f, 1f);

    // 확대가 풀리는 모든 경로(호버 해제·선택 해제·선택 패널 정리)에서 앞으로 꺼낸 상태도 같이 푼다.
    public void ScaleDefault()
    {
        DOTween.Kill(transform);
        transform.DOScale(defaultScale, ScaleDuration).SetEase(Ease.OutBack);
        SetFront(false);
    }
    public void ScaleHover()
    {
        DOTween.Kill(transform);
        transform.DOScale(defaultScale * hoverScale, ScaleDuration).SetEase(Ease.OutBack);
    }

    // 호버 자세용 — 선택(드래그) 중인 카드는 호버 자세를 걸지 않는다는 조건만 IsSelectable에 추가.
    bool IsHandCard() => !selected && IsSelectable();

    // 손패 안에서의 자세(슬롯 정렬 + 호버)는 카드 하나당 제네릭 트윈 하나(SetId(this), target 없음)로 움직인다.
    // 새 목표가 오면 진행 중인 트윈을 죽이고 현재 위치에서 이어 가므로 겹쳐도 중간에 멈추지 않는다. target이
    // transform이 아니라 ScaleHover/ScaleDefault의 DOTween.Kill(transform)에도 걸리지 않는다.
    // 날아오거나 나가는 이동 연출(CardCanvas.activeCardMoves) 중에는 슬롯·호버 의도만 기록하고, 연출이 끝나면
    // CardCanvas가 SettleToHandPose를 불러 마저 정착시킨다.
    const float LiftDuration = 0.12f;
    const float AlignDuration = 0.2f;

    void ApplyHoverPose()
    {
        if (inHoverPose) return;
        inHoverPose = true;
        SettleToHandPose(LiftDuration);
    }

    void ClearHoverPose()
    {
        if (!inHoverPose) return;
        inHoverPose = false;
        SettleToHandPose(LiftDuration);
    }

    // 지금 있어야 할 자세(슬롯, 호버 중이면 호버 자세)로 트윈한다. 잡혀 있거나(마우스가 위치를 정함) 이동 연출
    // 중이면 아무것도 하지 않는다.
    public void SettleToHandPose(float duration = AlignDuration)
    {
        if (selected || CardCanvas.instance == null || CardCanvas.instance.IsCardMoving(GetComponent<RectTransform>())) return;
        if (inHoverPose)
            // 최소 hoverLift만큼, 그리고 확대된 카드의 아랫변이 부모(손패는 화면) 아래 끝에 잘리지 않을 만큼 올리고 똑바로 세운다.
            TweenPose(new Vector3(restPos.x, Mathf.Max(restPos.y + hoverLift, ((RectTransform)transform.parent).rect.yMin + ((RectTransform)transform).rect.height * defaultScale.y * hoverScale / 2f), restPos.z), Quaternion.identity, duration);
        else
            TweenPose(restPos, restRotation, duration);
    }

    void TweenPose(Vector3 pos, Quaternion rot, float duration)
    {
        DOTween.Kill(this);
        Vector3 fromPos = transform.localPosition;
        Quaternion fromRot = transform.localRotation;
        DOTween.To(() => 0f, t =>
        {
            transform.localPosition = Vector3.LerpUnclamped(fromPos, pos, t);
            transform.localRotation = Quaternion.SlerpUnclamped(fromRot, rot, t);
        }, 1f, duration).SetEase(Ease.OutQuad).SetId(this);
    }

    // 카드를 다른 곳(마우스·이동 연출·선택 패널)이 직접 배치할 때 호출 — 되돌리는 트윈 없이 호버 자세만 버린다.
    public void CancelHoverPose()
    {
        DOTween.Kill(this);
        inHoverPose = false;
    }

    // AlignCards/ExcludeAlignCards(와 선택 패널 배치)가 슬롯 자세를 정할 때 호출. 슬롯을 기억하고 그쪽으로 미끄러진다.
    // snap이면 트윈 없이 바로 놓는다(선택 패널은 지금처럼 즉시 배치).
    public void SetHandPose(Vector3 pos, Quaternion rot, bool snap = false)
    {
        restPos = pos;
        restRotation = rot;
        if (snap)
        {
            DOTween.Kill(this);
            transform.localPosition = pos;
            transform.localRotation = rot;
            return;
        }
        SettleToHandPose();
    }

    // 손패로 들어오는 등장 연출(드로우·손패 추가)용 트윈. 출발 자세는 실제로 움직이기 시작할 때(딜레이가 끝난 뒤) 잡고,
    // 목표는 매 프레임 지금 슬롯(rest)을 읽는다 — 날아오는 중에 손패가 다시 정렬돼도 멈추지 않고 새 자리로 휘어 들어간다.
    public Tween HandSlotTween(float duration)
    {
        bool started = false;
        Vector3 fromPos = default;
        Quaternion fromRot = default;
        return DOTween.To(() => 0f, t =>
        {
            if (!started)
            {
                started = true;
                fromPos = transform.localPosition;
                fromRot = transform.localRotation;
            }
            transform.localPosition = Vector3.LerpUnclamped(fromPos, restPos, t);
            transform.localRotation = Quaternion.SlerpUnclamped(fromRot, restRotation, t);
        }, 1f, duration).SetEase(Ease.OutCubic);
    }

    // 호버·드래그 중인 손패 카드를 이웃 카드(와 MainCanvas UI)보다 앞에 그린다. 형제 순서는 AlignCards/
    // ExcludeAlignCards가 부채꼴 순서·handNumber와 함께 관리하므로 건드리지 않고, 카드에 붙인 하위 Canvas의
    // overrideSorting만 켜고 끈다(카드 선택 패널·CardFxLayer와 같은 방식). 끄면 부모 Canvas 정렬을 그대로 따른다.
    // 손패 카드만 필요하므로 처음 꺼낼 때 붙인다 — 적 행동 카드(AutoPiece) 같은 UI 밖 카드에는 붙지 않는다.
    Canvas frontCanvas;

    void SetFront(bool front)
    {
        if (front && frontCanvas == null)
        {
            frontCanvas = gameObject.AddComponent<Canvas>();
            frontCanvas.additionalShaderChannels = frontCanvas.rootCanvas.additionalShaderChannels; // TMP용 채널을 루트와 맞춤
            // 하위 Canvas의 그래픽은 부모 Canvas의 레이캐스터가 잡지 못하므로 EventTrigger가 계속 동작하도록 따로 붙인다.
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        if (frontCanvas == null || frontCanvas.overrideSorting == front) return;
        frontCanvas.overrideSorting = front;
        if (front) frontCanvas.sortingOrder = CardCanvas.instance.HoveredCardSortingOrder;
    }

    // 호버 중에 비활성화되면(기물 전환 등) MouseExit이 오지 않으므로 여기서 풀어 둔다.
    void OnDisable() => SetFront(false);

    public void MouseEnter()
    {
        // 카드 선택 패널(cardSelectionMode)은 손패가 아닌 덱/버림더미 카드도 보여주므로 그때는
        // IsSelectable(cards 소속 여부)과 무관하게 호버를 허용한다. 그 외(평소 손패 화면)에는
        // 로직상 이미 손패를 떠났지만 연출 대기 중이라 화면에 남아있는 카드는 호버 자체가 되면 안 된다.
        if (!CardCanvas.cardSelectionMode && !IsSelectable()) return;
        AudioManager.instance?.PlayCardHover();
        ScaleHover();
        if (IsHandCard())
        {
            ApplyHoverPose();
            // 카드 선택 패널이 열려 있을 땐 꺼내지 않는다 — 패널도 하위 Canvas라 정렬 순서가 겹친다.
            if (!CardCanvas.cardSelectionMode)
                SetFront(true);
        }
    }

    public void MouseExit()
    {
        bool keepScale = selected
            || (CardCanvas.cardSelectionMode && CardCanvas.instance.IsSelectedInPanel(GetComponent<RectTransform>()));
        if (!keepScale)
            ScaleDefault();
        ClearHoverPose(); // selected여도 호버 자세는 항상 풀어준다 — 선택 중엔 스케일만 유지되는 게 맞음
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
        // 집을 수 없다. 집은 카드는 레이캐스트를 계속 막아 그 아래 깔린 카드·UI가 반응하지 않게 하고,
        // HandZone 진입은 MouseDrag에서 사각형으로 판정한다.
        if (!selected && IsSelectable())
            CardCanvas.instance.CardSelected(handNumber);
    }

    public void MouseUp(BaseEventData data)
    {
        bool clearAfterDragUse = selected && CardCanvas.instance.nowusingCard == GetComponent<RectTransform>();
        if (selected) SelectedFalse();
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

        if (CardCanvas.instance.IsScreenPointInHandZone(pointerData.position))
        {
            if (!CardCanvas.instance.UseCard(handNumber))
            {
                SelectedFalse();
                CardCanvas.instance.CardUnSelected();
                CardDragArrow.instance?.Hide();
            }
        }
    }

}
/// <summary>카드 선택 패널에서 선택할 존</summary>
public enum CardZone { Hand, Deck, Discard, Any, SavedDeck }

/// <summary>코스트 변경 효과의 지속 시간</summary>
public enum CostDuration { Permanent, ThisTurnOnly, OneUse }

public enum EffectType { Move, Damage, Shield, Heal, SelfDamage, Draw, ApplyStatus, ApplyTurnEffect, ColDamageUp, BaseColDamageUp, ShieldBonusUp, BaseShieldBonusUp, DiscardHand, ShuffleHandToDeck, ExileHand, HandToDeckTop, SelectAndDiscard, SelectAndChangeCost, SelectAndReturnToDeck, AddCard, RestoreEnergy, Cleanse, Charge, Stun, Summon, ReduceCost, GrantChainMoveAttack, GrantSummonColDamage, GrantSummonMaxHp, AddGrave }
public record CardEffect
{
    public Board.BoardMode requiredMode { get; init; }
    public EffectType type { get; init; }
    public int dmg { get; init; }
    public RangeInfoSO effectRange { get; init; }
    public TargetLogic targetlogic { get; init; }
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
    public bool healOnHit { get; init; }                  // true면 적중 시 입힌 피해만큼 시전자 회복 (일반 공격/이동공격 모두 적용). 입힌 피해 = 대상별 실제 적용 피해(취약 등 보정 포함, Board.ApplyAttackDamage의 dealt)
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

    // Damage 타입에서 사용: 같은 대상에게 이 효과를 몇 번 적용할지 (기본 1 = 기존과 동일).
    // Move 타입이면 이동공격 충돌의 타격 수로, countsAsMoveAttack이면 이동공격 판정 타격 수로 쓰인다(Board.ResolveMoveAttackHit).
    public int hitCount { get; init; } = 1;

    // Damage 타입에서 사용: true면 이동 없이 그 자리에서 때리지만 이동공격으로 판정한다(Board.MoveAttackInPlace) —
    // 다음 이동공격 버프 소모, 기물의 이동공격 스플래시(MoveAttackRangeInfoSO, 대상 바로 앞 가상 도착 칸 기준), 연쇄 이동공격,
    // 가시 반격, 카드의 shieldOnMoveAttack이 실제 이동공격과 똑같이 적용되고 hitCount도 그대로 반영된다. 처치해도 전진하지 않는다.
    // 단일 대상 전용 — 범위형 targetlogic(AllEnemiesInRange 등)은 ExecuteAreaEffect가 먼저 처리하므로 적용되지 않는다.
    public bool countsAsMoveAttack { get; init; }

    // ApplyStatus + StatusEffectType.NextMoveAttackStatus에서 사용: 다음 이동공격에 맞은 대상에게 걸 상태이상
    // (이 효과의 statusEffectType/statusDuration/statusPower를 쓴다).
    public CardEffect onMoveAttackHitEffect { get; init; }

    // Damage 타입에서 사용: 0보다 크면 hitCount를 "이번 카드에서 앞 효과로 실제로 버려진 카드 수 × hitsPerDiscarded"로
    // 바꿔 실행한다(Board.ExecuteEffect). 버려진 카드가 없어 0이 되면 이 효과는 타격 없이 스킵된다.
    public int hitsPerDiscarded { get; init; }

    // 무덤 소모: graveCost는 이 효과 실행에 필요한 최소 무덤 수 — 첫 효과면 부족 시 카드 사용 불가,
    // 2번째 이후면 이 효과만 스킵(Board.ProcessNextCardEffectStep). consumeAllGrave면 graveCost 이상일 때
    // 가진 무덤을 전부 소모한다. dmgPerGrave는 소모한 무덤 1개당 dmg에 더할 값(0이면 비례 효과 없음).
    // 무덤은 카드를 낸 기물(시전자)의 것을 쓴다.
    public int graveCost { get; init; }
    public bool consumeAllGrave { get; init; }
    public int dmgPerGrave { get; init; }

    // true면 타겟을 다시 고르지 않고 직전 효과의 targetPos에 바로 적용한다(같은 대상 추가 타격 등).
    public bool useLastTarget { get; init; }

    // true면 실행 시점에 대상 칸에 살아 있는 기물이 없으면(앞 효과로 죽음, 빈 칸 지정 등) 이 효과를 실행하지 않는다 —
    // 무덤 비용도 차감되지 않는다. useLastTarget과 함께 쓰면 직전 효과의 대상과 같은 기물이어야 한다(칸에 다른 기물이
    // 들어와 있어도 스킵). 단일 대상 효과용 — 범위형(All*InRange)에는 적용하지 않는다.
    public bool skipIfTargetGone { get; init; }

    // 적 AI 전용 "아군 위치 고정 공격": effectRange를 시전자가 아니라 각 아군 위치를 중심으로 펼친다.
    // 칸은 플레이어 턴 시작 시점에 잠기고(Board.LockEnemyTelegraphs → AutoPiece.lockedTargetCells) 실행 시엔
    // 그 잠긴 칸을 그대로 친다 — 예고를 보고 그 칸에서 벗어나면 피할 수 있다. 범위형 targetlogic과 함께 쓴다.
    public bool lockOnAllyPositions { get; init; }

    // 이 효과의 시전자 — Board는 효과마다 이 기물의 현재 위치를 시전자 칸으로 쓴다(이동했으면 새 위치).
    // null이면 selectedButton 기준(기존 방식).
    // 효과를 실행하도록 등록하는 시점(카드 사용·소환 시 효과·턴 효과·유물·처치 시 효과 등)에 원본은 그대로 두고
    // `with { caster = ... }` 사본으로 기록한다 — TurnEffect가 카드의 중첩 CardEffect 객체를 공유하므로 원본을 바꾸면 안 된다.
    public Piece caster { get; init; }
}
