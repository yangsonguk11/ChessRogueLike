// 검증 전용 카드: StatusEffectType.Vulnerable(취약)을 부여해 attack/moveattack 피해 증가를
// 플레이테스트하기 위해 추가했다. 영구 지속을 테스트하려면 statusDuration을 -1로 바꾼다.
// 정식 카드로 유지할지는 플레이테스트 후 결정.
public class VulnerableTestCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "VulnerableTestCard";
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
        ? $"적을 선택해 취약({effects[0].statusPower})을 부여합니다. 공격으로 받는 피해가 {effects[0].statusPower} 증가합니다."
        : $"적을 선택해 {effects[0].statusDuration}턴간 취약({effects[0].statusPower})을 부여합니다. 공격으로 받는 피해가 {effects[0].statusPower} 증가합니다.";
}
