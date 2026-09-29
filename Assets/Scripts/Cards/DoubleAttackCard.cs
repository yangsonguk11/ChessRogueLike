using UnityEngine;

public class DoubleAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DoubleAttackCard";
        Cost = 3;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 4,
            hitCount = 2,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
        });
    }

    public override string EffectDescription => $"{EffectiveDmg(effects[0])}의 피해를 {effects[0].hitCount}번 줍니다.";
}
