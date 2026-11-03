using UnityEngine;

public class SummonGrowthCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SummonGrowthCard";
        Cost = 3;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        exileOnUse = true;

        effects.Add(new CardEffect // 1번째: self 즉시실행
        {
            type = EffectType.ApplyStatus,
            statusEffectType = StatusEffectType.TurnEffect,
            targetlogic = TargetLogic.self,
            turnPhase = TurnPhase.OwnTurnEnd,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.TickDuration 참고)
            onTurnEffect = new CardEffect
            {
                type = EffectType.GrantSummonColDamage,
                dmg = 1,
                targetlogic = TargetLogic.self,
                isBuff = true,
                animTrigger = "Buff", // 매 턴 발동 시에도 캐스터가 버프 포즈를 재생하도록
            },
            animTrigger = "Buff",
        });
        effects.Add(new CardEffect // 2번째: 후속 self 효과라 재클릭 없이 같은 시전자에게 자동 적용
        {
            type = EffectType.ApplyStatus,
            statusEffectType = StatusEffectType.TurnEffect,
            targetlogic = TargetLogic.self,
            turnPhase = TurnPhase.OwnTurnEnd,
            statusDuration = -1,
            onTurnEffect = new CardEffect
            {
                type = EffectType.GrantSummonMaxHp,
                dmg = 3,
                targetlogic = TargetLogic.self,
                isBuff = true,
                animTrigger = "Buff", // 매 턴 발동 시에도 캐스터가 버프 포즈를 재생하도록
            },
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;

    public override string EffectDescription =>
        $"매 턴 종료 시 다음에 소환할 기물의 힘을 {effects[0].onTurnEffect.dmg}, 체력을 {effects[1].onTurnEffect.dmg} 올립니다. (소환 시 소모됨)";
}
