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
            bool isArea = cardEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
                          cardEffect.targetlogic == TargetLogic.AllAlliesInRange ||
                          cardEffect.targetlogic == TargetLogic.AllPiecesInRange;
            string effectDesc = cardEffect.type switch
            {
                EffectType.Damage      => isArea ? $"광역 피해 {cardEffect.dmg}" : $"피해 {cardEffect.dmg}",
                EffectType.Heal        => isArea ? $"광역 회복 {cardEffect.dmg}" : $"회복 {cardEffect.dmg}",
                EffectType.Shield      => $"방어막 {cardEffect.dmg}",
                EffectType.ColDamageUp => cardEffect.dmg >= 0 ? $"이동공격력 +{cardEffect.dmg}" : $"이동공격력 {cardEffect.dmg}",
                EffectType.ShieldBonusUp => cardEffect.dmg >= 0 ? $"방어막 보너스 +{cardEffect.dmg}" : $"방어막 보너스 {cardEffect.dmg}",
                EffectType.GrantSummonColDamage => $"다음 소환 콜대미지 +{cardEffect.dmg}",
                EffectType.GrantSummonMaxHp     => $"다음 소환 체력 +{cardEffect.dmg}",
                EffectType.AddGrave    => $"무덤 +{cardEffect.dmg}",
                _                      => "효과"
            };
            return $"{timing} {effectDesc}";
        }
    }
    // cardEffect(=onTurnEndEffect 등)가 직접 명시한 값을 그대로 사용한다 — type만으로는
    // 버프/디버프를 구분할 수 없다(예: Damage는 자기 자신 대상이면 디버프, 적 대상이면 버프).
    public override bool IsBuff => cardEffect.isBuff;

    public TurnEffect(TurnPhase phase, CardEffect cardEffect, int duration)
    {
        this.phase      = phase;
        this.cardEffect = cardEffect;
        this.duration   = duration;
    }
}
