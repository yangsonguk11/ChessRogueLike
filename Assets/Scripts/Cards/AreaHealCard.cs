using UnityEngine;

// 범위 회복 카드: 마우스를 올린 위치를 중심으로 범위 내 아군을 회복
// Inspector에서 effectRange[0]에 회복 범위 RangeInfoSO를 할당해야 함
public class AreaHealCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Cost = 2;

        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Heal,
            5,
            TargetLogic.AllAlliesInRange,
            effectRange[0],
            false,
            AreaTargetMode.MouseCentered
        );
        effects.Add(cf);
    }

    public override bool CanUse()
    {
        throw new System.NotImplementedException();
    }

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
