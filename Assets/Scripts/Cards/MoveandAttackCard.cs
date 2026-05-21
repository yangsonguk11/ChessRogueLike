using UnityEngine;
using UnityEngine.EventSystems;

public class MoveAndAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveAndAttackCard";
        Cost = 2;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy, null, true);
        effects.Add(cf);

        cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.LowestHP, effectRange[0]);
        effects.Add(cf);
    }
    public override string EffectDescription => $"이동한 후 적에게 {effects[1].dmg} 피해를 줍니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
