using UnityEngine;
using UnityEngine.EventSystems;

public class MoveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveCard";
        Cost = 1;
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
