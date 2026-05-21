using UnityEngine;

public class PersistentShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "PersistentShieldCard";
        Cost = 2;
        type = CardType.Action;

        // 즉시 방어도 부여
        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Shield,
            4,
            TargetLogic.self,
            effectRange[0],
            lockCasterForNext: true
        ));

        // 다음 아군 턴 시작 시 방어도 부여 (1턴 후 소멸)
        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ApplyTurnEffect,
            0,
            TargetLogic.self,
            effectRange[0]
        )
        {
            onTurnEndEffect = new CardEffect(
                Board.BoardMode.Inspect,
                EffectType.Shield,
                4,
                TargetLogic.self
            ),
            turnDuration = 1,
            turnPhase = TurnPhase.OwnTurnStart
        });
    }

    public override string EffectDescription =>
        $"방어도 {effects[0].dmg} 부여, 다음 턴 시작 시 방어도 {effects[1].onTurnEndEffect.dmg} 추가";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
