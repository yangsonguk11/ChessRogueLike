using UnityEngine;

public class MoveAndDrawCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveAndDrawCard";
        Cost = 2;
        type = CardType.Move;

        effects.Add(new CardEffect(
            Board.BoardMode.command,
            EffectType.Move,
            0,
            TargetLogic.NearestEnemy
        ));

        effects.Add(new CardEffect(
            Board.BoardMode.Inspect,
            EffectType.Draw,
            1,
            TargetLogic.self
        ));
    }

    public override string EffectDescription => "이동한 후 카드를 1장 드로우합니다.";
}
