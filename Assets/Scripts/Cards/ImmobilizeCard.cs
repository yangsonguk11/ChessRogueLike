using UnityEngine;

public class ImmobilizeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ImmobilizeCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.DeBuff,
            0,
            TargetLogic.NearestEnemy
        )
        {
            statusEffectType = StatusEffectType.MovementDisabled,
            statusDuration = 2,
            animTrigger = "DeBuff"
        });
    }

    public override string EffectDescription =>
        $"적을 선택해 {effects[0].statusDuration}턴간 이동 불가 상태로 만듭니다.";
}
