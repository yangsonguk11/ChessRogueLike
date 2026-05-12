using UnityEngine;

public class HeavyAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        // 효과 1: 선택한 적에게 강한 피해 (서서 공격 애니메이션)
        effects.Add(new CardEffect(Board.BoardMode.targeting, EffectType.Damage, 6, TargetLogic.LowestHP, effectRange[0]));
        // 효과 2: 시전자가 자기 피해를 입음 (별도 로직)
        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.SelfDamage, 2, TargetLogic.self, null));
    }

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
