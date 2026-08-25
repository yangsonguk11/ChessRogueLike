using UnityEngine;

public class EnemyMoveAndColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyMoveAndColDamageUpCard";
        Cost = 2;
        type = CardType.Move;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = null,
            lockCasterForNext = true,
            animTrigger = "Move",
        });

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ColDamageUp,
            dmg = 2,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "이동한 후 이동공격력을 2 올립니다.";
}
