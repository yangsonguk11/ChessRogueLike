using UnityEngine;

public class AreaShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "AreaShieldCard";
        Cost = 1;
        type = CardType.Skill;
        CardEffect cf = new CardEffect(Board.BoardMode.targeting, EffectType.Shield, 3, TargetLogic.AllAlliesInRange, effectRange[0]);
        effects.Add(cf);
    }

    public override string EffectDescription => $"범위 내 모든 아군에게 방어도 {effects[0].dmg}를 부여합니다.";

    public override bool CanUse() => true;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
