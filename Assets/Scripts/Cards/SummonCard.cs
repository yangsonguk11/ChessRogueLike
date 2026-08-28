using UnityEngine;

public class SummonCard : Card
{
    [SerializeField] PieceInfo pieceToSummon;

    public override void Awake()
    {
        base.Awake();
        Name = "SummonCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyTile; // Self로 두면 NeedsTargeting()이 false가 되어 화살표/드롭 실행이 막힘 — MoveCard와 동일하게 AnyTile
        CardEffect cf = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Summon,
            effectRange = effectRange[0],
            summonPieceInfo = pieceToSummon,
        };
        effects.Add(cf);
    }
    public override string EffectDescription => $"{pieceToSummon?.PieceName}을(를) 소환합니다.";
}
