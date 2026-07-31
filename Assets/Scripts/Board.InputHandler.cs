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

    // targeting에서 시전자(selectedButton)를 쓰지 않고 targetPos만으로 동작하는 효과 타입 — 클릭한 기물 1개로 즉시 적용 가능
    // Damage는 AllEnemiesInRange/AllAlliesInRange/AllPiecesInRange(AoE, 캐스터 위치가 범위 기준점)를 제외하면 캐스터 없이도 가능하다.
    static bool IsSingleEntityEffect(CardEffect effect) =>
        effect.type is EffectType.DeBuff or EffectType.ApplyStatus or EffectType.ApplyTurnEffect or EffectType.ColDamageUp or EffectType.ShieldBonusUp
        || (effect.type == EffectType.Damage && effect.targetlogic != TargetLogic.AllEnemiesInRange
            && effect.targetlogic != TargetLogic.AllAlliesInRange && effect.targetlogic != TargetLogic.AllPiecesInRange);

    // effect가 캐스터(selectedButton) 선택 단계를 필요로 하는지. self 타겟이거나, 마우스 위치만으로
    // 발동하는 MouseCentered AoE가 아니면서 단일 대상 효과도 아니면 캐스터를 먼저 골라야 한다.
    // ButtonClicked의 targeting 분기와 ShowUseEligibilityPreview가 동일한 기준을 공유하기 위해 분리.
    static bool EffectNeedsCaster(CardEffect effect) =>
        effect == null || effect.targetlogic == TargetLogic.self
        || (effect.areaTargetMode != AreaTargetMode.MouseCentered && !IsSingleEntityEffect(effect));

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
            {
                CardEffect currentEffect = pendingEffects.Count > 0 ? pendingEffects.Peek() : null;

                // targeting은 기물(또는 위치) 1개만 지정하면 발동해야 함. self는 캐스터 선택 자체가 곧 대상 지정이라
                // 기존 OnSelectBoard 경로를 그대로 쓴다. 그 외에 시전자를 쓰지 않는 타입(DeBuff/ApplyStatus 등)이거나
                // 마우스로 위치만 지정하는 MouseCentered AoE는 캐스터 선택 없이 클릭 한 번으로 바로 적용한다.
                bool needsCaster = EffectNeedsCaster(currentEffect);

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
                        GetButtonScript(pos).SelectedTrue();
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
