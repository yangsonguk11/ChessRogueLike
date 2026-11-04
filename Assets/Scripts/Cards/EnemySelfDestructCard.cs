using UnityEngine;

// 적 전용 자폭: 시전자 중심 범위(effectRange[0])의 적 전원에게 damage + 힘 보너스만큼 피해를 준 뒤, 자신이 selfDamage만큼
// 피해를 받는다(기본 999 — 사실상 사망). Enemy-Balloon(소환수)이 쓴다. 범위 안에 적이 없을 때 터지지 않게 하는 것은
// 기물 프리팹의 조건부 행동(NoTargetInRange → 이동)이 맡는다.
public class EnemySelfDestructCard : Card
{
    public int damage = 10;
    public int selfDamage = 999;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemySelfDestructCard";
        Cost = 0;
        type = CardType.Attack;

        effects.Add(new CardEffect
        {
            type = EffectType.Damage,
            dmg = damage,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "AreaAttack",
        });
        effects.Add(new CardEffect
        {
            type = EffectType.SelfDamage,
            dmg = selfDamage,
            targetlogic = TargetLogic.self,
        });
    }

    public override string EffectDescription =>
        $"모든 적에게 {EffectiveDmg(effects[0])} 피해를 주고, 자신은 {selfDamage} 피해를 받습니다.";
}
