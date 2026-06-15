using UnityEngine;

public class SafeMoveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SafeMoveCard";
        Cost = 1;
        type = CardType.Move;

        effects.Add(new CardEffect(
            Board.BoardMode.command,
            EffectType.Move,
            0,
            TargetLogic.NearestEnemy
        )
        { noMoveAttack = true, animTrigger = "Move" });
    }

    public override string EffectDescription => "이동합니다. (이동공격 불가)";
}
