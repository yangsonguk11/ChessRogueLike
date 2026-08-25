using UnityEngine;

public class ShieldBonusUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ShieldBonusUpCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ShieldBonusUp,
            dmg = 1,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "방어막 보너스를 1 올립니다.";
}
