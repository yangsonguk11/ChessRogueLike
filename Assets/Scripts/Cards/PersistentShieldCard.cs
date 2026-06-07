using UnityEngine;

public class PersistentShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "PersistentShieldCard";
        Cost = 2;
        type = CardType.Skill;

        // 즉시 방어도 부여
        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Shield,
            4,
            TargetLogic.self,
            null,
            true
        ));

        // 다음 아군 턴 시작 시 방어도 부여 (1턴 후 소멸)
        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ApplyTurnEffect,
            0,
            TargetLogic.self
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
        $"방어도 {effects[0].dmg}를 부여하고, 다음 턴 시작 시 방어도 {effects[1].onTurnEndEffect.dmg}를 추가로 부여합니다.";
}
