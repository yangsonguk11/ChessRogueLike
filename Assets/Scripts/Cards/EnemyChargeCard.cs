// 적 전용: 아무 효과 없이 한 턴을 흘려보내며 "차지 중" 아이콘만 표시한다.
// 텔레그래프형 적(Enemy-ChargeKnight)의 사이클 2단계 — 다음 턴에 강화된 광역 공격이 온다는 걸 예고한다.
public class EnemyChargeCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyChargeCard";
        Cost = 0;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        // 인스펙터에 빈 RangeInfoSO(EmptyRangeInfo)를 넣어 예고에 범위가 뜨지 않게 한다(자기 대상 효과는 effectRange를
        // 비워 둬도 AutoPiece.GetMoveableButton이 범위를 표시하지 않는다).
        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        effects.Add(new CardEffect
        {
            type = EffectType.Charge,
            dmg = 0,
            targetlogic = TargetLogic.self,
            effectRange = range,
        });
    }

    public override string EffectDescription => "다음 턴에 강력한 공격을 준비합니다.";
}
