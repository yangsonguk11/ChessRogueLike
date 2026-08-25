using UnityEngine;
using UnityEngine.EventSystems;

public class MoveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveCard";
        Cost = 2;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            animTrigger = "Move",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => "이동합니다.";
}
