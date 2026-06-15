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

        CardEffect damage = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            5,
            TargetLogic.AllEnemiesInRange,
            range,
            false,
            AreaTargetMode.Fixed
        )
        { animTrigger = "AreaAttack" };
        // BoardMode.command + TargetLogic.self → ResolveEnemyTarget returns selectedButton (boss pos)
        CardEffect shield = new CardEffect(
            Board.BoardMode.command,
            EffectType.Shield,
            shieldAmount,
            TargetLogic.self
        )
        { animTrigger = "Shield" };
        effects.Add(damage);
        effects.Add(shield);
    }

    public override string EffectDescription =>
        $"5x5 범위에 {effects[0].dmg} 피해를 주고, 방어도 {shieldAmount}를 획득합니다.";
}
