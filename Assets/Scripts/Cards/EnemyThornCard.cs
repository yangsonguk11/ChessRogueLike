// 적 전용: 자신에게 가시(Thorn)를 부여한다. 가시형 적(White Knight 4)의 사이클 1단계.
public class EnemyThornCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyThornCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;
        dragDropTarget = DragDropTarget.Self;

        // effectRange를 비워두면 Enemy.GetMoveableButton()이 이동 범위로 폴백해서, 이동하지 않는 카드인데도
        // 예고에 이동 범위가 뜬다 — 인스펙터에 빈 RangeInfoSO(EmptyRangeInfo)를 넣어서 그 폴백을 막는다.
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            effectRange = range,
            statusEffectType = StatusEffectType.Thorn,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.OnTurnEnd 참고)
            statusPower = 2,
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription => $"자신에게 가시({effects[0].statusPower})를 부여합니다.";
}
