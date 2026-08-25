using UnityEngine;

public class FetchAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "FetchAttackCard";
        Cost = 0;
        type = CardType.Skill;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.Inspect,
            type = EffectType.AddCard,
            dmg = 0,
            targetlogic = TargetLogic.self,
            addCardID = "AttackCard",
            addCardZone = CardPositionZone.Hand,
        });
    }

    public override string EffectDescription => "공격 카드를 1장 손에 가져옵니다.";
}
