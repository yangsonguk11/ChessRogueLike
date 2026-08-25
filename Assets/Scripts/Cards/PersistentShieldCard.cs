using UnityEngine;

public class PersistentShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "PersistentShieldCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        // 즉시 방어도 부여
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 4,
            targetlogic = TargetLogic.self,
            effectRange = null,
            lockCasterForNext = true,
            animTrigger = "Shield",
        });

        // 다음 아군 턴 시작 시 방어도 부여 (1턴 후 소멸)
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyTurnEffect,
            dmg = 0,
            targetlogic = TargetLogic.self,
            onTurnEndEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.Shield,
                dmg = 4,
                targetlogic = TargetLogic.self,
                animTrigger = "Shield",
            },
            turnDuration = 1,
            turnPhase = TurnPhase.OwnTurnStart,
        });
    }

    public override string EffectDescription =>
        $"방어도 {EffectiveShield(effects[0])}를 부여하고, 다음 턴 시작 시 방어도 {EffectiveShield(effects[1].onTurnEndEffect)}를 추가로 부여합니다.";
}
