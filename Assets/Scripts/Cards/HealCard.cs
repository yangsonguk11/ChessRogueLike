using UnityEngine;
using UnityEngine.EventSystems;

public class HealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        CardEffect cf = new CardEffect(Board.BoardMode.targeting, EffectType.Heal, 2, TargetLogic.self, effectRange[0]);
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
