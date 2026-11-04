using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 조건부 행동의 조건. 판정은 다음 행동을 정하는 시점(적이 행동한 직후, 전투 시작·스폰 직후)의 보드로 한다.
public enum ActionCondition
{
    TargetInRange,    // 판정 카드(checkCard, 비면 card)를 지금 쓰면 맞는 대상이 있음(Board.HasTargetIfUsedNow)
    NoTargetInRange,  // 위의 반대 — 예: 공격 차례인데 범위 안에 상대가 없으면 다가간다
    HpAtMostPercent,  // 자기 체력이 최대 체력의 param% 이하
    TeammatesAtMost,  // 살아 있는 같은 팀 동료(자신 제외)가 param명 이하
}

// 조건 → 카드 한 줄. AutoPiece.conditionalActions에 위에서부터 평가해 처음 맞는 줄의 카드를 다음 행동으로 쓴다.
[System.Serializable]
public class ConditionalAction
{
    public ActionCondition condition;
    public int param;          // HpAtMostPercent: %, TeammatesAtMost: 남은 동료 수
    public Card checkCard;     // TargetInRange/NoTargetInRange가 판정할 카드 — 비면 card 자신
    public Card card;          // 조건이 맞으면 쓸 카드(이 기물에 붙은 Card 컴포넌트)
    public bool advanceCycle;  // true면 기본 순환(actionCards) 한 칸을 소모, false면 끼어들기 — 다음엔 순환 위치 그대로 이어감
}

public class AutoPiece : Piece
{
    [FormerlySerializedAs("enemyCards")]
    public List<Card> actionCards; // 순서대로 사용할 스킬/효과 리스트 (Enemy/AutoAlly 공용)

    // 상황에 따라 순환 대신 쓸 행동. 비어 있으면 actionCards 순환만 한다(PlanNextMove 참고).
    public List<ConditionalAction> conditionalActions = new List<ConditionalAction>();

    int Movenum;

    // CardEffect.lockOnAllyPositions 효과가 칠 보드 절대 좌표 — 플레이어 턴 시작 시 Board.LockEnemyTelegraphs가
    // 채우고, 행동을 마친 뒤(FinishMove) 비운다. 전투 런타임 전용 — 직렬화되면 Unity가 빈 리스트로 채워
    // null(잠금 없음)과 구분이 안 되므로 NonSerialized로 둔다.
    [System.NonSerialized] public List<Vector2Int> lockedTargetCells;

    Card nextMove;
    ConditionalAction plannedEntry; // 이번에 예고한 행동이 조건부 행동이면 그 줄, 순환 카드면 null
    StunnedCard stunnedCard;
    public override void Awake()
    {
        base.Awake();
        Movenum = 0;
        nextMove = actionCards[0]; // 보드 준비 전 임시값 — 전투 시작·스폰 직후 Board가 PlanNextMove로 다시 정한다
        stunnedCard = gameObject.AddComponent<StunnedCard>();
        // 행동 카드의 수치 미리보기(보드 칸 정보창의 다음 행동 카드 이미지)는 이 기물의 힘·방어막 보너스로 보여준다.
        foreach (Card card in actionCards)
            if (card != null) card.previewCasterOverride = this;
        foreach (ConditionalAction entry in conditionalActions)
            if (entry?.card != null) entry.card.previewCasterOverride = this;
        stunnedCard.previewCasterOverride = this;
    }

    void Start()
    {
        ActionText();
    }

    // 표시용(정보창·예고) 범위 = 다음 행동의 첫 효과 범위. 실제 대상 선택은 처리 중인 효과의 범위를 직접 쓴다(Board.ResolveTargetPos).
    public override List<Vector2Int> GetMoveableButton() {
        Card move = GetNextMove();
        if (move == null || move.effects.Count == 0) return base.GetMoveableButton();
        CardEffect first = move.effects[0];
        // 아군 위치 고정 공격의 effectRange는 시전자 기준 오프셋이 아니다 — 예고는 ShowAllEnemyRanges가 잠긴 칸으로 따로 그린다.
        if (first.lockOnAllyPositions)
            return new List<Vector2Int>();
        // 범위가 없는 자기 대상·보드 전체 효과는 보여줄 범위가 없다.
        if (first.effectRange == null && (first.targetlogic == TargetLogic.self || first.noRangeLimit))
            return new List<Vector2Int>();
        // 그 외는 효과 범위(effectRange, 비어 있으면 이동 범위 — 이동 효과도 같다).
        return first.GetRangeOffsets(this);
    }

    // 예고 중인 다음 행동(조회 전용 — 정보창·예고가 여러 번 부르므로 결정은 PlanNextMove에서만 한다).
    // 기절 상태면 예고했던 카드 대신 스턴 카드를 반환한다.
    public virtual Card GetNextMove()
    {
        if (IsStunned()) return stunnedCard;
        if (actionCards == null || actionCards.Count == 0) return null;
        return nextMove;
    }

    // 다음 행동을 정해 예고한다. 적이 행동한 직후(FinishMove), 전투 시작 배치 직후·스폰 직후(Board)에 부른다.
    // 정한 행동은 다음 자기 턴까지 바꾸지 않는다 — 예고를 보고 대응하는 플레이를 위해 플레이어 턴 중의 변화는 반영하지 않는다.
    public void PlanNextMove()
    {
        if (actionCards == null || actionCards.Count == 0) return;
        nextMove = ChooseNextMove(out plannedEntry);
        ActionText();
    }

    // 조건부 행동을 위에서부터 평가해 처음 맞는 줄의 카드, 없으면 순환 카드. 특수 AI는 오버라이드한다.
    protected virtual Card ChooseNextMove(out ConditionalAction entry)
    {
        foreach (ConditionalAction c in conditionalActions)
        {
            if (c?.card != null && IsConditionMet(c))
            {
                // 고른 카드가 마침 이번 순환 카드면 순환을 쓴 것으로 친다 — 아니면 순환이 멈춰 같은 카드가 두 번 연속 나온다
                // (예: 이동 → 공격 순환에 "범위 밖이면 이동"을 넣었을 때 이동 차례).
                entry = c.card == actionCards[Movenum] ? null : c;
                return c.card;
            }
        }
        entry = null;
        return actionCards[Movenum];
    }

    bool IsConditionMet(ConditionalAction c)
    {
        switch (c.condition)
        {
            case ActionCondition.TargetInRange:   return Board.HasTargetIfUsedNow(CheckCardOf(c), this);
            case ActionCondition.NoTargetInRange: return !Board.HasTargetIfUsedNow(CheckCardOf(c), this);
            case ActionCondition.HpAtMostPercent: return hp * 100 <= maxhp * c.param;
            case ActionCondition.TeammatesAtMost: return Board.instance != null && Board.instance.GetLivingTeammates(this).Count <= c.param;
            default:                              return false;
        }
    }

    // Unity 오브젝트라 ??(가짜 null 무시) 대신 != null로 고른다.
    static Card CheckCardOf(ConditionalAction c) => c.checkCard != null ? c.checkCard : c.card;

    // 행동을 마친 뒤: 순환 카드를 썼거나 순환을 소모하는 조건부 행동이었으면 순환을 한 칸 넘기고 다음 행동을 정한다.
    // 기절해 행동하지 못한 턴(acted = false)은 순환 위치를 유지해 기절이 풀리면 원래 순서가 이어지게 하되,
    // 조건은 지금 보드로 다시 판정한다.
    public void FinishMove(bool acted)
    {
        lockedTargetCells = null; // 잠금은 한 번의 행동에만 유효 — 지난 예고 칸이 다음 ShowAllEnemyRanges에 다시 뜨지 않게
        if (acted && (plannedEntry == null || plannedEntry.advanceCycle))
        {
            Movenum++;
            if (Movenum >= actionCards.Count)
                Movenum = 0;
        }
        PlanNextMove();
    }

    // 행동 패턴 자체를 다른 카드 목록으로 교체하고 첫 카드부터 다시 순환한다(예: GuardianPiece의 격노 전환).
    // 다음 행동도 바로 다시 정한다 — 범위 예고는 호출 직후 기물 사망 연출(DeathCor)의 ShowAllEnemyRanges가 다시 그린다.
    protected void SwitchActionCards(List<Card> cards)
    {
        if (cards == null || cards.Count == 0) return;
        actionCards = cards;
        Movenum = 0;
        lockedTargetCells = null;
        PlanNextMove();
    }
    public override void ActionText()
    {
        Card card = GetNextMove();
        if (card == null) return;

        var parts = new List<string>();
        foreach (CardEffect effect in card.effects)
        {
            string desc = BuildEffectDescription(effect);
            if (!string.IsNullOrEmpty(desc))
                parts.Add(desc);
        }

        pieceCanvas.ShowActionText(parts.Count > 0 ? string.Join(" ", parts) : card.Name);
    }

    string BuildEffectDescription(CardEffect effect)
    {
        string primary = effect.type switch
        {
            EffectType.Move        => "<sprite name=\"Move\">",
            EffectType.Damage      => effect.useColDamageAsDmg ? "<sprite name=\"Damage\">" : $"{effect.dmg + ColDamageDelta}<space=30><sprite name=\"Damage\">",
            EffectType.Shield      => $"{effect.dmg + ShieldBonusDelta}<space=30><sprite name=\"Shield\">",
            EffectType.Heal        => $"{effect.dmg}<space=30><sprite name=\"Heal\">",
            EffectType.SelfDamage  => $"{effect.dmg}<space=30><sprite name=\"Damage\">",
            EffectType.Draw        => effect.dmg > 1 ? $"{effect.dmg}" : "",
            EffectType.ApplyStatus => BuildStatusDescription(effect),
            EffectType.ColDamageUp => $"<sprite name=\"Damage\"><space=10>+{effect.dmg}",
            EffectType.ShieldBonusUp => $"<sprite name=\"Shield\"><space=10>+{effect.dmg}",
            EffectType.Stun        => "<sprite name=\"Stun\">",
            EffectType.AddCard     => "<sprite name=\"Negative\">", // 상대 덱에 카드를 넣는 방해 행동
            // Damage/Shield/Move/Heal/Positive/Negative 6개 아이콘 중 어디에도 안 맞는 효과(Charge 포함)는
            // 전용 아이콘 없이 자연스럽게 Unknown으로 표시한다.
            _                      => "<sprite name=\"Unknown\">"
        };

        // 주 효과에 상태이상이 함께 붙어있는 경우 표시
        if (effect.type != EffectType.ApplyStatus && effect.statusEffectType != StatusEffectType.None)
        {
            string statusDesc = BuildStatusDescription(effect);
            if (!string.IsNullOrEmpty(statusDesc))
                primary += $"+{statusDesc}";
        }

        return primary;
    }

    // 상태이상 종류(독/화상/강화 등)를 따로 표기하지 않고, 이로운 효과인지 해로운 효과인지만
    // Positive/Negative 아이콘으로 통일해서 보여준다. 아이콘 자체는 actIcon에 나중에 추가될 예정.
    // 턴 효과는 종류만으로 알 수 없어 감싼 효과의 isBuff(TurnEffect.IsBuff와 같은 값)로 판정한다.
    static bool IsPositiveStatus(CardEffect effect) => effect.statusEffectType switch
    {
        StatusEffectType.Regen or
        StatusEffectType.Strengthen or
        StatusEffectType.Thorn or
        StatusEffectType.Taunt      => true,
        StatusEffectType.TurnEffect => effect.onTurnEffect?.isBuff ?? false,
        _                           => false,
    };

    string BuildStatusDescription(CardEffect effect)
    {
        if (effect.statusEffectType == StatusEffectType.None) return "";
        if (effect.statusEffectType == StatusEffectType.Stun) return "<sprite name=\"Stun\">";
        return $"<sprite name=\"{(IsPositiveStatus(effect) ? "Positive" : "Negative")}\">";
    }
}
