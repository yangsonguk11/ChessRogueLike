using UnityEngine;

public class AreaAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        CardEffect cf = new CardEffect(Board.BoardMode.targeting, EffectType.Damage, 2, TargetLogic.AllEnemiesInRange, effectRange[0]);
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
