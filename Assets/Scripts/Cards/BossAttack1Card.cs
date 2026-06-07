using UnityEngine;

// 보스 패턴 1: 상하좌우 + 대각선 방향으로 3칸씩 공격 (퀸 이동 형태)
public class BossAttack1Card : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "보스 패턴 1";
        Cost = 0;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            4,
            TargetLogic.AllEnemiesInRange,
            effectRange.Count > 0 ? effectRange[0] : null,
            false,
            AreaTargetMode.Fixed
        );
        effects.Add(cf);
    }

    public override string EffectDescription => $"상하좌우+대각선으로 {effects[0].dmg} 피해를 줍니다.";
}
