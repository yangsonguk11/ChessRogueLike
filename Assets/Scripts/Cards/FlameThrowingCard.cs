using UnityEngine;

public class FlameThrowingCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "FlameThrowingCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        CardEffect cf = new CardEffect
        {
            type = EffectType.ApplyStatus,
            statusEffectType = StatusEffectType.TurnEffect,
            dmg = 0,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
            onTurnEffect = new CardEffect
            {
                type = EffectType.Damage,
                dmg = 2,
                targetlogic = TargetLogic.AllEnemiesInRange,
                effectRange = effectRange[0],
                animTrigger = "AreaAttack",
                isBuff = true,
            },
            statusDuration = 3,
        };

        effects.Add(cf);
    }

    public override string EffectDescription =>
        $"턴 종료 시 주변에 {effects[0].onTurnEffect.dmg} 피해를 줍니다. ({effects[0].statusDuration}턴 지속)";
}
