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

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ColDamageUp,
            dmg = 1,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "이동공격력을 1 올립니다.";
}
