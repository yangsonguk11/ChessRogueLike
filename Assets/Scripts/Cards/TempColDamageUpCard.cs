using UnityEngine;

public class TempColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "TempColDamageUpCard";
        Cost = 0;
        type = CardType.Skill;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ColDamageUp,
            2,
            TargetLogic.self,
            effectRange[0],
            true
        ));

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
                EffectType.ColDamageUp,
                -2,
                TargetLogic.self
            ),
            turnDuration = 1,
            turnPhase = TurnPhase.OwnTurnStart
        });
    }

    public override string EffectDescription => "이번 턴 동안 이동공격력을 2 올립니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
