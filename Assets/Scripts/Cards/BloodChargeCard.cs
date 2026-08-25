using UnityEngine;

public class BloodChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "BloodChargeCard";
        Cost = 2;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            animTrigger = "Move",
            healOnHit = true,
        });
    }

    public override string EffectDescription => "이동합니다. 이동공격 시 입힌 피해만큼 체력을 회복합니다.";
}
