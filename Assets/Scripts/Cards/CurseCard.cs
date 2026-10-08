// 사거리 안의 적 하나에게 독을 건다 — 적의 턴이 끝날 때마다 고정 피해(힘·취약과 무관)가 들어간다.
public class CurseCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "CurseCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.LowestHP, // 적 AI가 쓸 경우의 대상 선택 기준(플레이어는 드롭으로 지정)
            effectRange = effectRange[0],
            statusEffectType = StatusEffectType.Poison,
            statusDuration = 3,
            statusPower = 3,
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription =>
        $"적에게 {effects[0].statusDuration}턴간 독({effects[0].statusPower})을 부여합니다.";
}
