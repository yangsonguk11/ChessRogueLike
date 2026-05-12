using UnityEngine;

// 범위 회복 카드: 마우스를 올린 위치를 중심으로 범위 내 아군을 회복
// effectRange[0] = AoE 회복 범위 (필수)
// effectRange[1] = AoE 중심 배치 가능 사거리 (선택, 없으면 이동 범위 사용)
public class AreaHealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;

        RangeInfoSO targetRange = effectRange.Count > 1 ? effectRange[1] : null;
        bool useMovement = targetRange == null;

        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Heal,
            5,
            TargetLogic.AllAlliesInRange,
            effectRange[0],
            false,
            AreaTargetMode.MouseCentered,
            targetRange,
            useMovement
        );
        effects.Add(cf);
    }

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
