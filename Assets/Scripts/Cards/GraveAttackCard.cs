using UnityEngine;

// 적에게 피해 후, 무덤 1을 소모해 같은 대상에게 추가 피해 — 무덤이 없으면 추가 타격만 스킵
public class GraveAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "GraveAttackCard";
        Cost = 2;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 3,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            animTrigger = "Attack",
        });
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = 3,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            useLastTarget = true, // 같은 대상에게 추가 타격 — 다시 클릭하지 않음
            graveCost = 1,
            animTrigger = "Attack",
        });
    }

    public override string EffectDescription =>
        $"적에게 {EffectiveDmg(effects[0])} 피해를 줍니다. 무덤을 {effects[1].graveCost} 소모해 같은 대상에게 {EffectiveDmg(effects[1])} 피해를 더 줍니다.";
}
