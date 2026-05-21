using UnityEngine;

public class ChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ChargeCard";
        Cost = 1;
        type = CardType.Move;
        shieldOnMoveAttack = true;
        moveAttackShieldAmount = 3;

        effects.Add(new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy));
    }

    public override string EffectDescription => $"이동합니다. 이동공격 시 방어도 {moveAttackShieldAmount}를 획득합니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
