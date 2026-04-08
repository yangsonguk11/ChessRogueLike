using UnityEngine;
using UnityEngine.EventSystems;

public class AttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.LowestHP, effectRange[0]);
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
