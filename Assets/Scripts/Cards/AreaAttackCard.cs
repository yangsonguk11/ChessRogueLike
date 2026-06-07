using UnityEngine;

public class AreaAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "AreaAttackCard";
        Cost = 2;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(Board.BoardMode.targeting, EffectType.Damage, 2, TargetLogic.AllEnemiesInRange, effectRange[0]);
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 적에게 {effects[0].dmg} 피해를 줍니다.";
}
