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
            type = EffectType.ColDamageUp,
            dmg = 3,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
        effects.Add(new CardEffect // 2번째: self 타겟 — 후속 효과라 시전자 재선택(OnSelectBoard)으로 자동 연쇄
        {
            type = EffectType.ReduceCost,
            dmg = 1,
            targetlogic = TargetLogic.self,
        });
    }

    // 코스트가 0이 되면(세 번째 사용 시점) 소멸 — FinishUseCard가 모든 효과 처리 후 호출되므로
    // 이 시점의 Cost는 이미 이번 사용의 ReduceCost가 반영된 값이다.
    public override bool ShouldExileOnUse() => exileOnUse || Cost <= 0; // 소멸 저주에 따른 소멸은 CardCanvas.FinishUseCard가 따로 더한다

    public override string EffectDescription => "힘을 3 올립니다. 코스트가 1 감소합니다. (코스트가 0이 되면 소멸)";
}
