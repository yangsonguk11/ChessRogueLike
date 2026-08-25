using UnityEngine;

public class HeavyAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "HeavyAttackCard";
        Cost = 2;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.Enemy;
        // 효과 1: 선택한 적에게 강한 피해 (서서 공격 애니메이션)
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 6,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
        });
        // 효과 2: 시전자가 자기 피해를 입음 (별도 로직)
        effects.Add(new CardEffect { requiredMode = Board.BoardMode.Inspect, type = EffectType.SelfDamage, dmg = 2, targetlogic = TargetLogic.self, effectRange = null });
    }

    public override string EffectDescription => $"적에게 {EffectiveDmg(effects[0])} 피해를 주고, 자신도 {effects[1].dmg} 피해를 받습니다.";
}
