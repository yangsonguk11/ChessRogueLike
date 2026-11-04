using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2Int> hoverRangeButtons = new List<Vector2Int>();
    Vector2Int currentHoverDirection = Vector2Int.up;

    public void ButtonHovered(Vector2Int pos)
    {
        // AoE 카드 범위 미리보기. MouseCentered는 캐스터를 선택하지 않고 targeting 클릭 한 번으로 바로 발동하므로
        // 캐스터 선택 전에도 마우스 위치 기준으로 미리보기를 보여줘야 함.
        // Directional4/8도 마찬가지로, 캐스터 확정(드롭)은 아직 안 됐어도 카드를 든 채로 보드를 훑는 동안
        // 방향 미리보기가 나와야 하므로, 카드 소유 기물(activePiece) 위치를 캐스터 삼아 미리 보여준다.
        // 카드를 들고만 있는 동안은 효과 큐가 비어 있으므로 미리보기 중인 조준 효과를 본다(CurrentAimEffect).
        CardEffect pendingEffect = CurrentAimEffect();
        bool previewableWithoutCaster = pendingEffect != null &&
            (pendingEffect.areaTargetMode == AreaTargetMode.MouseCentered ||
             pendingEffect.areaTargetMode == AreaTargetMode.Directional4 ||
             pendingEffect.areaTargetMode == AreaTargetMode.Directional8);
        if (pendingEffect != null && (isSelectedButtonActive() || previewableWithoutCaster))
        {
            CardEffect effect = pendingEffect;
            if (effect.areaTargetMode != AreaTargetMode.Fixed)
            {
                hoverRangeButtons.Clear();

                // 범위 칸 계산은 실제 실행(ExecuteAreaEffect)과 같은 GetAreaEffectCells — 여기서는 미리보기의 시전자 칸
                // (선택 칸, 없으면 카드 주인)과 마우스 방향만 정한다.
                Vector2Int casterPos = pos;
                Vector2Int dir = currentHoverDirection;
                if (effect.areaTargetMode != AreaTargetMode.MouseCentered)
                {
                    Button ownerButton = GetButtonForPiece(CardCanvas.instance?.ActivePiece);
                    casterPos = isSelectedButtonActive() ? selectedButton
                        : (ownerButton != null ? ownerButton.GetLocation() : pos);
                    dir = GetSnappedDirection(casterPos, pos, effect.areaTargetMode == AreaTargetMode.Directional8);
                    currentHoverDirection = dir;
                }

                hoverRangeButtons.AddRange(GetAreaEffectCells(effect, casterPos, pos, dir, out _));
                RefreshRangeDisplay();
            }
            return;
        }

        // 시전자 미선택 상태: 기물 위에 올리면 정보창 + 3순위 "기물 선택" 범위(ShowButtonInfo). 카드를 든 상태면
        // 1순위(조준)가 켜져 있어 이 범위는 가려진다.
        if (!isSelectedButtonActive())
        {
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
                ShowButtonInfo(pos);
            else
                HideButtonInfo();
        }
    }

    public void ButtonUnhovered()
    {
        ClearHoverRange();
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
    }

    public void ClearHoverRange()
    {
        hoverRangeButtons.Clear();
        RefreshRangeDisplay();
    }

    // 기준 방향(-1,0 = 인스펙터 위쪽 행)에서 dir 방향으로 오프셋 목록을 회전
    public static List<Vector2Int> RotateOffsets(List<Vector2Int> offsets, Vector2Int dir)
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
    public static Vector2Int GetSnappedDirection(Vector2Int from, Vector2Int to, bool eightDir)
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
