using UnityEngine;

public class ColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ColDamageUpCard";
        Cost = 1;
        type = CardType.Skill;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ColDamageUp,
            1,
            TargetLogic.self
        ));
    }

    public override string EffectDescription => "이동공격력을 1 올립니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
