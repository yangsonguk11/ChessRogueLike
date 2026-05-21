using UnityEngine;

public class DrawCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DrawCard";
        Cost = 1;
        type = CardType.Skill;
        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.Draw, 1, TargetLogic.self, null));
        effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.Draw, 1, TargetLogic.self, null));
    }

    public override string EffectDescription => $"카드를 {effects.Count}장 드로우합니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
