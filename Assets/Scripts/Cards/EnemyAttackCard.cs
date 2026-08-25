using UnityEngine;

public class EnemyAttackCard : Card
{
    

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAttackCard";
        Cost = 2;
        type = CardType.Attack;
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 5,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = range,
            animTrigger = "Attack",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"근처 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
