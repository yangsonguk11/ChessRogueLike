using UnityEngine;

// 이번 턴 안에 하는 다음 이동공격(이동공격 판정 공격 포함)의 피해를 배수로 키우는 버프. 이번 턴에 이동공격하지 않으면 턴 종료 시 사라진다.
public class MightyChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MightyChargeCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.NextMoveAttackMultiplier,
            statusDuration = 1, // 이번 턴 동안
            statusPower = 2,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => $"이번 턴 동안 다음 이동공격의 피해가 {effects[0].statusPower}배가 됩니다.";
}
