using UnityEngine;
using UnityEngine.EventSystems;

public class MoveAndAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveAndAttackCard";
        Cost = 3;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Move, 0, TargetLogic.NearestEnemy, null, true)
            { animTrigger = "Move" };
        effects.Add(cf);

        cf = new CardEffect(Board.BoardMode.command, EffectType.Damage, 3, TargetLogic.LowestHP, effectRange[0])
            { animTrigger = "Attack" };
        effects.Add(cf);
    }
    public override string EffectDescription => $"이동한 후 적에게 {EffectiveDmg(effects[1])} 피해를 줍니다.";
}
