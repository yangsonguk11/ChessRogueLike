// 시전자에게 턴 효과 두 개를 건다 — 턴 종료마다 주변 적에게 고정 피해, 주변 아군 회복.
// 범위는 프리팹 effectRange[0](주변 8칸)이고 시전자 칸은 포함하지 않는다(FlameThrowingCard와 같은 방식).
public class WardZoneCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "WardZoneCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        effects.Add(new CardEffect // 1번째: self 즉시실행
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyTurnEffect,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
            turnDuration = 3,
            onTurnEndEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.Damage,
                dmg = 3,
                targetlogic = TargetLogic.AllEnemiesInRange,
                effectRange = effectRange[0],
                animTrigger = "AreaAttack",
                ignoreCasterColDamageBonus = true,
                isBuff = true,
            },
        });
        effects.Add(new CardEffect // 2번째: 후속 self 효과라 재클릭 없이 같은 시전자에게 자동 적용
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyTurnEffect,
            targetlogic = TargetLogic.self,
            turnDuration = 3,
            onTurnEndEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.Heal,
                dmg = 2,
                targetlogic = TargetLogic.AllAlliesInRange,
                effectRange = effectRange[0],
                animTrigger = "Heal",
                isBuff = true,
            },
        });
    }

    public override string EffectDescription =>
        $"{effects[0].turnDuration}턴 동안 턴 종료 시 주변 적에게 {effects[0].onTurnEndEffect.dmg} 고정 데미지, 주변 아군을 {effects[1].onTurnEndEffect.dmg} 회복합니다.";
}
