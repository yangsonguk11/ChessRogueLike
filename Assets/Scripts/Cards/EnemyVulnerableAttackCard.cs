using UnityEngine;

public class EnemyVulnerableAttackCard : Card
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
            type = EffectType.Damage,
            dmg = 4,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
            ignoreCasterColDamageBonus = true,
        });
        effects.Add(new CardEffect
        {
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            statusEffectType = StatusEffectType.Vulnerable,
            statusDuration = -1,
            statusPower = 1,
            useLastTarget = true,    // 타격한 대상에게 그대로 — 다시 클릭하지 않음(타격으로 대상이 죽었으면 취약은 무시된다)
            animTrigger = "ApplyStatus",
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;
    public override string EffectDescription =>
        $"적에게 {EffectiveDmg(effects[0])} 피해를 주고 약화({effects[1].statusPower})를 부여합니다.";
}
