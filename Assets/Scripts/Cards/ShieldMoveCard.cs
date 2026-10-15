using UnityEngine;

// 방어도를 얻은 뒤 이동한다. 방어도는 self라 조준에서 빠지고 이동이 첫 조준 효과(Card.AimEffectIndex)가 되어,
// 잡으면 NowUsing 자리에서 이동 범위를 겨누다가 이동할 칸에 놓는 순간 방어도 → 그 칸으로 이동이 실행된다.
// 이동 불가 상태로 잡으면 이동 효과가 빠져(Card.PrepareUseEffects) 방어도만 얻는 Self 카드처럼 쓰인다.
public class ShieldMoveCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ShieldMoveCard";
        Cost = 2;
        type = CardType.Move;
        dragDropTarget = DragDropTarget.Self;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Shield,
            dmg = 3,
            targetlogic = TargetLogic.self,
            animTrigger = "Shield",
        });

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            animTrigger = "Move",
        });
    }

    public override string EffectDescription => $"방어도 {EffectiveShield(effects[0])}를 얻은 후 이동합니다.";
}
