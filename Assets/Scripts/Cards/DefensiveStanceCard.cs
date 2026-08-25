using UnityEngine;

// 방어 태세 카드
// - 이미 이동했으면 사용 불가
// - 사용 시 자신에게 방어도 부여
// - 사용 후 이번 턴 이동 불가
public class DefensiveStanceCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DefensiveStanceCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        blocksMovementAfterUse = true;
        requiresCasterNotMoved = true;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 5,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
            lockCasterForNext = true,
        });
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.DeBuff,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.MovementDisabled,
            statusDuration = 2,
        });
    }

    public override string EffectDescription => $"방어도 {EffectiveShield(effects[0])}를 부여합니다. (이동 전 사용 가능, 사용 후 이동 불가)";
}
