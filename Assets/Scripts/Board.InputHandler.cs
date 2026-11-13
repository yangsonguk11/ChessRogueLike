using UnityEngine;

public partial class Board
{
    public bool IsValidDragTarget(Vector2Int pos, DragDropTarget target)
    {
        if (pos.x < 0 || pos.y < 0) return false;
        Piece piece = GetButtonScript(pos).GetPieceScript();
        return target switch
        {
            DragDropTarget.Ally     => piece != null && piece.teamID == 0,
            DragDropTarget.Enemy    => piece != null && piece.teamID != 0,
            DragDropTarget.AnyPiece => piece != null,
            DragDropTarget.AnyTile  => true,
            DragDropTarget.Self     => true, // 위치와 무관하게 항상 카드 주인이 대상 — CardCanvas가 보통 이 검사 자체를 건너뜀
            _                       => false,
        };
    }

    public void ButtonClicked(Vector2Int pos)
    {
        if (!boardReady) return;
        if (!TurnManager.instance.IsPlayerActionable) return;

        // 기물 다중 선택 대기 중이면 아래 분기보다 우선 처리한다.
        if (PieceSelectionActive)
        {
            HandlePieceSelectionClick(pos);
            return;
        }

        if (currentActiveCard == null)
        {
            IdleClick(pos);
            return;
        }

        // 카드 선택 패널 확정을 기다리는 동안에는 보드 클릭을 받지 않는다.
        if (CardCanvas.cardSelectionMode || pendingEffects.Count == 0) return;
        TargetClick(pos);
    }

    // 카드를 쓰는 중이 아닐 때의 클릭: NPC 대화·상점 열기·기물 선택(아군이면 그 기물의 손패로 전환).
    void IdleClick(Vector2Int pos)
    {
        if (GetButtonScript(pos).GetPieceScript() is NPC npc)
        {
            DialogueSO npcDialogue = npc.Dialogue; // Dialogue는 읽을 때마다 클릭 횟수를 올리므로 한 번만 읽는다
            if (npcDialogue != null)
            {
                dialogueUI?.Show(npcDialogue);
                return;
            }
        }

        if (GetButtonScript(pos).GetPieceScript() is ShopObject)
        {
            ShopCanvas.instance?.Show();
            return;
        }

        if (selectedButton == pos)
        {
            ClearSelectedButton();
            return;
        }
        if (selectedButton.x >= 0 && selectedButton.y >= 0)
            ClearSelectedButton();

        if (GetButtonScript(pos).IsSelectable())
        {
            selectedButton = pos;
            GetButtonScript(pos).SelectedTrue();
        }

        // 아군 기물을 클릭하면 그 기물의 손패/덱으로 화면을 전환한다. 이 선택(selectedButton)이
        // 유지되는 동안은 Board.HoverRange.cs의 ButtonUnhovered가 hover 종료 시 이 기물로 되돌린다.
        Piece clickedAlly = GetButtonScript(pos).GetPieceScript();
        if (clickedAlly != null && clickedAlly.teamID == 0)
            CardCanvas.instance.SetActivePiece(clickedAlly);
    }

    // 카드 효과가 대상 칸을 기다리는 동안의 클릭: 칸 1개만 지정하면 발동한다.
    void TargetClick(Vector2Int pos)
    {
        // self는 캐스터 선택 자체가 곧 대상 지정이라 OnSelectBoard 경로를 쓴다. 캐스터는 드롭 시점에 ConfirmCasterOnDrop이
        // 항상 먼저 확정해두므로(AutoSelectCardOwnerAsCaster), 보통은 여기 도달할 때 이미 selectedButton이 활성 상태다 —
        // 아래는 그게 아직 안 된 경우(예: ActivePiece를 못 찾은 예외 상황)의 폴백으로, 자기 기물을 직접 클릭해 캐스터를 확정하게 한다.
        if (selectedButton.x < 0 || selectedButton.y < 0)
        {
            Piece clickedPiece = GetButtonScript(pos).GetPieceScript();
            if (clickedPiece != null && clickedPiece.teamID == 0)
            {
                selectedButton = pos;
                // self 타겟 효과면 위 대입이 OnSelectBoard의 즉시 실행(및 카드 사용 종료)까지 그 자리에서
                // 끝내버려 selectedButton이 이미 (-1,-1)로 초기화됐을 수 있다 — 그 상태에서 여기서 다시
                // 켜면 아무도 끄지 않는 표시가 남는다 (AutoSelectCardOwnerAsCaster와 동일한 가드).
                if (isSelectedButtonActive())
                    GetButtonScript(selectedButton).SelectedTrue();
            }
        }
        else if (!selectedButtonMovable.Contains(pos))
        {
            // 유효하지 않은 범위 클릭 — 기물 선택 유지
            AnnouncementUI.instance?.Show("사거리 밖입니다.");
        }
        else if (IsTauntBlocked(pendingEffects.Peek(), pendingEffects.Peek().caster, pos, selectedButtonMovable))
        {
            // 사거리 안에 도발 기물이 있으면 공격/이동 효과는 그 기물만 대상으로 고를 수 있다 — 기물 선택 유지
            AnnouncementUI.instance?.Show("도발을 가진 기물이 있습니다.");
        }
        else
        {
            // 사거리 안이면 빈 칸이어도 실행한다(이동·소환·범위 중심). 기물만 받는 효과는 첫 조준이면 드롭 대상
            // 검사(dragDropTarget)가 이미 걸렀고, 후속 효과는 빈 칸 클릭도 허용해야 대상이 없을 때 카드가 멈추지 않는다.
            ExecuteEffect(pendingEffects.Dequeue(), pos);
            ScheduleNextCardEffect();
        }
    }
}
