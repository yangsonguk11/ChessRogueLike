using UnityEngine;

public class MoveAndDrawCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveAndDrawCard";
        Cost = 2;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = null,
            lockCasterForNext = true,
            animTrigger = "Move",
        });

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Draw,
            dmg = 1,
            targetlogic = TargetLogic.self,
        });
    }

    public override string EffectDescription => "이동한 후 카드를 1장 드로우합니다.";
}
