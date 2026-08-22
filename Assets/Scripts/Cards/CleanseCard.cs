public class CleanseCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "CleanseCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        effects.Add(new CardEffect(
            Board.BoardMode.command,
            EffectType.Cleanse,
            0,
            TargetLogic.self
        )
        {
            cleanseBuffs = false
        });
    }

    public override string EffectDescription => "자신에게 걸린 디버프를 모두 제거합니다.";
}
