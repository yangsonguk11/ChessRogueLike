using UnityEngine;
using UnityEngine.EventSystems;

public class MoveandAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;

        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 0, effectRange[0], TargetLogic.NearestEnemy);
        effects.Add(cf);

        cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, effectRange[1], TargetLogic.LowestHP);
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
