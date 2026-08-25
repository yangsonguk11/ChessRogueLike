using UnityEngine;

public class DrawCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "DrawCard";
        Cost = 1;
        type = CardType.Skill;
        effects.Add(new CardEffect { requiredMode = Board.BoardMode.Inspect, type = EffectType.Draw, dmg = 1, targetlogic = TargetLogic.self, effectRange = null });
        effects.Add(new CardEffect { requiredMode = Board.BoardMode.Inspect, type = EffectType.Draw, dmg = 1, targetlogic = TargetLogic.self, effectRange = null });
    }

    public override string EffectDescription => $"카드를 {effects.Count}장 드로우합니다.";
}
