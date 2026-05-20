public enum TurnPhase { OwnTurnStart, OwnTurnEnd }

public class TurnEffect : StatusEffect
{
    public readonly TurnPhase phase;
    public readonly CardEffect cardEffect;

    public override string DisplayName
    {
        get
        {
            string timing = phase == TurnPhase.OwnTurnStart ? "턴 시작" : "턴 종료";
            return $"턴 효과 ({timing}, {duration}턴 남음)";
        }
    }
    public override bool IsBuff => cardEffect.type == EffectType.Heal || cardEffect.type == EffectType.Shield;

    public TurnEffect(TurnPhase phase, CardEffect cardEffect, int duration)
    {
        this.phase      = phase;
        this.cardEffect = cardEffect;
        this.duration   = duration;
    }
}
