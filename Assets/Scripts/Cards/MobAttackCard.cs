using UnityEngine;

// Enemy-Mob 전용 공격: EnemyAttackCard와 같은 대상·범위에 기본 7 피해(Mob의 힘 7에 맞춤)를 준다.
public class MobAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MobAttackCard";
        Cost = 2;
        type = CardType.Attack;
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        CardEffect cf = new CardEffect
        {
            type = EffectType.Damage,
            dmg = 7,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = range,
            animTrigger = "Attack",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
