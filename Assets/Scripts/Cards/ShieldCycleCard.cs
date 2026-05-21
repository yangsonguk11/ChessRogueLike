using UnityEngine;

public class ShieldCycleCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ShieldCycleCard";
        Cost = 1;
        type = CardType.Skill;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Shield,
            3,
            TargetLogic.self,
            effectRange[0],
            true
        ));

        effects.Add(new CardEffect(
            Board.BoardMode.Inspect,
            EffectType.HandToDeckTop,
            1,
            TargetLogic.self
        ));
    }

    public override string EffectDescription =>
        $"방어도 {effects[0].dmg}를 부여하고, 손의 카드 {effects[1].dmg}장을 무작위로 덱 맨 위로 되돌립니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
