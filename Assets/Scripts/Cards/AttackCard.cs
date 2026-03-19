using UnityEngine;
using UnityEngine.EventSystems;

public class AttackCard : Card
{
    [SerializeField] RangeInfoSO effectRange;
    public override void Awake()
    {
        base.Awake();
        Cost = 2;
        CardEffect cf = new CardEffect();
        cf.requiredMode = Board.BoardMode.command;
        cf.type = EffectType.Damage;
        cf.dmg = 3;
        cf.effectRange = effectRange;
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
