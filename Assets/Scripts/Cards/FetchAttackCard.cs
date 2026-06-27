using UnityEngine;

public class FetchAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "FetchAttackCard";
        Cost = 0;
        type = CardType.Skill;

        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.AddCard, 0, TargetLogic.self)
        {
            addCardID = "AttackCard",
            addCardZone = CardPositionZone.Deck
        });
    }

    public override string EffectDescription => "공격 카드를 1장 손에 가져옵니다.";
}
