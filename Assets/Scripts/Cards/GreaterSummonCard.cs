using UnityEngine;

public class GreaterSummonCard : Card
{
    [SerializeField] PieceInfo pieceToSummon;

    public override void Awake()
    {
        base.Awake();
        Name = "GreaterSummonCard";
        Cost = 4;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyTile; // SummonCard와 동일 — Self로 두면 NeedsTargeting()이 false가 되어 화살표/드롭 실행이 막힘
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
    public override string EffectDescription => $"강력한 {pieceToSummon?.PieceName}을(를) 소환합니다.";
}
