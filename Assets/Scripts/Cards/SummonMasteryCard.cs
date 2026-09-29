using UnityEngine;

public class SummonMasteryCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SummonMasteryCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect // 1번째: self 타겟 — OnSelectBoard가 즉시 실행
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.GrantSummonColDamage,
            dmg = 2,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
        effects.Add(new CardEffect // 2번째: 위치 불필요 — Inspect라 자동 연쇄
        {
            requiredMode = Board.BoardMode.Inspect,
            type = EffectType.GrantSummonMaxHp,
            dmg = 2,
        });
    }

    public override string EffectDescription => "다음에 소환할 기물의 ColDamage, 체력을 2씩 올립니다. (소환 시 소모됨)";
}
