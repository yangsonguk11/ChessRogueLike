using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2Int> hoverRangeButtons = new List<Vector2Int>();
    List<Vector2Int> hoverPieceRangeButtons = new List<Vector2Int>();
    bool hoverPieceIsAlly = true;
    bool allEnemyRangeSuppressed = false;
    Vector2Int currentHoverDirection = Vector2Int.up;

    public void ButtonHovered(Vector2Int pos)
    {
        // AoE 카드 범위 미리보기. MouseCentered는 캐스터를 선택하지 않고 targeting 클릭 한 번으로 바로 발동하므로
        // 캐스터 선택 전에도 마우스 위치 기준으로 미리보기를 보여줘야 함.
        // Directional4/8도 마찬가지로, 캐스터 확정(드롭)은 아직 안 됐어도 카드를 든 채로 보드를 훑는 동안
        // 방향 미리보기가 나와야 하므로, 카드 소유 기물(activePiece) 위치를 캐스터 삼아 미리 보여준다.
        CardEffect pendingEffect = pendingEffects.Count > 0 ? pendingEffects.Peek() : null;
        bool previewableWithoutCaster = pendingEffect != null &&
            (pendingEffect.areaTargetMode == AreaTargetMode.MouseCentered ||
             pendingEffect.areaTargetMode == AreaTargetMode.Directional4 ||
             pendingEffect.areaTargetMode == AreaTargetMode.Directional8);
        if (pendingEffects.Count > 0 && (isSelectedButtonActive() || previewableWithoutCaster))
        {
            CardEffect effect = pendingEffects.Peek();
            if (effect.areaTargetMode != AreaTargetMode.Fixed)
            {
                ClearHoverRange();

                List<Vector2Int> offsets = effect.effectRange?.GetAbleRange();
                if (offsets != null)
                {
                    Vector2Int center;
                    if (effect.areaTargetMode == AreaTargetMode.MouseCentered)
                    {
                        center = pos;
                    }
                    else
                    {
                        Button ownerButton = GetButtonForPiece(CardCanvas.instance?.ActivePiece);
                        Vector2Int casterPos = isSelectedButtonActive() ? selectedButton
                            : (ownerButton != null ? ownerButton.GetLocation() : pos);
                        center = casterPos;
                        Vector2Int dir = GetSnappedDirection(casterPos, pos, effect.areaTargetMode == AreaTargetMode.Directional8);
                        currentHoverDirection = dir;
                        offsets = RotateOffsets(offsets, dir);
                    }

                    foreach (Vector2Int offset in offsets)
                    {
                        Vector2Int target = center + offset;
                        if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                        GetButtonScript(target).RangeOn(0);
                        hoverRangeButtons.Add(target);
                    }
                }
            }
            return;
        }

        // 시전자 미선택 상태: 기물 위에 올리면 범위와 정보 표시
        if (!isSelectedButtonActive())
        {
            ClearHoverPieceRange();
            Piece hoveredPiece = GetButtonScript(pos).GetPieceScript();

            Piece newCaster = (hoveredPiece != null && hoveredPiece.teamID == 0)
                ? hoveredPiece : null;
            if (casterPiece != newCaster)
            {
                casterPiece = newCaster;
                CardCanvas.instance?.RefreshAllCardViews();
            }

            // 아군 기물 hover: CardCanvas에 그 기물의 손패/덱을 미리 보여준다
            if (newCaster != null && CardCanvas.instance != null && newCaster != CardCanvas.instance.ActivePiece)
                CardCanvas.instance.SetActivePiece(newCaster, silent: true);

            if (hoveredPiece != null)
            {
                hoverPieceIsAlly = hoveredPiece.teamID == 0;

                // 적군 전체 기본 범위가 켜져 있는 상태라면, 그 위에 마우스를 올린 적군의 범위만 보이도록 잠깐 꺼둔다.
                if (hoveredPiece.teamID == 1 && enemyAlwaysOnRange.Count > 0)
                {
                    ClearAllEnemyRanges();
                    allEnemyRangeSuppressed = true;
                }

                List<Vector2Int> offsets;
                int teamForRange;
                if (pendingEffects.Count > 0 && hoveredPiece.teamID == 0)
                {
                    // 카드 사용 중 + 아군 기물: 카드 effectRange 표시
                    CardEffect effect = pendingEffects.Peek();
                    offsets = effect.effectRange != null
                        ? effect.effectRange.GetAbleRange()
                        : new List<Vector2Int>();
                    teamForRange = 0;
                }
                else
                {
                    // Inspect 모드: 기물 기본 이동 범위 표시
                    offsets = hoveredPiece.GetMoveableButton();
                    teamForRange = hoveredPiece.teamID;
                }
                foreach (Vector2Int offset in offsets)
                {
                    Vector2Int target = pos + offset;
                    if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                    GetButtonScript(target).RangeOn(teamForRange);
                    hoverPieceRangeButtons.Add(target);
                }
                ShowButtonInfo(pos);
            }
            else
            {
                HideButtonInfo();
            }
        }
    }

    public void ButtonUnhovered()
    {
        ClearHoverRange();
        ClearHoverPieceRange();
        if (!isSelectedButtonActive())
            HideButtonInfo();
        if (!isSelectedButtonActive() && casterPiece != null)
        {
            casterPiece = null;
            CardCanvas.instance?.RefreshAllCardViews();
        }
        // 좌클릭으로 선택 중인(selectedButton) 아군 기물이 있을 때만 그 기물로 되돌린다.
        // 선택이 없으면(예: 같은 기물을 다시 클릭해 선택 해제한 경우) 방금 hover했던 기물이 그대로 보인다.
        if (isSelectedButtonActive())
        {
            Piece selectedPiece = GetPieceAt(selectedButton);
            if (selectedPiece != null && selectedPiece.teamID == 0)
                CardCanvas.instance?.SetActivePiece(selectedPiece, silent: true);
        }
        if (allEnemyRangeSuppressed)
        {
            ShowAllEnemyRanges();
            allEnemyRangeSuppressed = false;
        }
    }

    public void ClearHoverRange()
    {
        foreach (Vector2Int v in hoverRangeButtons)
            GetButtonScript(v).RangeOff(0);
        hoverRangeButtons.Clear();
    }

    void ClearHoverPieceRange()
    {
        int team = hoverPieceIsAlly ? 0 : 1;
        foreach (Vector2Int v in hoverPieceRangeButtons)
            GetButtonScript(v).RangeOff(team);
        hoverPieceRangeButtons.Clear();
    }

    // 기준 방향(-1,0 = 인스펙터 위쪽 행)에서 dir 방향으로 오프셋 목록을 회전
    public List<Vector2Int> RotateOffsets(List<Vector2Int> offsets, Vector2Int dir)
    {
        // RangeInfoSO에서 행(row)이 Vector2.x에 매핑되므로 인스펙터 위쪽 = (-1,0)
        // 해당 기준 방향을 dir로 회전시키는 각도 = atan2(dir.y, -dir.x)
        float theta = Mathf.Atan2(dir.y, -dir.x);
        float cos = Mathf.Cos(theta);
        float sin = Mathf.Sin(theta);

        var rotated = new List<Vector2Int>();
        foreach (Vector2Int offset in offsets)
        {
            float rx = offset.x * cos + offset.y * sin;
            float ry = -offset.x * sin + offset.y * cos;
            rotated.Add(new Vector2Int(Mathf.RoundToInt(rx), Mathf.RoundToInt(ry)));
        }
        return rotated;
    }

    // from → to 방향을 4방향 또는 8방향으로 스냅
    Vector2Int GetSnappedDirection(Vector2Int from, Vector2Int to, bool eightDir)
    {
        Vector2Int delta = to - from;
        if (delta == Vector2Int.zero) return Vector2Int.up;

        if (!eightDir)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
            else
                return new Vector2Int(0, delta.y > 0 ? 1 : -1);
        }
        else
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            int snappedDeg = Mathf.RoundToInt(angle / 45f) * 45;
            float rad = snappedDeg * Mathf.Deg2Rad;
            return new Vector2Int(Mathf.RoundToInt(Mathf.Cos(rad)), Mathf.RoundToInt(Mathf.Sin(rad)));
        }
    }
}
