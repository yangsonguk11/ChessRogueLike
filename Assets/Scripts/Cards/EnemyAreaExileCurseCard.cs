using UnityEngine;

// 적 전용: 시전자 중심 범위(effectRange[0]) 안의 상대 기물 전원에게 duration턴간 소멸 저주(ExileOnUseEffect)를 건다.
// 상태이상은 걸린 기물의 턴이 끝날 때 줄어드므로, 기본값 1이면 바로 다음 플레이어 턴에 그 기물로 사용하는 카드가 모두 소멸한다.
// 예시 적 Enemy-CurseKnight이 쓴다.
public class EnemyAreaExileCurseCard : Card
{
    public int duration = 1;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAreaExileCurseCard";
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
            statusEffectType = StatusEffectType.ExileOnUse,
            statusDuration = duration,
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription => $"모든 적에게 {effects[0].statusDuration}턴간 소멸 저주를 겁니다.";
}
