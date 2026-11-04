using UnityEngine;

// 적 전용: pieceToSummon 기물을 두 번 소환한다(소환 CardEffect 2개). Enemy-MobBalloon이 쓴다.
// 소환 위치는 SummonCard와 같은 원래 로직이다 — targetlogic 기본값(NearestEnemy)으로 effectRange[0](시전자 기준) 안의 가장 가까운
// 상대 칸을, 없으면 범위 안 빈 칸 중 가장 가까운 상대와 가장 가까운 칸을 대상으로 잡고, Board.SummonPieceAt이 그 칸부터 빈 칸을 찾는다.
// 두 효과는 차례로 실행되므로 두 번째는 첫 소환으로 칸이 찬 보드에서 다시 대상을 골라 그다음으로 가까운 칸에 나온다.
public class EnemyDoubleSummonCard : Card
{
    [SerializeField] PieceInfo pieceToSummon;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyDoubleSummonCard";
        Cost = 0;
        type = CardType.Skill;

        RangeInfoSO range = effectRange.Count > 0 ? effectRange[0] : null;
        effects.Add(new CardEffect
        {
            type = EffectType.Summon,
            effectRange = range,
            summonPieceInfo = pieceToSummon,
            animTrigger = "Summon", // Summon State가 없는 기물은 Board.PlayTriggerAnimCor가 Attack으로 대체
        });
        effects.Add(new CardEffect
        {
            type = EffectType.Summon,
            effectRange = range,
            summonPieceInfo = pieceToSummon,
            animTrigger = "Summon",
        });
    }

    public override string EffectDescription => $"{pieceToSummon?.PieceName}을(를) 2개 소환합니다.";
}
