// 적 전용(Boss-Berserker)의 패시브 — 프리팹 onSpawnCards에 연결해 스폰 시 자신에게 영구 버프 "상처 입히기"(WoundOnHitEffect)를 건다.
// 이 기물의 공격·이동공격 피해로 상대의 체력이 줄면 그 기물의 버린 카드 더미에 상처(WoundCard)가 2장 들어간다(Board.ApplyOnHitPassives).
// 버프라서 디스펠(DispelCard)로 지울 수 있다.
public class BossWoundOnHitCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "BossWoundOnHitCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ApplyStatus,
            dmg = 0,
            targetlogic = TargetLogic.self,
            statusEffectType = StatusEffectType.WoundOnHit,
            statusDuration = -1, // 음수 = 영구 지속 (StatusEffect.OnTurnEnd 참고)
            isBuff = true,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "자신에게 상처 입히기를 부여합니다. 피해를 준 적의 버린 카드 더미에 상처를 2장 넣습니다.";
}
