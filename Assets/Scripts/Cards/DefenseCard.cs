using UnityEngine;

public class DefenseCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DefenseCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 2,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
        };
        effects.Add(cf);

    }
    public override string EffectDescription => $"자신에게 방어도 {EffectiveShield(effects[0])}를 부여합니다.";
}
