using UnityEngine;

// 사용 불가 방해 카드: 효과가 없고 어떤 경우에도 쓸 수 없어 손패 자리만 차지한다(Card.IsUnplayable).
// 보상 풀·기본 덱에는 없고, 적이 전투 중 덱에 넣는 경로(예: Enemy-WoundKnight의 EnemyAddCardCard)로만 들어온다.
public class WoundCard : Card
{
    public override bool IsUnplayable => true;

    public override void Awake()
    {
        base.Awake();
        Name = "WoundCard";
        Cost = 0;
        type = CardType.Skill;
    }

    public override string EffectDescription => "사용 불가.";
}
