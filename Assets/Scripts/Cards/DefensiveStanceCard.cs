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
        Name = "DefensiveStanceCard";
        Cost = 1;
        type = CardType.Skill;
        blocksMovementAfterUse = true;

        effects.Add(new CardEffect(Board.BoardMode.targeting, EffectType.Shield, 5, TargetLogic.self));
    }

    public override string EffectDescription => $"방어도 {effects[0].dmg}를 부여합니다. (이동 전 사용 가능, 사용 후 이동 불가)";

    public override bool CanUse() => !Board.playerMovedThisTurn;
}
