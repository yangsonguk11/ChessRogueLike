// 적 전용: 자신에게 가시(Thorn)를 부여한다. 가시형 적(Enemy-ThornKnight)의 사이클 1단계.
public class EnemyThornCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyThornCard";
        Cost = 0;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        // 인스펙터에 빈 RangeInfoSO(EmptyRangeInfo)를 넣어 예고에 범위가 뜨지 않게 한다(자기 대상 효과는 effectRange를
        // 비워 둬도 AutoPiece.GetMoveableButton이 범위를 표시하지 않는다).
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        effects.Add(new CardEffect
        {
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            effectRange = range,
            statusEffectType = StatusEffectType.Thorn,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.TickDuration 참고)
            statusPower = 2,
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription => $"자신에게 가시({effects[0].statusPower})를 부여합니다.";
}
