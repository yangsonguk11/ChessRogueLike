using UnityEngine;

public class ChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 1;
        shieldOnMoveAttack = true;
        moveAttackShieldAmount = 3;

        effects.Add(new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy));
    }

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
