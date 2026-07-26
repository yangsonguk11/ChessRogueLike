using UnityEngine;

public class SquadTrainingCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SquadTrainingCard";
        Cost = 2;
        type = CardType.Skill;

        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.BaseColDamageUp, 1, TargetLogic.self)
        {
            pieceSelectCount = 2,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription =>
        $"아군 기물 {effects[0].pieceSelectCount}체를 선택해 이동공격력을 영구적으로 {effects[0].dmg} 올립니다.";
}
