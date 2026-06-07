using UnityEngine;
using UnityEngine.EventSystems;

public class AttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "AttackCard";
        Cost = 1;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.LowestHP, effectRange[0]);
        effects.Add(cf);
    }
    public override string EffectDescription => $"적에게 {effects[0].dmg} 피해를 줍니다.";
}
