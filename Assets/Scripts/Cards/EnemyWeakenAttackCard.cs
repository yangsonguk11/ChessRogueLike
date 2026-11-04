using UnityEngine;

// 적 전용: effectRange[0] 안의 가장 가까운 상대에게 damage + 힘 보너스만큼 피해를 주고 약화(weaken)를 건다.
// duration이 음수면 영구다. 피해 효과에 붙은 상태이상은 공격 경로(Board.StrikeTarget → ApplyStatusEffect)가 맞은 대상에게 걸고,
// 같은 약화도 합치지 않아 맞을 때마다 하나씩 쌓인다(힘은 0 아래로 내려가지 않음 — WeakenEffect). Enemy-MobBalloon이 쓴다.
public class EnemyWeakenAttackCard : Card
{
    public int damage = 4;
    public int weaken = 1;
    public int duration = -1;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyWeakenAttackCard";
        Cost = 0;
        type = CardType.Attack;

        effects.Add(new CardEffect
        {
            type = EffectType.Damage,
            dmg = damage,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            statusEffectType = StatusEffectType.Weaken,
            statusPower = weaken,
            statusDuration = duration,
            animTrigger = "Attack",
        });
    }

    public override string EffectDescription => duration < 0
        ? $"적에게 {EffectiveDmg(effects[0])} 피해를 주고 약화({weaken})를 부여합니다."
        : $"적에게 {EffectiveDmg(effects[0])} 피해를 주고 {duration}턴간 약화({weaken})를 부여합니다.";
}
