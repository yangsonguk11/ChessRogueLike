// 적 전용: 이동한 뒤 이동공격력을 5 올린다. EnemyMoveAndColDamageUpCard(+2)와 동일한 구조의 +5 버전.
// 텔레그래프형 적(White Knight 5)의 사이클 1단계 — 이후 차지(2단계)를 거쳐 3단계 광역 공격에 이 보너스가 실린다.
public class EnemyMoveAndColDamageUp5Card : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyMoveAndColDamageUp5Card";
        Cost = 0;
        type = CardType.Move;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = null,
            lockCasterForNext = true,
            animTrigger = "Move",
        });

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.ColDamageUp,
            dmg = 5,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => "이동한 후 이동공격력을 5 올립니다.";
}
