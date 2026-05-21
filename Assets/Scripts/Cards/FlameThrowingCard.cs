using UnityEngine;

public class FlameThrowingCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "FlameThrowingCard";
        Cost = 1;

        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ApplyTurnEffect,
            0,
            TargetLogic.self
        )
        {
            onTurnEndEffect = new CardEffect(
                Board.BoardMode.Inspect,
                EffectType.Damage,
                2,
                TargetLogic.AllEnemiesInRange,
                effectRange[0]
            ),
            turnDuration = 3
        };

        effects.Add(cf);
    }

    public override string EffectDescription =>
        $"턴 종료 시 주변 {effects[0].onTurnEndEffect.dmg} 데미지, {effects[0].turnDuration}턴 지속";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
