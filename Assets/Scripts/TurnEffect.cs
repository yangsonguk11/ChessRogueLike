// 카드가 정의한 턴 효과(StatusEffectType.TurnEffect) — 걸린 기물의 자기 턴 phase마다 cardEffect를 그 기물을 시전자로 실행하고,
// 지속시간도 같은 phase에 줄어든다. 처리는 다른 상태와 같은 Board.EnqueueTeamStatusEffects가 한다.
// cardEffect가 올린 스탯(힘·무덤 등)은 누적형이라 만료돼도 되돌리지 않는다.
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
                EffectType.ColDamageUp => cardEffect.dmg >= 0 ? $"힘 +{cardEffect.dmg}" : $"힘 {cardEffect.dmg}",
                EffectType.ShieldBonusUp => cardEffect.dmg >= 0 ? $"방어막 보너스 +{cardEffect.dmg}" : $"방어막 보너스 {cardEffect.dmg}",
                EffectType.GrantSummonColDamage => $"다음 소환 힘 +{cardEffect.dmg}",
                EffectType.GrantSummonMaxHp     => $"다음 소환 체력 +{cardEffect.dmg}",
                EffectType.AddGrave    => $"무덤 +{cardEffect.dmg}",
                _                      => "효과"
            };
            return $"{timing} {effectDesc}";
        }
    }
    // cardEffect(=CardEffect.onTurnEffect)가 직접 명시한 값을 그대로 사용한다 — type만으로는
    // 버프/디버프를 구분할 수 없다(예: Damage는 자기 자신 대상이면 디버프, 적 대상이면 버프).
    public override bool IsBuff => cardEffect.isBuff;

    public override TurnPhase TickPhase => phase;
    public override CardEffect TurnEffectAt(TurnPhase p) => p == phase ? cardEffect : null;

    public TurnEffect(TurnPhase phase, CardEffect cardEffect, int duration)
    {
        this.phase      = phase;
        this.cardEffect = cardEffect;
        this.duration   = duration;
    }
}
