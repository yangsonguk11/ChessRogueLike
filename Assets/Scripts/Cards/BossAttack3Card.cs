using UnityEngine;

// 보스 패턴 3: 주변 5x5 범위에 피해를 주고 방어도를 획득
public class BossAttack3Card : Card
{
    public int shieldAmount = 5;

    public override void Awake()
    {
        base.Awake();
        Name = "보스 패턴 3";
        Cost = 0;
        type = CardType.Skill;
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;

        CardEffect damage = new CardEffect
        {
            type = EffectType.Damage,
            dmg = 5,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = range,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "AreaAttack",
            vfx = "Ring_Test" // 대상 위치 이펙트 — 임시 테스트 이펙트(아트 교체 전)
        };
        // TargetLogic.self → ResolveTargetPos가 시전자 칸(보스 자신)을 반환
        CardEffect shield = new CardEffect
        {
            type = EffectType.Shield,
            dmg = shieldAmount,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
        };
        effects.Add(damage);
        effects.Add(shield);
    }

    public override string EffectDescription =>
        $"모든 적에게 {EffectiveDmg(effects[0])} 피해를 주고, 방어도 {EffectiveShield(effects[1])}를 획득합니다.";
}
