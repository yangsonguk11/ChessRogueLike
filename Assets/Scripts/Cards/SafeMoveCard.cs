using UnityEngine;

public class SafeMoveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SafeMoveCard";
        Cost = 1;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            noMoveAttack = true,
            animTrigger = "Move",
        });
    }

    public override string EffectDescription => "이동합니다. (이동공격 불가)";
}
