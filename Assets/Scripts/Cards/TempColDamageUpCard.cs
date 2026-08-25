using UnityEngine;

public class TempColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "TempColDamageUpCard";
        Cost = 0;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.Strengthen,
            statusDuration = 1,
            statusPower = 2,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "이번 턴 동안 이동공격력을 2 올립니다.";
}
