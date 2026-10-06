// MagicMissileCard를 손에 2장 넣는다 — 전투 중에만 쓰이고 덱 저장에는 남지 않는다(FetchAttackCard와 같은 AddCard 방식).
public class LoadMagicMissileCard : Card
{
    const string MissileCardID = "MagicMissileCard";

    public override void Awake()
    {
        base.Awake();
        Name = "LoadMagicMissileCard";
        Cost = 0;
        type = CardType.Skill;
        for (int i = 0; i < 2; i++)
        {
            effects.Add(new CardEffect
            {
                requiredMode = Board.BoardMode.Inspect,
                type = EffectType.AddCard,
                dmg = 0,
                targetlogic = TargetLogic.self,
                addCardID = MissileCardID,
                addCardZone = CardPositionZone.Hand,
            });
        }
    }

    public override string EffectDescription => $"{MissileCardID} 카드를 {effects.Count}장 손에 가져옵니다.";
}
