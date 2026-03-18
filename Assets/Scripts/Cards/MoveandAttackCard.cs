using UnityEngine;
using UnityEngine.EventSystems;

public class MoveandAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;

        CardEffect cf = new CardEffect();
        cf.requiredMode = Board.BoardMode.command;
        cf.type = EffectType.Move;
        effects.Add(cf);

        cf = new CardEffect();
        cf.requiredMode = Board.BoardMode.command;
        cf.type = EffectType.Damage;
        cf.dmg = 3;
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
