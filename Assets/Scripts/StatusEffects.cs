using System.Collections.Generic;
using UnityEngine;

// 독: 매 자기 턴 종료 시 고정 피해
public class PoisonEffect : StatusEffect
{
    public readonly int damagePerTurn;
    public override string DisplayName => $"독 ({damagePerTurn})";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(0.4f, 0.9f, 0.2f); // 독성 초록

    public PoisonEffect(int duration, int damagePerTurn)
    {
        this.duration = duration;
        this.damagePerTurn = damagePerTurn;
    }

    // 실제 피해 적용은 Piece.TickStatusEffects가 같은 종류끼리 damagePerTurn을 합산한 값으로 Board가 효과 큐에서 한 번만 처리한다.
    // 여기서는 지속시간만 감소시키면 되므로 base.OnTurnEnd 그대로 사용(override 불필요).
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 화상: 독보다 높은 피해의 DoT
public class BurningEffect : StatusEffect
{
    public readonly int damagePerTurn;
    public override string DisplayName => $"화상 ({damagePerTurn})";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(1f, 0.5f, 0f); // 화상 주황

    public BurningEffect(int duration, int damagePerTurn)
    {
        this.duration = duration;
        this.damagePerTurn = damagePerTurn;
    }

    // 실제 피해 적용은 Piece.TickStatusEffects가 같은 종류끼리 damagePerTurn을 합산한 값으로 Board가 효과 큐에서 한 번만 처리한다.
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 재생: 매 자기 턴 종료 시 회복
public class RegenEffect : StatusEffect
{
    public readonly int healPerTurn;
    public override string DisplayName => $"재생 ({healPerTurn})";
    public override bool IsBuff => true;

    public RegenEffect(int duration, int healPerTurn)
    {
        this.duration = duration;
        this.healPerTurn = healPerTurn;
    }

    // 실제 회복 적용은 Piece.TickStatusEffects가 같은 종류끼리 healPerTurn을 합산한 값으로 Board가 효과 큐에서 한 번만 처리한다.
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 기절: 행동 불가. 아군은 카드 사용 자체가 막히고, 적은 다음 행동이 StunnedCard로 바뀐다(Enemy.GetNextMove).
public class StunEffect : StatusEffect
{
    public override string DisplayName => "기절";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(1f, 0.85f, 0.2f); // 기절 노랑

    public StunEffect(int duration)
    {
        this.duration = duration;
    }

    // 걸리는 즉시 다음 행동 예고를 다시 계산해서 보여준다(Piece.ActionText는 기본적으로 아무 일도 안 하고,
    // Enemy만 오버라이드해서 GetNextMove()로 다음 카드를 다시 뽑아 텍스트를 갱신함 — 이러면 원래 예고돼 있던
    // 카드 대신 스턴이 걸렸다는 게 바로 반영된다). 아군에게는 안전한 무해 호출이다.
    // ShowAllEnemyRanges()도 같이 다시 돌려서, 보드에 이미 하이라이트된 칸(기절 전 카드 기준 범위)도
    // 새로 GetNextMove()가 반환하는 StunnedCard(빈 범위) 기준으로 즉시 갱신되게 한다.
    public override void OnApply(Piece piece)
    {
        piece.ActionText();
        piece.SetAnimBool("Stun", true);
        Board.instance?.ShowAllEnemyRanges();
    }
    public override void OnRemove(Piece piece)
    {
        piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
        piece.SetAnimBool("Stun", false);
        piece.ActionText(); // 이 시점엔 이미 activeEffects에서 제거된 뒤라 IsStunned()가 정확히 false를 반환한다.
    }
}

// 강화: colDamage 증가, 해제 시 원복
public class StrengthenEffect : StatusEffect
{
    public readonly int bonusDamage;
    public override string DisplayName => $"강화 (+{bonusDamage})";
    public override bool IsBuff => true;

    public StrengthenEffect(int duration, int bonusDamage)
    {
        this.duration = duration;
        this.bonusDamage = bonusDamage;
    }

    public override void OnApply(Piece piece) => piece.AddColDamage(bonusDamage);
    public override void OnRemove(Piece piece)
    {
        piece.colDamage -= bonusDamage;
        piece.ShowStatusText(DisplayName + " 해제", false, EffectColor);
    }
}

// 가시: 이동공격을 받으면 공격자에게 고정 피해 반격
public class ThornEffect : StatusEffect
{
    public readonly int returnDamage;
    public override string DisplayName => $"가시 ({returnDamage})";
    public override bool IsBuff => true;

    public ThornEffect(int duration, int returnDamage)
    {
        this.duration = duration;
        this.returnDamage = returnDamage;
    }

    public override int OnReceiveMoveAttack(Piece self, Piece attacker)
    {
        return returnDamage;
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 취약: attack/moveattack으로 받는 피해 +N. 독/화상/반격/자해 등 다른 경로의 피해는 증가하지 않는다
// (보정은 Board.ApplyAttackDamage를 거치는 공격 피해에만 적용됨).
public class VulnerableEffect : StatusEffect
{
    public readonly int bonusDamage;
    public override string DisplayName => $"취약 (+{bonusDamage})";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(1f, 0.45f, 0.65f); // 취약 분홍

    public VulnerableEffect(int duration, int bonusDamage)
    {
        this.duration = duration;
        this.bonusDamage = bonusDamage;
    }

    public override int IncomingAttackDamageBonus => bonusDamage;
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 도발: 상대 진영이 공격/이동 대상을 고를 때, 사거리 안에 도발 기물이 있으면 그 기물을 우선한다
// (Board.PrioritizeTauntTargets). 지금은 적/자동행동 아군 AI에만 적용되고, 플레이어 카드 대상 제한은 예정.
public class TauntEffect : StatusEffect
{
    public override string DisplayName => "도발";
    public override bool IsBuff => true;

    public TauntEffect(int duration)
    {
        this.duration = duration;
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 이동 불가: 현재 게임플레이 미적용, 상태 표시만
public class MovementDisabledEffect : StatusEffect
{
    public override string DisplayName => "이동 불가";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(0.6f, 0.6f, 0.6f); // 이동 불가 회색

    public MovementDisabledEffect(int duration)
    {
        this.duration = duration;
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 소멸 저주: 지속되는 동안 이 기물이 사용하는 카드는 버림 더미 대신 소멸 더미로 간다. 판정은 카드 로직이 시작되는 순간
// (CardCanvas.DeductEnergyForCard) 저주가 있는지로 하고, 소멸 처리는 CardCanvas.FinishUseCard가 한다.
// 카드 인스턴스의 exileOnUse는 건드리지 않으므로 저주가 풀리면 카드는 원래대로다. 걸려 있는 동안 카드 면의 "(소멸)"은 Card.ShowsExile.
public class ExileOnUseEffect : StatusEffect
{
    public override string DisplayName => "소멸 저주";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(0.65f, 0.4f, 0.9f); // 소멸 저주 보라

    public ExileOnUseEffect(int duration)
    {
        this.duration = duration;
    }

    public override void OnApply(Piece piece) => CardCanvas.instance?.RefreshAllCardViews();
    public override void OnRemove(Piece piece)
    {
        piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
        CardCanvas.instance?.RefreshAllCardViews();
    }
}

// 결속: BondPiece가 스스로 거는 영구 버프. 실제 강화(동료 사망 시 완전 회복 + 힘 증가)는 BondPiece.OnTeammateDied가 하고,
// 이 효과는 정보창 표시와 디스펠 대상 역할만 한다 — 지워지면 강화가 멈춘다. 카드로 거는 일이 없어 StatusEffectType은 없다.
public class BondEffect : StatusEffect
{
    public override string DisplayName => "결속";
    public override bool IsBuff => true;
    public override Color EffectColor => new Color(1f, 0.55f, 0.2f); // 결속 주황

    public BondEffect()
    {
        duration = -1;
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 상처 입히기: Boss-Berserker가 스폰 시 스스로 거는 영구 버프(BossWoundOnHitCard). 실제 처리는 Board.ApplyOnHitPassives —
// 이 기물의 공격·이동공격 피해로 상대의 체력이 줄면(방어도로 다 막으면 제외) 그 기물의 버린 카드 더미에 cardID 카드를 count장 넣는다.
public class WoundOnHitEffect : StatusEffect
{
    public readonly string cardID = "WoundCard";
    public readonly int count = 2;
    public override string DisplayName => "상처 입히기";
    public override bool IsBuff => true;
    public override Color EffectColor => new Color(0.8f, 0.2f, 0.25f); // 상처 적색

    public WoundOnHitEffect(int duration)
    {
        this.duration = duration;
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 약화: colDamage 감소, 해제 시 원복
public class WeakenEffect : StatusEffect
{
    public readonly int reducedDamage;
    public override string DisplayName => $"약화 (-{reducedDamage})";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(0.65f, 0.35f, 0.85f); // 약화 보라

    public WeakenEffect(int duration, int reducedDamage)
    {
        this.duration = duration;
        this.reducedDamage = reducedDamage;
    }

    int actualReduction;
    public override void OnApply(Piece piece)
    {
        actualReduction = Mathf.Min(reducedDamage, piece.colDamage);
        piece.AddColDamage(-actualReduction);
    }
    public override void OnRemove(Piece piece)
    {
        piece.colDamage += actualReduction;
        piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
    }
}

// 이동공격 1회에 적용할 "다음 이동공격" 버프의 합계 — Piece.Peek/ConsumeNextMoveAttackBonus가 만든다.
// 피해는 (기본 + flatDamage) × (1 + extraMultiplier)이고, 대상별 취약 보정은 그 뒤 Board.ApplyAttackDamage에서 붙는다.
public class MoveAttackBonus
{
    public int flatDamage;      // 가산 피해 합
    public int extraMultiplier; // 배율 가산분 합(×2 버프 하나당 +1 — ×2 두 개면 ×3)
    public readonly List<CardEffect> onHitEffects = new List<CardEffect>(); // 적중 대상에게 걸 상태이상(statusEffectType 등)
    public readonly List<string> consumedNames = new List<string>();      // 소모 텍스트용 버프 이름(버프마다 텍스트 1개)

    public bool HasAny => consumedNames.Count > 0;
    public int Apply(int baseDmg) => Mathf.Max(0, (baseDmg + flatDamage) * (1 + extraMultiplier));
}

// 다음 이동공격(이동공격 판정 공격 포함) 1회를 강화하는 버프 공통. 같은 종류라도 합치지 않고 걸린 만큼 따로 들고 있다가,
// 이동공격이 실제로 일어나는 순간 Board가 전부 합산해 한꺼번에 소모한다(Piece.ConsumeNextMoveAttackBonus).
// duration이 음수면 이동공격할 때까지 유지, 양수면 다른 상태이상처럼 턴 종료마다 줄어 만료된다.
// 부여 텍스트/파티클은 ApplyStatusToTarget의 StatusTextReaction이 재생하므로 OnApply에서는 띄우지 않는다.
public abstract class NextMoveAttackEffect : StatusEffect
{
    public override bool IsBuff => true;

    public abstract void AddTo(MoveAttackBonus bonus);

    public override void OnApply(Piece piece) => CardCanvas.instance?.RefreshAllCardViews();
    // 턴 만료·디스펠로 사라질 때만 호출된다 — 이동공격으로 소모될 때는 Piece가 OnRemove 없이 빼고 "소모" 텍스트를 따로 띄운다.
    public override void OnRemove(Piece piece)
    {
        piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
        CardCanvas.instance?.RefreshAllCardViews();
    }
}

// 다음 이동공격 피해 +N
public class NextMoveAttackDamageEffect : NextMoveAttackEffect
{
    public readonly int bonusDamage;
    public override string DisplayName => $"다음 이동공격 +{bonusDamage}";

    public NextMoveAttackDamageEffect(int duration, int bonusDamage)
    {
        this.duration = duration;
        this.bonusDamage = bonusDamage;
    }

    public override void AddTo(MoveAttackBonus bonus) => bonus.flatDamage += bonusDamage;
}

// 다음 이동공격 피해 ×M — 여러 개면 배율 가산분(M - 1)끼리 더한다.
public class NextMoveAttackMultiplierEffect : NextMoveAttackEffect
{
    public readonly int multiplier;
    public override string DisplayName => $"다음 이동공격 ×{multiplier}";

    public NextMoveAttackMultiplierEffect(int duration, int multiplier)
    {
        this.duration = duration;
        this.multiplier = multiplier;
    }

    public override void AddTo(MoveAttackBonus bonus) => bonus.extraMultiplier += Mathf.Max(0, multiplier - 1);
}

// 다음 이동공격에 맞은 대상(주 대상 + 스플래시)마다 inflict의 상태이상을 1회 건다.
public class NextMoveAttackStatusEffect : NextMoveAttackEffect
{
    public readonly CardEffect inflict;
    public override string DisplayName => $"다음 이동공격 적중 시 {InflictLabel(inflict)}";

    public NextMoveAttackStatusEffect(int duration, CardEffect inflict)
    {
        this.duration = duration;
        this.inflict = inflict;
    }

    public override void AddTo(MoveAttackBonus bonus)
    {
        if (inflict != null && inflict.statusEffectType != StatusEffectType.None)
            bonus.onHitEffects.Add(inflict);
    }

    // 적중 대상에게 걸 상태를 "N턴 이름"으로 — N은 걸리는 상태의 지속(이 버프의 남은 턴은 StatusLabel이 뒤에 붙인다).
    // 이름은 실제로 걸릴 상태의 DisplayName이라 대상에게 보이는 이름과 같다.
    static string InflictLabel(CardEffect e)
    {
        if (e == null) return "효과 없음";
        string name = Board.CreateStatusEffect(e.statusEffectType, e.statusDuration, e.statusPower)?.DisplayName
            ?? e.statusEffectType.ToString();
        return e.statusDuration >= 0 ? $"{e.statusDuration}턴 {name}" : name;
    }
}
