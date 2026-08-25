using UnityEngine;

public class ExecutionerCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ExecutionerCard";
        Cost = 1;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 3,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
            onKillEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.BaseColDamageUp,
                dmg = 1,
                targetlogic = TargetLogic.self,
                animTrigger = "Buff",
            },
        });
    }

    public override string EffectDescription =>
        $"적에게 {EffectiveDmg(effects[0])} 피해를 줍니다. 처치 시 이동공격력이 영구적으로 {effects[0].onKillEffect.dmg} 오릅니다.";
}
