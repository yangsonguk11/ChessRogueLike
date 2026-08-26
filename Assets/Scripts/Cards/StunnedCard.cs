// 기절한 적의 턴을 대신하는 카드. 기존 적 프리팹을 하나도 안 건드리기 위해 프리팹에 미리 두지 않고
// Enemy.Awake()가 동적으로(AddComponent) 붙인다. 효과는 EffectType.Stun 하나뿐인 무효과 마커.
public class StunnedCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "StunnedCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Stun,
            dmg = 0,
            targetlogic = TargetLogic.self,
            effectRange = RangeInfoSODatabase.instance?.GetRangeInfoSO("EmptyRangeInfo"),
        });
    }

    public override string EffectDescription => "기절 상태라 행동할 수 없습니다.";
}
