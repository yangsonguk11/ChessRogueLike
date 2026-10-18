using UnityEngine;

// 적 전용(Boss-Pursuer): AllyLockedStrikeCard처럼 각 아군 위치를 중심으로 effectRange[0] 패턴을 잠가 피해를 주고,
// 맞은 적에게 duration턴간 이동 불가(MovementDisabledEffect)를 건다 — 피해 효과에 붙은 상태이상은 AreaAttackPiece가 건다.
// 적 턴에 걸리므로 기본값 2면 다음 플레이어 턴과 그다음 플레이어 턴 동안 이동할 수 없다.
public class AllyLockedImmobilizeStrikeCard : Card
{
    public int damage = 14;
    public int duration = 2;

    public override void Awake()
    {
        base.Awake();
        Name = "AllyLockedImmobilizeStrikeCard";
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
            lockOnAllyPositions = true,
            statusEffectType = StatusEffectType.MovementDisabled,
            statusDuration = duration,
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription =>
        $"모든 적에게 {EffectiveDmg(effects[0])} 피해를 주고 {effects[0].statusDuration}턴간 이동 불가를 겁니다.";
}
