using UnityEngine;
using UnityEngine.EventSystems;

public class MagicAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MagicAttackCard";
        Cost = 3;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyPiece;
        CardEffect cf = new CardEffect(Board.BoardMode.targeting, EffectType.Damage, 10, TargetLogic.NearestEnemy, effectRange[0])
            { animTrigger = "Attack", hasCaster = false };
        effects.Add(cf);
    }
    public override string EffectDescription => $"기물에 {EffectiveDmg(effects[0])}의 데미지를 줍니다.";
}
