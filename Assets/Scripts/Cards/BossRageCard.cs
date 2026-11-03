// 적 전용(데미지 강요 보스)의 패시브 — 프리팹 onSpawnCards에 연결해 스폰 시 자신에게 영구 턴 효과를 건다:
// 자기 턴이 끝날 때마다 힘 +amount. 버프라서 디스펠(DispelCard)로 지울 수 있다.
public class BossRageCard : Card
{
    public int amount = 2;

    public override void Awake()
    {
        base.Awake();
        Name = "BossRageCard";
        Cost = 0;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            type = EffectType.ApplyStatus,
            statusEffectType = StatusEffectType.TurnEffect,
            targetlogic = TargetLogic.self,
            turnPhase = TurnPhase.OwnTurnEnd,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.TickDuration 참고)
            onTurnEffect = new CardEffect
            {
                type = EffectType.ColDamageUp,
                dmg = amount,
                targetlogic = TargetLogic.self,
                isBuff = true,
                animTrigger = "Buff",
            },
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => $"자기 턴이 끝날 때마다 힘이 {amount} 오릅니다.";
}
