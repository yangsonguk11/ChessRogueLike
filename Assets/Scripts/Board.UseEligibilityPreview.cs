using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // 카드를 손에 들어 usecardzone에 들어온 시점(Board.UseCard)에 이 카드를 쓸 수 있는 칸을 미리 보여주는 하이라이트.
    // pieceSelectCount를 쓰는 카드는 RequestPieceSelection이 이미 자체적으로 하이라이트를 처리하므로 건드리지 않는다.
    List<(Vector2Int pos, int teamID)> useEligibilityHighlights = new List<(Vector2Int, int)>();

    // true면 지금 든 카드가 픽업 시점에 ShowCasterEffectRange로 실제 사거리(selectedButtonMovable)를
    // 미리 계산해둔 상태 — 드롭 시 IsValidDropPos가 이 사거리를 기준으로 검증해야 한다는 뜻.
    bool pendingUseHasRangeLimit;

    // 카드를 들고만 있는 동안(효과 큐는 아직 비어 있음) 겨누는 조준 효과 — 호버 범위 미리보기가 큐 대신 이걸 본다.
    CardEffect previewAimEffect;

    // 지금 겨누는 효과: 효과 처리 중이면 큐 맨 앞, 카드를 들고만 있으면 미리보기 중인 조준 효과.
    CardEffect CurrentAimEffect() => pendingEffects.Count > 0 ? pendingEffects.Peek() : previewAimEffect;

    // 카드를 집었을 때(CardCanvas.CommitNowUsingCard)와 사용 확정 때(UseCard) 호출. 범위는 이번 사용 효과 중
    // 처음 나오는 조준 효과(Card.AimEffectIndex, 없으면 첫 효과) 기준이다.
    public void ShowUseEligibilityPreview(Card card)
    {
        ClearSelectedButton();
        pendingUseHasRangeLimit = false;
        List<CardEffect> effects = card.UseEffects;
        if (effects.Count == 0) return;
        int aimIndex = card.AimEffectIndex;
        previewAimEffect = aimIndex >= 0 ? effects[aimIndex] : null;
        CardEffect first = effects[Mathf.Max(0, aimIndex)];
        if (first.pieceSelectCount > 0) return;

        // BoardMode.command는 ButtonClicked에서 항상 캐스터 선택을 먼저 요구하므로 first.noRangeLimit와 무관하게 캐스터형으로 취급.
        bool needsCaster = first.requiredMode == BoardMode.command || !first.noRangeLimit;

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
                {
                    ShowCasterEffectRange(ownerButton.GetLocation(), first);
                    pendingUseHasRangeLimit = true;
                }
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
        RefreshRangeDisplay();
    }

    // 드래그로 카드를 놓은 칸이 실제로 유효한지 검사. pendingUseHasRangeLimit가 false면(사거리 제한이 없거나
    // 애초에 계산하지 않은 카드) 어디든 유효 — 그 외엔 픽업 시점에 계산해둔 selectedButtonMovable 안인지 확인한다.
    public bool IsValidDropPos(Vector2Int pos) => !pendingUseHasRangeLimit || selectedButtonMovable.Contains(pos);

    void ClearUseEligibilityPreview()
    {
        previewAimEffect = null;
        useEligibilityHighlights.Clear();
        RefreshRangeDisplay();
    }
}
