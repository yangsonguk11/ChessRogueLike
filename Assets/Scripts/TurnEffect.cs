public enum TurnPhase { OwnTurnStart, OwnTurnEnd }

public class TurnEffect : StatusEffect
{
    public readonly TurnPhase phase;
    public readonly CardEffect cardEffect;

    public override string DisplayName
    {
        get
        {
            string timing = phase == TurnPhase.OwnTurnStart ? "턴 시작 시" : "턴 종료 시";
            string effectDesc = cardEffect.type switch
            {
                EffectType.Damage => cardEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
                                      cardEffect.targetlogic == TargetLogic.AllAlliesInRange
                    ? $"광역 피해 {cardEffect.dmg}"
                    : $"피해 {cardEffect.dmg}",
                EffectType.Heal        => $"회복 {cardEffect.dmg}",
                EffectType.Shield      => $"방어막 {cardEffect.dmg}",
                EffectType.ColDamageUp => cardEffect.dmg >= 0 ? $"이동공격력 +{cardEffect.dmg}" : $"이동공격력 {cardEffect.dmg}",
                _                      => "효과"
            };
            return $"{timing} {effectDesc}";
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
