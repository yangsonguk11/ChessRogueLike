using UnityEngine;

public class WarmUpDamageCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "WarmUpDamageCard";
        Cost = 3;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect // 1번째: self 타겟 — OnSelectBoard가 즉시 실행
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ColDamageUp,
            dmg = 3,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
        effects.Add(new CardEffect // 2번째: 위치 불필요 — Inspect라 자동 연쇄
        {
            requiredMode = Board.BoardMode.Inspect,
            type = EffectType.ReduceCost,
            dmg = 1,
        });
    }

    // 코스트가 0이 되면(세 번째 사용 시점) 소멸 — FinishUseCard가 모든 효과 처리 후 호출되므로
    // 이 시점의 Cost는 이미 이번 사용의 ReduceCost가 반영된 값이다.
    public override bool ShouldExileOnUse() => Cost <= 0;

    public override string EffectDescription => "이동공격력을 3 올립니다. 코스트가 1 감소합니다. (코스트가 0이 되면 소멸)";
}
