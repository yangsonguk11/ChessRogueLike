// 적 하나에게 취약(StatusEffectType.Vulnerable)을 부여해 attack/moveattack으로 받는 피해를 늘린다.
// 사거리 제한 없이 보드 위 어느 적에게든 쓸 수 있다. 영구 지속으로 바꾸려면 statusDuration을 -1로 둔다.
public class VulnerableCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "VulnerableCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            statusEffectType = StatusEffectType.Vulnerable,
            statusDuration = 2,
            statusPower = 1,
            animTrigger = "ApplyStatus",
            noRangeLimit = true,
        });
    }

    public override string EffectDescription => effects[0].statusDuration < 0
        ? $"취약({effects[0].statusPower})을 부여합니다."
        : $"{effects[0].statusDuration}턴간 취약({effects[0].statusPower})을 부여합니다.";
}
