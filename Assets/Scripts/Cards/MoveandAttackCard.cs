using UnityEngine;
using UnityEngine.EventSystems;

public class MoveAndAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveAndAttackCard";
        Cost = 3;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.AnyTile;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = null,
            lockCasterForNext = true,
            animTrigger = "Move",
        };
        effects.Add(cf);

        cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 3,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"이동한 후 적에게 {EffectiveDmg(effects[1])} 피해를 줍니다.";
}
