public class CleanseCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "CleanseCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        effects.Add(new CardEffect
        {
            // targeting + self: 드롭해 시전자가 확정되는 순간(Board.OnSelectBoard) 시전자에게 바로 실행된다.
            // command였을 땐 시전자만 정해지고 범위 칸을 한 번 더 클릭해야 실행됐다.
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Cleanse,
            dmg = 0,
            targetlogic = TargetLogic.self,
            cleanseBuffs = false,
        });
    }

    public override string EffectDescription => "자신에게 걸린 디버프를 모두 제거합니다.";
}
