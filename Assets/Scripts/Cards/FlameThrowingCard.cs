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
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyTurnEffect,
            dmg = 0,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
            onTurnEndEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.Damage,
                dmg = 2,
                targetlogic = TargetLogic.AllEnemiesInRange,
                effectRange = effectRange[0],
                animTrigger = "AreaAttack",
            },
            turnDuration = 3,
        };

        effects.Add(cf);
    }

    public override string EffectDescription =>
        $"턴 종료 시 주변에 {effects[0].onTurnEndEffect.dmg} 피해를 줍니다. ({effects[0].turnDuration}턴 지속)";
}
