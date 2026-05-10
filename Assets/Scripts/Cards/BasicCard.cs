using UnityEngine;
using UnityEngine.EventSystems;

public class BasicCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 1;

        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy, effectRange[0]);
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
