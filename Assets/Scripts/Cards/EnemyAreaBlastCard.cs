using UnityEngine;

// 적 전용: 시전자 중심 범위(effectRange[0])의 적 전원에게 damage + 힘 보너스만큼 피해를 준다.
// Enemy-BindKnight(이동 불가 다음 큰 피해), Enemy-BondSweeper가 쓴다. 수호자의 격노 패턴은 이동이 붙은 EnemyMoveAndBlastCard.
public class EnemyAreaBlastCard : Card
{
    public int damage = 6;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAreaBlastCard";
        Cost = 0;
        type = CardType.Attack;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = damage,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription => $"모든 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
