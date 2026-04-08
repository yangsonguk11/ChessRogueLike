using UnityEngine;

public class EnemyMoveCard : Card
{
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 3, TargetLogic.NearestEnemy, effectRange[0]);
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
