using UnityEngine;

public class ShieldCycleCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ShieldCycleCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 3,
            targetlogic = TargetLogic.self,
            effectRange = null,
            lockCasterForNext = true,
            animTrigger = "Shield",
        });

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.Inspect,
            type = EffectType.HandToDeckTop,
            dmg = 1,
            targetlogic = TargetLogic.self,
        });
    }

    public override string EffectDescription =>
        $"방어도 {EffectiveShield(effects[0])}를 부여하고, 손의 카드 {effects[1].dmg}장을 무작위로 덱 맨 위로 되돌립니다.";
}
