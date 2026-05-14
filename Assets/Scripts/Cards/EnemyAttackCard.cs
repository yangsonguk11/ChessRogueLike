using UnityEngine;

public class EnemyAttackCard : Card
{
    

    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.NearestEnemy, range);
        effects.Add(cf);
    }
    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
