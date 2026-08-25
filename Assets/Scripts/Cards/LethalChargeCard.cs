using UnityEngine;

public class LethalChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "LethalChargeCard";
        Cost = 3;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            animTrigger = "Move",
            onKillEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.RestoreEnergy,
                dmg = 2,
                targetlogic = TargetLogic.self,
            },
        });
    }

    public override string EffectDescription =>
        $"이동합니다. 이동공격으로 적을 처치하면 코스트를 {effects[0].onKillEffect.dmg} 회복합니다.";
}
