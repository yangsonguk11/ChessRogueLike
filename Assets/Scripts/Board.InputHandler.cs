using UnityEngine;

public partial class Board
{
    public bool IsValidDragTarget(Vector2 pos, DragDropTarget target)
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

    public void ButtonClicked(Vector2 pos)
    {
        if (!boardReady) return;
        if (TurnManager.instance.currentState != TurnState.Player) return;

        // 기물 다중 선택 대기 중이면 boardmode(targeting)의 기존 분기보다 우선 처리한다.
        if (PieceSelectionActive)
        {
            HandlePieceSelectionClick(pos);
            return;
        }

        switch (boardmode)
        {
            case BoardMode.Inspect:
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
                break;

            case BoardMode.command:
                if (selectedButton.x < 0 || selectedButton.y < 0)
                {
                    Piece clickedPiece = GetButtonScript(pos).GetPieceScript();
                    if (clickedPiece != null && clickedPiece.teamID == 0)
                    {
                        selectedButton = pos;
                        GetButtonScript(pos).SelectedTrue();
                    }
                }
                else if (!selectedButtonMovable.Contains(pos))
                {
                    // 유효하지 않은 범위 클릭 — 기물 선택 유지
                }
                else if (selectedButtonMovable.Contains(pos))
                {
                    ExecuteEffect(pendingEffects.Dequeue(), pos);
                    ScheduleNextCardEffect();
                }
                break;

            case BoardMode.targeting:
            {
                CardEffect currentEffect = pendingEffects.Count > 0 ? pendingEffects.Peek() : null;

                // targeting은 기물(또는 위치) 1개만 지정하면 발동해야 함. self는 캐스터 선택 자체가 곧 대상 지정이라
                // 기존 OnSelectBoard 경로를 그대로 쓴다. 그 외에 CardEffect.hasCaster가 false인 카드
                // (DeBuff/ApplyStatus 등 단일 대상 효과, 마우스로 위치만 지정하는 MouseCentered AoE 등)는
                // 캐스터 선택 없이 클릭 한 번으로 바로 적용한다.
                bool needsCaster = currentEffect == null || currentEffect.hasCaster;

                if (selectedButton.x < 0 || selectedButton.y < 0)
                {
                    if (!needsCaster)
                    {
                        if (IsValidDragTarget(pos, currentActiveCard.dragDropTarget))
                        {
                            selectedButton = pos;
                            ExecuteEffect(pendingEffects.Dequeue(), pos);
                            ScheduleNextCardEffect();
                        }
                        break;
                    }

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
                }
                else
                {
                    bool isAoE = currentEffect != null &&
                        (currentEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
                         currentEffect.targetlogic == TargetLogic.AllAlliesInRange ||
                         currentEffect.targetlogic == TargetLogic.AllPiecesInRange);
                    bool isMouseAoE = currentEffect != null &&
                        currentEffect.areaTargetMode != AreaTargetMode.Fixed;

                    if (isAoE || isMouseAoE || GetButtonScript(pos).GetPiece() != null)
                    {
                        ExecuteEffect(pendingEffects.Dequeue(), pos);
                        ScheduleNextCardEffect();
                    }
                }
                break;
            }
        }
    }
}
