using UnityEngine;

public class RecoverShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "RecoverShieldCard";
        Description = "방어도를 얻고 버린 카드 1장을 덱으로 되돌린다.";
        Cost = 1;
        type = CardType.Skill;

        effects.Add(new CardEffect(Board.BoardMode.targeting, EffectType.Shield, 4, TargetLogic.self)
            { animTrigger = "Shield" });

        effects.Add(new CardEffect(Board.BoardMode.cardSelecting, EffectType.SelectAndReturnToDeck, 0, TargetLogic.self)
        {
            cardZone = CardZone.Discard,
            selectCount = 1
        });
    }

    public override string EffectDescription =>
        $"방어도 {EffectiveShield(effects[0])}를 얻고\n버린 카드 1장을 덱으로 되돌립니다.";
}
