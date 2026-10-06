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
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyTurnEffect,
            targetlogic = TargetLogic.self,
            turnPhase = TurnPhase.OwnTurnStart,
            turnDuration = -1, // 음수 = 영구 지속 (StatusEffect.OnTurnEnd 참고)
            onTurnEndEffect = new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.AddGrave,
                dmg = 1,
                targetlogic = TargetLogic.self,
                isBuff = true,
            },
            animTrigger = "Buff",
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;
    public override string EffectDescription => $"매 턴 시작 시 무덤을 {effects[0].onTurnEndEffect.dmg} 얻습니다.";
}
