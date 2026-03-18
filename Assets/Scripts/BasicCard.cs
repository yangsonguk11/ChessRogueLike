using UnityEngine;
using UnityEngine.EventSystems;

public class BasicCard : Card
{
    public override void Awake()
    {
        base.Awake();
        CardEffect cf = new CardEffect();
        cf.requiredMode = Board.BoardMode.command;
        cf.type = EffectType.Move;
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
