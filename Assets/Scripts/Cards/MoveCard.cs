using UnityEngine;
using UnityEngine.EventSystems;

public class MoveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MoveCard";
        Cost = 2;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.AnyTile;
        CardEffect cf = new CardEffect
        {
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            animTrigger = "Move",
            vfx = "Burst_Test", // 대상 위치 이펙트 — 임시 테스트 이펙트(아트 교체 전)
        };
        effects.Add(cf);
    }
    public override string EffectDescription => "이동합니다.";
}
