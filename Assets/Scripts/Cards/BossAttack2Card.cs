using UnityEngine;

// 보스 패턴 2: 선풍기(바람개비) 형태로 회전하며 공격
// RangeInfoSO 패턴이 90도 회전 대칭 — 4개 날개 각 3칸
public class BossAttack2Card : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "보스 패턴 2";
        Cost = 0;
        type = CardType.Attack;
        CardEffect cf = new CardEffect
        {
            type = EffectType.Damage,
            dmg = 5,
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
