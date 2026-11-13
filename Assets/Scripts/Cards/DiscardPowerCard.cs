using UnityEngine;

// 사용 불가 카드(Card.IsUnplayable)지만, 카드 효과로 버려지면 주인 기물의 힘이 오른다(Card.onDiscardEffects).
// 턴 종료에 손에 남아 버려질 때는 발동하지 않는다.
public class DiscardPowerCard : Card
{
    public override bool IsUnplayable => true;

    public override void Awake()
    {
        base.Awake();
        Name = "DiscardPowerCard";
        Cost = 0;
        type = CardType.Skill;

        onDiscardEffects.Add(new CardEffect // 전투 한정 힘 상승(영구 강화인 BaseColDamageUp 아님)
        {
            type = EffectType.ColDamageUp,
            dmg = 1,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription =>
        $"사용 불가.\n버려졌을 때: 힘을 {onDiscardEffects[0].dmg} 올립니다.";
}
