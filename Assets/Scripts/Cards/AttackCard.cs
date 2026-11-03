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
        dragDropTarget = DragDropTarget.Enemy;
        CardEffect cf = new CardEffect
        {
            type = EffectType.Damage,
            dmg = 4,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
            vfx = "Burst_Test", // 대상 위치 이펙트 — 임시 테스트 이펙트(아트 교체 전)
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"적에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
