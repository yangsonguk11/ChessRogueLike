using UnityEngine;

public class ChainMoveAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ChainMoveAttackCard";
        Cost = 3;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        exileOnUse = true;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.GrantChainMoveAttack,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "이동 공격 시 이동 범위 내 적에게 한 번 더 같은 피해를 줍니다. (버프, 소멸)";
}
