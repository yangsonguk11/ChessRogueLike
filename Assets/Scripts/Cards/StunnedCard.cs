// 기절한 적의 턴을 대신하는 카드. 기존 적 프리팹을 하나도 안 건드리기 위해 프리팹에 미리 두지 않고
// Enemy.Awake()가 동적으로(AddComponent) 붙인다. 효과는 EffectType.Stun 하나뿐인 무효과 마커.
public class StunnedCard : Card
{
    // 플레이어가 쓰는 카드가 아니라 코스트가 의미 없다 — WoundCard처럼 카드 면에 코스트 숫자를 숨긴다.
    // AI가 실제로 실행하는 카드라 IsUnplayable은 쓰지 않는다.
    public override bool HidesCost => true;

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

    public override string EffectDescription => "기절";
}
