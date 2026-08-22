using UnityEngine;

// 범위 공격 카드: 마우스를 올린 위치를 중심으로 범위 내 모든 기물(아군/적 무관)에게 피해
// effectRange[0] = AoE 피해 범위 (필수)
// effectRange[1] = AoE 중심 배치 가능 사거리 (선택, 없으면 이동 범위 사용)
public class ZoneAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ZoneAttackCard";
        Cost = 2;
        type = CardType.Attack;
        dragDropTarget = DragDropTarget.AnyTile; // 마우스로 지정한 칸을 중심으로 즉시 발동 (캐스터 선택 불필요)

        RangeInfoSO targetRange = effectRange.Count > 1 ? effectRange[1] : null;
        bool useMovement = targetRange == null;

        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            2,
            TargetLogic.AllPiecesInRange,
            effectRange[0],
            false,
            AreaTargetMode.MouseCentered,
            targetRange,
            useMovement
        )
        { animTrigger = "AreaAttack", hasCaster = false };
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 기물에게 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
