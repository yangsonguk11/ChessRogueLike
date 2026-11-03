using UnityEngine;

public class EmpowerAllyCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EmpowerAllyCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self; // 드롭 위치와 무관 — 놓은 뒤 보드에서 기물을 고른다

        effects.Add(new CardEffect
        {
            type = EffectType.BaseColDamageUp,
            dmg = 2,
            targetlogic = TargetLogic.self,
            pieceSelectCount = 1,
            excludeCasterFromPieceSelection = true, // 소환사 자신은 선택 대상에서 제외 — 소환한 기물 등 다른 아군만 강화
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription =>
        $"자신을 제외한 아군 기물 1체를 선택해 힘을 영구적으로 {effects[0].dmg} 올립니다.";
}
