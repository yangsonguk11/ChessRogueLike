using UnityEngine;

public class LifeDrainCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "LifeDrainCard";
        Cost = 2;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.AnyTile; // 범위 공격: 특정 기물이 아니라 발동 기준점(칸)을 지정

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 2,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange[0],
            healOnHit = true,
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription => $"범위 내 적에게 {EffectiveDmg(effects[0])} 피해를 주고, 적중한 적마다 입힌 피해만큼 회복합니다.";
}
