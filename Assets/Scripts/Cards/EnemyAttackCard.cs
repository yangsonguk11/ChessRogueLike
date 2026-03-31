using UnityEngine;

public class EnemyAttackCard : Card
{
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Awake();
        Cost = 2;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, effectRange[0], TargetLogic.NearestEnemy);
        effects.Add(cf);
    }
    public override bool CanUse()
    {
        throw new System.NotImplementedException();
    }

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
