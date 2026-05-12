using UnityEngine;

// 방어 태세 카드
// - 이미 이동했으면 사용 불가
// - 사용 시 자신에게 방어도 부여
// - 사용 후 이번 턴 이동 불가
public class DefensiveStanceCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 1;
        blocksMovementAfterUse = true;

        effects.Add(new CardEffect(Board.BoardMode.targeting, EffectType.Shield, 5, TargetLogic.self));
    }

    public override bool CanUse() => !Board.playerMovedThisTurn;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
