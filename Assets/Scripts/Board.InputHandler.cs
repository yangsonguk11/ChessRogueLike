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
            _                       => false,
        };
    }

    public void ButtonClicked(Vector2 pos)
    {
        if (!boardReady) return;
        if (TurnManager.instance.currentState != TurnState.Player) return;

        switch (boardmode)
        {
            case BoardMode.Inspect:
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
                else
                {
                    CardEffect currentEffect = pendingEffects.Count > 0 ? pendingEffects.Peek() : null;
                    bool isAoE = currentEffect != null &&
                        (currentEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
                         currentEffect.targetlogic == TargetLogic.AllAlliesInRange);
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
