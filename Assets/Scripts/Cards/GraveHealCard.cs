using UnityEngine;

// 무덤 1을 소모해 드롭한 아군 기물 하나를 회복 — 무덤이 없으면 첫 효과 비용 부족으로 사용 불가
public class GraveHealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "GraveHealCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Ally;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Heal,
            dmg = 3,
            targetlogic = TargetLogic.self,
            noRangeLimit = true, // 보드 위 아군 누구에게든
            graveCost = 1,
            animTrigger = "Heal",
        });
    }

    public override string EffectDescription =>
        $"무덤을 {effects[0].graveCost} 소모해 아군 하나를 {effects[0].dmg} 회복합니다.";
}
