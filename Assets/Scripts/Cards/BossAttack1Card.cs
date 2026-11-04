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
        CardEffect cf = new CardEffect
        {
            type = EffectType.Damage,
            dmg = 6,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "AreaAttack",
            vfx = "Lightning_Test" // 대상 위치 이펙트 — 임시 테스트 이펙트(아트 교체 전)
        };
        effects.Add(cf);
    }

    public override string EffectDescription => $"모든 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
