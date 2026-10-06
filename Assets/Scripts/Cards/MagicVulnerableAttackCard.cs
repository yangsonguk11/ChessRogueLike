// 사거리 안 적 하나에게 고정 피해를 여러 번 준 뒤, 같은 대상에게 취약을 건다.
// 취약은 타격이 모두 끝난 다음에 걸리므로 이 카드의 타격에는 붙지 않는다(이후 공격부터 적용).
public class MagicVulnerableAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MagicVulnerableAttackCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Damage,
            dmg = 2,
            hitCount = 3,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
            ignoreCasterColDamageBonus = true,
        });
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            statusEffectType = StatusEffectType.Vulnerable,
            statusDuration = 2,
            statusPower = 2,
            useLastTarget = true,    // 타격한 대상에게 그대로 — 다시 클릭하지 않음
            skipIfTargetGone = true, // 타격으로 대상이 죽었으면 취약은 건너뜀
            animTrigger = "ApplyStatus",
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;
    public override string EffectDescription =>
        $"{EffectiveDmg(effects[0])}의 고정 피해를 {effects[0].hitCount}번 줍니다. {effects[1].statusDuration}턴간 취약({effects[1].statusPower})을 부여합니다.";
}
