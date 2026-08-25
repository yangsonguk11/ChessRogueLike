using UnityEngine;
using UnityEngine.EventSystems;

public class HealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "HealCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Heal,
            dmg = 2,
            targetlogic = TargetLogic.self,
            animTrigger = "Heal",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"자신을 {effects[0].dmg} 회복합니다.";
}
