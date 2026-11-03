using UnityEngine;
using UnityEngine.EventSystems;

public class MagicAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MagicAttackCard";
        Cost = 3;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyPiece;
        CardEffect cf = new CardEffect
        {
            type = EffectType.Damage,
            dmg = 10,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = effectRange[0],
            animTrigger = "Attack",
            vfx = "Beam_Test", // 임시 테스트 이펙트(아트 교체 전)
            ignoreCasterColDamageBonus = true,
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"기물에 {EffectiveDmg(effects[0])}의 마법 피해를 줍니다.";
}
