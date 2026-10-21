using UnityEngine;

public class ColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ColDamageUpCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        exileOnUse = true;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ColDamageUp,
            dmg = 2,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => $"힘을 {effects[0].dmg} 올립니다.";
}
