using UnityEngine;

public class HeavyShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "HeavyShieldCard";
        Cost = 2;
        type = CardType.Action;
        exileOnUse = true;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Shield,
            8,
            TargetLogic.self,
            effectRange[0]
        ));
    }

    public override string EffectDescription => $"자신에게 방어도 {effects[0].dmg} 부여 (소멸)";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
