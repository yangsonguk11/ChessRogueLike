using UnityEngine;

public class SacrificeShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SacrificeShieldCard";
        Description = "손의 카드 1장을 버리고 방어도를 얻는다.";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        // 효과 1: 자신에게 방어도 부여 (DefenseCard와 동일 패턴)
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 6,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
        });
        // 효과 2: 손패에서 카드 1장 선택해서 버리기
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.cardSelecting,
            type = EffectType.SelectAndDiscard,
            dmg = 0,
            targetlogic = TargetLogic.self,
            cardZone = CardZone.Hand,
            selectCount = 1,
        });

    }

    public override string EffectDescription =>
        $"손의 카드 1장을 버린 후\n방어도 {EffectiveShield(effects[0])}를 얻습니다.";

}
