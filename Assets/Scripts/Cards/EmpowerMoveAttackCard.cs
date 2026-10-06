using UnityEngine;

// 다음 이동공격(이동공격 판정 공격 포함)의 피해를 올리는 버프. 이동공격할 때까지 유지되고, 여러 장 쓰면 합산해 한 번에 소모된다.
public class EmpowerMoveAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EmpowerMoveAttackCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.NextMoveAttackDamage,
            statusDuration = -1, // 이동공격할 때까지 유지
            statusPower = 4,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => $"다음 이동공격의 피해가 {effects[0].statusPower} 증가합니다.";
}
