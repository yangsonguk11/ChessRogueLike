using UnityEngine;

// 손의 카드를 모두 버리고, 실제로 버려진 카드 1장당 같은 대상을 한 번씩 타격 — 버린 카드가 없으면 타격만 스킵
public class FinalAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "FinalAttackCard";
        Cost = 2;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.Enemy;

        // 버리기를 먼저 해야 버린 수가 정해지지만, 대상은 카드를 든 시점에 골라야 하므로(사거리 표시·취소 가능)
        // 버리기 효과가 대상 클릭을 받는다. targetlogic이 범위형(All*InRange)이면 ExecuteAreaEffect로 빠져
        // 버리기가 무시되므로 단일 대상으로 둔다.
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.DiscardHand,
            dmg = 0, // 0 = 손패 전부
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
        });
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 4,
            hitsPerDiscarded = 1, // 버린 카드 1장당 1회 타격
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            useLastTarget = true, // 버리기에서 고른 대상을 그대로 친다 — 다시 클릭하지 않음
            animTrigger = "Attack",
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;

    public override string EffectDescription =>
        $"손의 카드를 모두 버리고, 버린 카드 1장당 {EffectiveDmg(effects[1])}의 피해를 줍니다.";
}
