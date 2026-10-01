using UnityEngine;

// 도발 기물(tauntAutoAlly)을 소환하고 소멸한다. 도발은 소환된 기물 프리팹의 onSpawnCards(TauntCard)가 스폰 시 건다.
public class TauntSummonCard : Card
{
    [SerializeField] PieceInfo pieceToSummon;

    public override void Awake()
    {
        base.Awake();
        Name = "TauntSummonCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyTile; // SummonCard와 동일 — Self로 두면 NeedsTargeting()이 false가 되어 화살표/드롭 실행이 막힘
        exileOnUse = true;
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Summon,
            effectRange = effectRange[0],
            summonPieceInfo = pieceToSummon,
            animTrigger = "Attack",
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"도발 상태의 {pieceToSummon?.PieceName}을(를) 소환합니다. (소멸)";
}
