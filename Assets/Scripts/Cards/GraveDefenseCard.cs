// 가진 무덤을 전부 소모해, 소모한 무덤 1당 방어도 4를 얻는다 — 무덤이 없으면 첫 효과 비용 부족으로 사용 불가.
public class GraveDefenseCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "GraveDefenseCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 0,
            targetlogic = TargetLogic.self,
            graveCost = 1,
            consumeAllGrave = true,
            dmgPerGrave = 4, // 방어도 = 소모한 무덤 × 4 (+ 방어막 보너스)
            animTrigger = "Shield",
        });
    }

    public override string EffectDescription =>
        $"무덤을 모두 소모하고, 소모한 만큼 방어도를 {effects[0].dmgPerGrave} 얻습니다.";
}
