using UnityEngine;

public class AreaShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "AreaShieldCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyTile; // 범위 방어: 특정 기물이 아니라 발동 기준점(칸)을 지정
        CardEffect cf = new CardEffect(Board.BoardMode.command, EffectType.Shield, 3, TargetLogic.AllAlliesInRange, effectRange[0])
            { animTrigger = "Shield" };
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 아군에게 방어도 {EffectiveShield(effects[0])}를 부여합니다.";
}
