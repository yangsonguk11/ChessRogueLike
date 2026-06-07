using UnityEngine;

// 4방향 광역 공격 카드
// RangeInfoSO의 패턴은 위쪽 방향 기준으로 Inspector에서 작성
// 시전자에서 마우스 방향으로 패턴이 회전되어 발동
public class DirectionalAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DirectionalAttackCard";
        Cost = 2;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            5,
            TargetLogic.AllEnemiesInRange,
            effectRange[0],
            false,
            AreaTargetMode.Directional4
        );
        effects.Add(cf);
    }

    public override string EffectDescription => $"방향을 선택해 범위 내 적에게 {effects[0].dmg} 피해를 줍니다.";
}
