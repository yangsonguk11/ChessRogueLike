using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2> hoverRangeButtons = new List<Vector2>();
    Vector2 currentHoverDirection = Vector2.up;

    public void ButtonHovered(Vector2 pos)
    {
        if (pendingEffects.Count == 0) return;
        CardEffect effect = pendingEffects.Peek();
        if (effect.areaTargetMode == AreaTargetMode.Fixed) return;
        if (!isSelectedButtonActive()) return;
        if (!selectedButtonMovable.Contains(pos)) { ClearHoverRange(); return; }

        ClearHoverRange();

        List<Vector2> offsets = effect.effectRange?.GetAbleRange();
        if (offsets == null) return;

        Vector2 center;
        if (effect.areaTargetMode == AreaTargetMode.MouseCentered)
        {
            center = pos;
        }
        else
        {
            // Directional4 or Directional8: rotate pattern from caster toward hovered pos
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

    public void ButtonUnhovered()
    {
        ClearHoverRange();
    }

    public void ClearHoverRange()
    {
        foreach (Vector2 v in hoverRangeButtons)
        {
            // 사거리 표시용 셀(selectedButtonMovable)은 끄지 않음
            if (!selectedButtonMovable.Contains(v))
                GetButtonScript(v).RangeOff();
        }
        hoverRangeButtons.Clear();
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
