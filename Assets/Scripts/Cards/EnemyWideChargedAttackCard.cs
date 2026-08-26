using UnityEngine;

// 적 전용: 넓은 범위(EnemyAttackCard와 동일한 RangeInfoSO)에 기본 10 데미지 + 이동공격력 보너스를 가한다.
// 텔레그래프형 적(White Knight 5)의 사이클 3단계 — 1단계에서 쌓인 이동공격력(+5)만큼 데미지가 추가로 붙어
// 첫 사이클 기준 10 + 5 = 15 데미지가 나온다. ColDamageUp은 영구 누적이라 사이클을 반복할수록 더 세진다.
public class EnemyWideChargedAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyWideChargedAttackCard";
        Cost = 0;
        type = CardType.Attack;
        user = User.Enemy;

        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 10,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = range,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription => $"넓은 범위의 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
