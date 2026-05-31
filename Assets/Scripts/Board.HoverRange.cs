using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2> hoverRangeButtons = new List<Vector2>();
    List<Vector2> hoverPieceRangeButtons = new List<Vector2>();
    Vector2 currentHoverDirection = Vector2.up;

    public void ButtonHovered(Vector2 pos)
    {
        // AoE 카드 범위 미리보기 (카드 사용 중)
        if (pendingEffects.Count > 0)
        {
            CardEffect effect = pendingEffects.Peek();
            if (effect.areaTargetMode != AreaTargetMode.Fixed && isSelectedButtonActive())
            {
                ClearHoverRange();

                List<Vector2> offsets = effect.effectRange?.GetAbleRange();
                if (offsets != null)
                {
                    Vector2 center;
                    if (effect.areaTargetMode == AreaTargetMode.MouseCentered)
                    {
                        center = pos;
                    }
                    else
                    {
                        center = selectedButton;
                        Vector2 dir = GetSnappedDirection(selectedButton, pos, effect.areaTargetMode == AreaTargetMode.Directional8);
                        currentHoverDirection = dir;
                        offsets = RotateOffsets(offsets, dir);
                    }

                    foreach (Vector2 offset in offsets)
                    {
                        Vector2 target = center + offset;
                        if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                        GetButtonScript(target).RangeOn();
                        hoverRangeButtons.Add(target);
                    }
                }
            }
            return;
        }

        // Inspect 모드: 기물 위에 올리면 행동 범위와 정보 표시 (선택 없이)
        if (boardmode == BoardMode.Inspect && !isSelectedButtonActive())
        {
            ClearHoverPieceRange();
            Piece hoveredPiece = GetButtonScript(pos).GetPieceScript();
            if (hoveredPiece != null)
            {
                List<Vector2> moveRange = hoveredPiece.GetMoveableButton();
                foreach (Vector2 offset in moveRange)
                {
                    Vector2 target = pos + offset;
                    if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                    GetButtonScript(target).RangeOn();
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
    }

    public void ClearHoverRange()
    {
        foreach (Vector2 v in hoverRangeButtons)
            GetButtonScript(v).RangeOff();
        hoverRangeButtons.Clear();
    }

    void ClearHoverPieceRange()
    {
        foreach (Vector2 v in hoverPieceRangeButtons)
            GetButtonScript(v).RangeOff();
        hoverPieceRangeButtons.Clear();
    }

    // 기준 방향(-1,0 = 인스펙터 위쪽 행)에서 dir 방향으로 오프셋 목록을 회전
    public List<Vector2> RotateOffsets(List<Vector2> offsets, Vector2 dir)
    {
        // RangeInfoSO에서 행(row)이 Vector2.x에 매핑되므로 인스펙터 위쪽 = (-1,0)
        // 해당 기준 방향을 dir로 회전시키는 각도 = atan2(dir.y, -dir.x)
        float theta = Mathf.Atan2(dir.y, -dir.x);
        float cos = Mathf.Cos(theta);
        float sin = Mathf.Sin(theta);

        var rotated = new List<Vector2>();
        foreach (Vector2 offset in offsets)
        {
            float rx = offset.x * cos + offset.y * sin;
            float ry = -offset.x * sin + offset.y * cos;
            rotated.Add(new Vector2(Mathf.RoundToInt(rx), Mathf.RoundToInt(ry)));
        }
        return rotated;
    }

    // from → to 방향을 4방향 또는 8방향으로 스냅
    Vector2 GetSnappedDirection(Vector2 from, Vector2 to, bool eightDir)
    {
        Vector2 delta = to - from;
        if (delta == Vector2.zero) return Vector2.up;

        if (!eightDir)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return new Vector2(delta.x > 0 ? 1 : -1, 0);
            else
                return new Vector2(0, delta.y > 0 ? 1 : -1);
        }
        else
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            int snappedDeg = Mathf.RoundToInt(angle / 45f) * 45;
            float rad = snappedDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.RoundToInt(Mathf.Cos(rad)), Mathf.RoundToInt(Mathf.Sin(rad)));
        }
    }
}
