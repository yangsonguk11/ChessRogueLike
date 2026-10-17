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
            // self가 아니어야 드롭한 칸이 대상이 된다 — targeting + self는 시전자를 확정하는 순간(Board.OnSelectBoard)
            // 시전자 칸에서 바로 실행돼 고른 아군 대신 자신이 회복됐다. 아군만 고르게 하는 건 dragDropTarget이 맡는다(VulnerableCard와 같은 방식).
            targetlogic = TargetLogic.NearestEnemy,
            noRangeLimit = true, // 보드 위 아군 누구에게든
            graveCost = 1,
            animTrigger = "Heal",
        });
    }

    public override string EffectDescription =>
        $"무덤을 {effects[0].graveCost} 소모해 아군 하나를 {effects[0].dmg} 회복합니다.";
}
