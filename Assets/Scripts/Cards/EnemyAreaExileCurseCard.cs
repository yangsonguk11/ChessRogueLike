using UnityEngine;

// 적 전용: 시전자 중심 범위(effectRange[0]) 안의 상대 기물 전원에게 duration턴간 소멸 저주(ExileOnDrawEffect)를 건다.
// 상태이상은 걸린 기물의 턴이 끝날 때 줄어드므로, 기본값 1이면 바로 다음 플레이어 턴에 뽑는 카드(턴 시작 5장 + 드로우 효과)에
// 모두 소멸이 붙는다. 예시 적 Enemy-CurseKnight이 쓴다.
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
            statusEffectType = StatusEffectType.ExileOnDraw,
            statusDuration = duration,
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription => $"주변 범위의 적 모두에게 {effects[0].statusDuration}턴간 소멸 저주를 겁니다.";
}
