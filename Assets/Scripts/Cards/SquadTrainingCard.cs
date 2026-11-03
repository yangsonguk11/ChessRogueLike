using UnityEngine;

public class SquadTrainingCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SquadTrainingCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self; // 드롭 위치와 무관 — 놓은 뒤 보드에서 기물을 고른다

        effects.Add(new CardEffect
        {
            type = EffectType.BaseColDamageUp,
            dmg = 1,
            targetlogic = TargetLogic.self,
            pieceSelectCount = 2,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription =>
        $"아군 기물 {effects[0].pieceSelectCount}체를 선택해 힘을 영구적으로 {effects[0].dmg} 올립니다.";
}
