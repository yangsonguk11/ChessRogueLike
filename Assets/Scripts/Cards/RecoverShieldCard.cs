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
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 4,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
        });

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.cardSelecting,
            type = EffectType.SelectAndReturnToDeck,
            dmg = 0,
            targetlogic = TargetLogic.self,
            cardZone = CardZone.Discard,
            selectCount = 1,
        });
    }

    public override string EffectDescription =>
        $"방어도 {EffectiveShield(effects[0])}를 얻고\n버린 카드 1장을 덱으로 되돌립니다.";
}
