public class VampiricFangRelic : Relic
{
    public VampiricFangRelic()
    {
        Name = "흡혈의 송곳니";
        Description = "적을 처치하면 처치한 기물이 체력을 2 회복합니다.";
        Timing = RelicTiming.OnKill;
        Effects.Add(new CardEffect { requiredMode = Board.BoardMode.Inspect, type = EffectType.Heal, dmg = 2, targetlogic = TargetLogic.self });
    }
}
