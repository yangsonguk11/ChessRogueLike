using UnityEngine;

public class SacrificeShieldCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "SacrificeShieldCard";
        Description = "손의 카드 1장을 버린다.\n방어도 6을 얻는다.";
        Cost = 1;
        type = CardType.Skill;

        // 효과 1: 자신에게 방어도 부여 (DefenseCard와 동일 패턴)
        effects.Add(new CardEffect(Board.BoardMode.targeting, EffectType.Shield, 6, TargetLogic.self));
        // 효과 2: 손패에서 카드 1장 선택해서 버리기
        effects.Add(new CardEffect(Board.BoardMode.cardSelecting, EffectType.SelectAndDiscard, 0, TargetLogic.self)
        {
            cardZone = CardZone.Hand,
            selectCount = 1
        });

    }

    public override string EffectDescription =>
        $"손의 카드 1장을 버린 후\n방어도 {effects[1].dmg}를 얻습니다.";

    // 버릴 카드가 없으면 사용 불가 (이 카드 자신은 이미 손패 목록에 포함된 상태로 체크됨)
    public override bool CanUse() => CardCanvas.instance.cards.Count >= 2;

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
}
