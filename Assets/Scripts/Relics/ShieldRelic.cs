// 전투 시작 시 방어막 3을 부여하는 유물.
public class ShieldRelic : Relic
{
    public ShieldRelic()
    {
        Name = "철갑 부적";
        Description = "전투 시작 시 방어막 3을 얻습니다.";
        Timing = RelicTiming.CombatStart;
        Effects.Add(new CardEffect(Board.BoardMode.Inspect, EffectType.Shield, 3, TargetLogic.self));
    }
}
