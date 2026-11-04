using UnityEngine;

public class EnemyAttackCard : Card
{
    
    [SerializeField] int dmg = 5;
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAttackCard";
        Cost = 2;
        type = CardType.Attack;
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        CardEffect cf = new CardEffect
        {
            type = EffectType.Damage,
            dmg = dmg,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = range,
            animTrigger = "Attack",
            vfx = "Burst_Test", // 대상 위치 이펙트 — 임시 테스트 이펙트(아트 교체 전)
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
