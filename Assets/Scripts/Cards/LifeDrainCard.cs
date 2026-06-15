using UnityEngine;

public class LifeDrainCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "LifeDrainCard";
        Cost = 2;
        type = CardType.Attack;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            2,
            TargetLogic.AllEnemiesInRange,
            effectRange[0]
        )
        { healOnHit = 2, animTrigger = "AreaAttack" });
    }

    public override string EffectDescription => $"범위 내 적에게 {EffectiveDmg(effects[0])} 피해를 주고, 적중한 적마다 {effects[0].healOnHit}를 회복합니다.";
}
