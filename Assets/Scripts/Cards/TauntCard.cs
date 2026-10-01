// 자신에게 영구 도발을 건다. 기물 프리팹의 onSpawnCards에 연결해 보드에 배치될 때 발동시키는 용도(손패 카드로도 동작).
// 아군/적 어느 프리팹에 붙여도 그 기물에게 도발이 걸린다.
public class TauntCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "TauntCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.Taunt,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.OnTurnEnd 참고)
            animTrigger = "ApplyStatus",
        });
    }

    public override string EffectDescription => "자신에게 도발을 부여합니다. 상대는 사거리 안의 도발 기물을 먼저 노립니다.";
}
