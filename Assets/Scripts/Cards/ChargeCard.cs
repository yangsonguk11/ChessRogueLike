using UnityEngine;

public class ChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ChargeCard";
        Cost = 2;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;
        shieldOnMoveAttack = true;
        moveAttackShieldAmount = 3;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            animTrigger = "Move",
        });
    }

    public override string EffectDescription => $"이동합니다. 이동공격 시 방어도 {moveAttackShieldAmount}를 획득합니다.";
}
