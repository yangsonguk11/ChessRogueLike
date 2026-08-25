using UnityEngine;

// 범위 방어 카드: 마우스를 올린 위치를 중심으로 범위 내 모든 기물(아군/적 무관)에게 방어도 부여
// effectRange[0] = AoE 방어 범위 (필수)
// effectRange[1] = AoE 중심 배치 가능 사거리 (선택, 없으면 이동 범위 사용)
public class ZoneShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ZoneShieldCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyTile; // 마우스로 지정한 칸을 중심으로 즉시 발동 (캐스터 선택 불필요)

        RangeInfoSO targetRange = effectRange.Count > 1 ? effectRange[1] : null;
        bool useMovement = targetRange == null;

        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 3,
            targetlogic = TargetLogic.AllPiecesInRange,
            effectRange = effectRange[0],
            lockCasterForNext = false,
            areaTargetMode = AreaTargetMode.MouseCentered,
            targetingRange = targetRange,
            targetingUsesMovement = useMovement,
            animTrigger = "Shield",
            hasCaster = false,
        };
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 기물에게 방어도 {EffectiveShield(effects[0])}를 부여합니다.";
}
