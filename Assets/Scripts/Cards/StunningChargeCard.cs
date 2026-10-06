using UnityEngine;

// 다음 이동공격(이동공격 판정 공격 포함)에 맞은 적(주 대상 + 스플래시)마다 기절을 거는 버프. 이동공격할 때까지 유지된다.
public class StunningChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "StunningChargeCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.NextMoveAttackStatus,
            statusDuration = -1, // 이동공격할 때까지 유지
            onMoveAttackHitEffect = new CardEffect
            {
                statusEffectType = StatusEffectType.Stun,
                statusDuration = 1,
            },
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription =>
        $"다음 이동공격에 적중한 적에게 기절을 {effects[0].onMoveAttackHitEffect.statusDuration}턴 부여합니다.";
}
