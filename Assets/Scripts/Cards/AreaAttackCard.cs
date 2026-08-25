using UnityEngine;

public class AreaAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "AreaAttackCard";
        Cost = 2;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.AnyTile; // 범위 공격: 특정 기물이 아니라 발동 기준점(칸)을 지정
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 2,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange[0],
            animTrigger = "AreaAttack",
        };
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
