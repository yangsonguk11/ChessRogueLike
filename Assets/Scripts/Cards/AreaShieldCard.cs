using UnityEngine;

public class AreaShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 1;
        CardEffect cf = new CardEffect(Board.BoardMode.Inspect, EffectType.Shield, 3, TargetLogic.AllAlliesInRange, effectRange[0]);
        effects.Add(cf);
    }

    public override bool CanUse()
    {
        throw new System.NotImplementedException();
    }

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
