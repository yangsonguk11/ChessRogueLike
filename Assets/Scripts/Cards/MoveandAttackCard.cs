using UnityEngine;
using UnityEngine.EventSystems;

public class MoveandAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;

        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy, null, true);
        effects.Add(cf);

        cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.LowestHP, effectRange[0]);
        effects.Add(cf);
    }
    public override bool CanUse() => !Board.playerMovedThisTurn;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
