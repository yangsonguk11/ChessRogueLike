public class DispelCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DispelCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;
        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Cleanse,
            0,
            TargetLogic.NearestEnemy
        )
        {
            cleanseBuffs = true,
            hasCaster = false
        });
    }

    public override string EffectDescription => "적을 선택해 걸려 있는 버프를 모두 제거합니다.";
}
