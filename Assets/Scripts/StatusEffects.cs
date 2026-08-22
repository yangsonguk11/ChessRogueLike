using UnityEngine;

// 독: 매 자기 턴 종료 시 고정 피해
public class PoisonEffect : StatusEffect
{
    public readonly int damagePerTurn;
    public override string DisplayName => $"독 ({damagePerTurn}/턴)";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(0.4f, 0.9f, 0.2f); // 독성 초록

    public PoisonEffect(int duration, int damagePerTurn)
    {
        this.duration = duration;
        this.damagePerTurn = damagePerTurn;
    }

    public override bool OnTurnEnd(Piece piece)
    {
        piece.GetDamage(damagePerTurn);
        return base.OnTurnEnd(piece);
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 화상: 독보다 높은 피해의 DoT
public class BurningEffect : StatusEffect
{
    public readonly int damagePerTurn;
    public override string DisplayName => $"화상 ({damagePerTurn}/턴)";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(1f, 0.5f, 0f); // 화상 주황

    public BurningEffect(int duration, int damagePerTurn)
    {
        this.duration = duration;
        this.damagePerTurn = damagePerTurn;
    }

    public override bool OnTurnEnd(Piece piece)
    {
        piece.GetDamage(damagePerTurn);
        return base.OnTurnEnd(piece);
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 재생: 매 자기 턴 종료 시 회복
public class RegenEffect : StatusEffect
{
    public readonly int healPerTurn;
    public override string DisplayName => $"재생 ({healPerTurn}/턴)";
    public override bool IsBuff => true;

    public RegenEffect(int duration, int healPerTurn)
    {
        this.duration = duration;
        this.healPerTurn = healPerTurn;
    }

    public override bool OnTurnEnd(Piece piece)
    {
        piece.GetHeal(healPerTurn);
        return base.OnTurnEnd(piece);
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
}

// 기절: 행동 불가 (적은 카드 사용 스킵)
public class StunEffect : StatusEffect
{
    public override string DisplayName => "기절";
    public override bool IsBuff => false;
    public override Color EffectColor => new Color(1f, 0.85f, 0.2f); // 기절 노랑

    public StunEffect(int duration)
    {
        this.duration = duration;
    }
    public override void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);
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
