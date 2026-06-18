using UnityEngine;

public class EnemyMoveAndColDamageUpCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyMoveAndColDamageUpCard";
        Cost = 2;
        type = CardType.Move;

        effects.Add(new CardEffect(
            Board.BoardMode.command,
            EffectType.Move,
            0,
            TargetLogic.NearestEnemy,
            null,
            true
        )
        { animTrigger = "Move" });

        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ColDamageUp,
            2,
            TargetLogic.self
        )
        { animTrigger = "Buff" });
    }

    public override string EffectDescription => "이동한 후 이동공격력을 2 올립니다.";
}
