using UnityEngine;

// 4방향 광역 공격 카드
// RangeInfoSO의 패턴은 위쪽 방향 기준으로 Inspector에서 작성
// 시전자에서 마우스 방향으로 패턴이 회전되어 발동
public class DirectionalAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;

        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            4,
            TargetLogic.AllEnemiesInRange,
            effectRange[0],
            false,
            AreaTargetMode.Directional4
        );
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
