using UnityEngine;

public class TempColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "TempColDamageUpCard";
        Cost = 0;
        type = CardType.Skill;

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ApplyStatus,
            0,
            TargetLogic.self
        )
        {
            statusEffectType = StatusEffectType.Strengthen,
            statusDuration = 1,
            statusPower = 2,
            animTrigger = "Buff"
        });
    }

    public override string EffectDescription => "이번 턴 동안 이동공격력을 2 올립니다.";
}
