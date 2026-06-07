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
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.NearestEnemy, range);
        effects.Add(cf);
    }
    public override string EffectDescription => $"근거리에서 {effects[0].dmg} 피해를 줍니다.";
}
