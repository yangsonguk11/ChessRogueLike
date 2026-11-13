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

        // 버리기를 먼저 해야 버린 수가 정해진다. 버리기는 시전자 자신이 대상(self)이라 조준에서 빠지고, 피해 효과가
        // 첫 조준 효과(Card.AimEffectIndex)가 된다 — 적에게 놓는 순간 손패를 버린 뒤 그 적을 친다.
        effects.Add(new CardEffect
        {
            type = EffectType.DiscardHand,
            dmg = 0, // 0 = 손패 전부
            targetlogic = TargetLogic.self,
        });
        effects.Add(new CardEffect
        {
            type = EffectType.Damage,
            dmg = 4,
            hitsPerDiscarded = 1, // 버린 카드 1장당 1회 타격
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
        });
    }

    public override CardRarity Rarity => CardRarity.Rare;

    public override string EffectDescription =>
        $"손의 카드를 모두 버리고, 버린 카드 1장당 {EffectiveDmg(effects[1])}의 피해를 줍니다.";
}
