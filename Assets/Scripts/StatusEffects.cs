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

    // 실제 피해 적용은 Piece.ProcessStatusEffects가 같은 종류끼리 damagePerTurn을 누적해서 한 번만 처리한다.
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

    // 실제 피해 적용은 Piece.ProcessStatusEffects가 같은 종류끼리 damagePerTurn을 누적해서 한 번만 처리한다.
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

    // 실제 회복 적용은 Piece.ProcessStatusEffects가 같은 종류끼리 healPerTurn을 누적해서 한 번만 처리한다.
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
        Board.instance?.ShowAllEnemyRanges();
    }
    public override void OnRemove(Piece piece)
    {
        piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
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
