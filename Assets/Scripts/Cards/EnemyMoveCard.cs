using UnityEngine;

public class EnemyMoveCard : Card
{
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyMoveCard";
        Cost = 2;
        type = CardType.Move;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy);
        effects.Add(cf);
    }
    public override string EffectDescription => "이동합니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
