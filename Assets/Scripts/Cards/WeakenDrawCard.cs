using UnityEngine;

// 사거리 안의 적에게 약화(힘 감소)를 걸고 카드를 드로우한다 — 드로우는 효과 1개당 1장이라 Draw 효과를 장수만큼 둔다.
public class WeakenDrawCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "WeakenDrawCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.LowestHP, // 적 AI가 쓸 경우의 대상 선택 기준(플레이어는 드롭으로 지정)
            effectRange = effectRange[0],
            statusEffectType = StatusEffectType.Weaken,
            statusDuration = 2,
            statusPower = 2,
            animTrigger = "ApplyStatus",
        });
        effects.Add(new CardEffect { requiredMode = Board.BoardMode.Inspect, type = EffectType.Draw, dmg = 1, targetlogic = TargetLogic.self });
        effects.Add(new CardEffect { requiredMode = Board.BoardMode.Inspect, type = EffectType.Draw, dmg = 1, targetlogic = TargetLogic.self });
    }

    public override CardRarity Rarity => CardRarity.Rare;

    public override string EffectDescription =>
        $"적에게 {effects[0].statusDuration}턴간 약화({effects[0].statusPower})를 부여하고 카드를 {effects.Count - 1}장 드로우합니다.";
}
