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

        if (needsCaster)
        {
            // self 타겟 효과는 대상이 항상 캐스터 자신이라 보여줄 의미 있는 범위가 없다.
            // 그 외(Move/Attack 등)는 카드를 낸 기물 기준 실제 이동/공격 가능 칸을 미리 보여준다.
            // ShowCasterEffectRange는 selectedButton을 건드리지 않으므로 캐스터가 "확정"되지 않고
            // (self 즉시실행도 트리거되지 않음), 드롭 시점에 Board.ConfirmCasterOnDrop이 별도로 확정한다.
            if (first.targetlogic != TargetLogic.self)
            {
                Button ownerButton = GetButtonForPiece(CardCanvas.instance?.ActivePiece);
                if (ownerButton != null)
                    ShowCasterEffectRange(ownerButton.GetLocation(), first);
            }
            return;
        }

        PieceSelectFilter filter;
        switch (card.dragDropTarget)
        {
            case DragDropTarget.Ally: filter = PieceSelectFilters.Team(0); break;
            case DragDropTarget.Enemy: filter = (pos, piece) => piece.teamID != 0; break;
            case DragDropTarget.AnyPiece: filter = (pos, piece) => true; break;
            default: return; // AnyTile: 보여줄 의미 있는 하이라이트가 없음
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
