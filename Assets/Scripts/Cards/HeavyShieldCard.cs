using UnityEngine;

public class HeavyShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "HeavyShieldCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        exileOnUse = true;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 8,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
        });
    }

    public override string EffectDescription => $"자신에게 방어도 {EffectiveShield(effects[0])}를 부여합니다. (소멸)";
}
