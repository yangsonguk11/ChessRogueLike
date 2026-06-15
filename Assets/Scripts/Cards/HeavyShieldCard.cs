using UnityEngine;

public class HeavyShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "HeavyShieldCard";
        Cost = 2;
        type = CardType.Skill;
        exileOnUse = true;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Shield,
            8,
            TargetLogic.self
        )
        { animTrigger = "Shield" });
    }

    public override string EffectDescription => $"자신에게 방어도 {effects[0].dmg}를 부여합니다. (소멸)";
}
