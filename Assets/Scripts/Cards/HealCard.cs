using UnityEngine;
using UnityEngine.EventSystems;

public class HealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "HealCard";
        Cost = 2;
        type = CardType.Skill;
        CardEffect cf = new CardEffect(Board.BoardMode.targeting, EffectType.Heal, 2, TargetLogic.self);
        effects.Add(cf);
    }
    public override string EffectDescription => $"자신을 {effects[0].dmg} 회복합니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
