using UnityEngine;

public class DrawCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 1;
        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.Draw, 1, TargetLogic.self, null));
        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.Draw, 1, TargetLogic.self, null));
    }

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
