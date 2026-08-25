using UnityEngine;

public class ColDamageAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ColDamageAttackCard";
        Cost = 1;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = null,
            useColDamageAsDmg = true,
            animTrigger = "Attack",
        });
    }

    public override string EffectDescription => $"이동 범위 내 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
