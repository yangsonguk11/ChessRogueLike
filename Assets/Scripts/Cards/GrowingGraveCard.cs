// 시전자에게 영구 턴 효과를 건다 — 매 턴 시작 시 무덤 +1. 여러 장 쓰면 그만큼 겹친다(SummonGrowthCard와 같은 방식).
public class GrowingGraveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "GrowingGraveCard";
        Cost = 3;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        effects.Add(new CardEffect
        {
            type = EffectType.ApplyStatus,
            statusEffectType = StatusEffectType.TurnEffect,
            targetlogic = TargetLogic.self,
            turnPhase = TurnPhase.OwnTurnStart,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.TickDuration 참고)
            onTurnEffect = new CardEffect
            {
                type = EffectType.AddGrave,
                dmg = 1,
                targetlogic = TargetLogic.self,
                isBuff = true,
            },
            animTrigger = "Buff",
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;
    public override string EffectDescription => $"매 턴 시작 시 무덤을 {effects[0].onTurnEffect.dmg} 얻습니다.";
}
