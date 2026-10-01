using UnityEngine;

// 적에게 피해를 주고, 처치 시 무덤을 1 얻는다 — ExecutionerCard(처치 시 ColDamageUp)와 같은 onKillEffect 패턴, AddGrave만 다름
public class GraveHarvestCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "GraveHarvestCard";
        Cost = 2;
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
                type = EffectType.AddGrave,
                dmg = 1,
                targetlogic = TargetLogic.self,
            },
        });
    }

    public override string EffectDescription =>
        $"적에게 {EffectiveDmg(effects[0])} 피해를 줍니다. 처치 시 무덤을 {effects[0].onKillEffect.dmg} 얻습니다.";
}
