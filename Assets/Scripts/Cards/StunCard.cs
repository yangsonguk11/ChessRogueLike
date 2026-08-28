public class StunCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "StunCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyPiece;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.LowestHP,
            effectRange = effectRange[0],
            statusEffectType = StatusEffectType.Stun,
            statusDuration = 1,
            animTrigger = "ApplyStatus",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"대상에게 기절을 {effects[0].statusDuration}턴 부여합니다.";
}
