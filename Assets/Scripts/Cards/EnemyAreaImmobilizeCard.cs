using UnityEngine;

// 적 전용: 시전자 중심 범위(effectRange[0]) 안의 상대 기물 전원을 duration턴간 이동 불가로 만든다.
// 이동 불가는 걸린 기물의 턴이 끝날 때 줄어들므로, 기본값 1이면 바로 다음 플레이어 턴 동안 묶였다가 그 턴이 끝날 때 풀린다.
// Enemy-BindKnight은 이어서 같은 범위에 EnemyAreaBlastCard로 큰 피해를 준다.
public class EnemyAreaImmobilizeCard : Card
{
    public int duration = 1;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAreaImmobilizeCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            statusEffectType = StatusEffectType.MovementDisabled,
            statusDuration = duration,
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription => $"주변 범위의 적 모두를 {effects[0].statusDuration}턴간 이동 불가 상태로 만듭니다.";
}
