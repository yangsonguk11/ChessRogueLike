public class ThornCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ThornCard";
        Cost = 1;
        type = CardType.Skill;
        effects.Add(new CardEffect(
            Board.BoardMode.command,
            EffectType.ApplyStatus,
            0,
            TargetLogic.self,
            _statusEffectType: StatusEffectType.Thorn,
            _statusDuration: 1,
            _statusPower: 3
        ));
    }

    public override string EffectDescription =>
        $"자신에게 가시({effects[0].statusPower}) 버프를 {effects[0].statusDuration}턴 부여합니다.\n이동공격을 받으면 공격자에게 {effects[0].statusPower}의 피해를 줍니다.";

    public override bool CanUse() => true;

    public override void Execute() { }
}
