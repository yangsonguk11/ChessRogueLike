using UnityEngine;

public partial class Board
{
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
                    if (GetButtonScript(pos).IsSelectable())
                    {
                        selectedButton = pos;
                        GetButtonScript(pos).SelectedTrue();
                    }
                }
                else if (selectedButton == pos || !selectedButtonMovable.Contains(pos))
                {
                    if (!IsLockedCasterActive())
                        ClearSelectedButton();
                }
                else
                {
                    if (GetButtonScript(selectedButton).GetPiece().GetComponent<Piece>().teamID == 1) { }
                    else if (selectedButtonMovable.Contains(pos))
                    {
                        ExecuteEffect(pendingEffects.Dequeue(), pos);
                        ScheduleNextCardEffect();
                    }
                }
                break;

            case BoardMode.targeting:
                if (selectedButton.x < 0 || selectedButton.y < 0)
                {
                    if (GetButtonScript(pos).IsSelectable())
                    {
                        selectedButton = pos;
                        GetButtonScript(pos).SelectedTrue();
                    }
                }
                else if (!selectedButtonMovable.Contains(pos))
                {
                    if (!IsLockedCasterActive())
                        ClearSelectedButton();
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
