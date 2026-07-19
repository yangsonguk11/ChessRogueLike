using UnityEngine;

public class ShieldBonusUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ShieldBonusUpCard";
        Cost = 1;
        type = CardType.Skill;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ShieldBonusUp,
            1,
            TargetLogic.self
        )
        { animTrigger = "Buff" });
    }

    public override string EffectDescription => "방어막 보너스를 1 올립니다.";
}
