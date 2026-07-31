using UnityEngine;

// 범위 회복 카드: 시전자를 중심으로 범위 내 아군을 회복
public class AreaHealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "AreaHealCard";
        Cost = 2;
        type = CardType.Skill;

        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Heal, 5, TargetLogic.AllAlliesInRange, effectRange[0])
            { animTrigger = "Heal" };
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 아군을 {effects[0].dmg} 회복합니다.";
}
