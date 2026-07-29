using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // 카드를 손에 들어 usecardzone에 들어온 시점(Board.UseCard)에 이 카드를 쓸 수 있는 칸을 미리 보여주는 하이라이트.
    // pieceSelectCount를 쓰는 카드는 RequestPieceSelection이 이미 자체적으로 하이라이트를 처리하므로 건드리지 않는다.
    List<(Vector2 pos, int teamID)> useEligibilityHighlights = new List<(Vector2, int)>();

    void ShowUseEligibilityPreview(Card card)
    {
        if (card.effects.Count == 0) return;
        CardEffect first = card.effects[0];
        if (first.pieceSelectCount > 0) return;

        // BoardMode.command는 ButtonClicked에서 항상 캐스터 선택을 먼저 요구하므로 EffectNeedsCaster와 무관하게 캐스터형으로 취급.
        bool needsCaster = first.requiredMode == BoardMode.command || EffectNeedsCaster(first);

        PieceSelectFilter filter;
        if (needsCaster)
        {
            // 캐스터 선택형 카드: 캐스터로 고를 수 있는 내 기물 전체
            filter = PieceSelectFilters.Team(0);
        }
        else
        {
            // 드래그로 바로 타겟에 꽂는 카드: dragDropTarget이 허용하는 대상
            switch (card.dragDropTarget)
            {
                case DragDropTarget.Ally: filter = PieceSelectFilters.Team(0); break;
                case DragDropTarget.Enemy: filter = (pos, piece) => piece.teamID != 0; break;
                case DragDropTarget.AnyPiece: filter = (pos, piece) => true; break;
                default: return; // AnyTile: 보여줄 의미 있는 하이라이트가 없음
            }
        }

        useEligibilityHighlights = HighlightMatchingPieces(new[] { filter });
    }

    void ClearUseEligibilityPreview()
    {
        foreach (var (pos, teamID) in useEligibilityHighlights)
            GetButtonScript(pos).RangeOff(teamID);
        useEligibilityHighlights.Clear();
    }
}
