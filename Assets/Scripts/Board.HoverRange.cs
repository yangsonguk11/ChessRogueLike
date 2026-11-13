using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2Int> hoverRangeButtons = new List<Vector2Int>();

    public void ButtonHovered(Vector2Int pos)
    {
        // AoE 카드 범위 미리보기. MouseCentered는 캐스터를 선택하지 않고 targeting 클릭 한 번으로 바로 발동하므로
        // 캐스터 선택 전에도 마우스 위치 기준으로 미리보기를 보여줘야 함.
        // 방향성 범위 효과(RotatesRangeWithFacing)도 마찬가지로, 캐스터 확정(드롭)은 아직 안 됐어도 카드를 든 채로 보드를
        // 훑는 동안 방향 미리보기가 나와야 하므로, 카드 소유 기물(activePiece) 위치를 캐스터 삼아 미리 보여준다.
        // 카드를 들고만 있는 동안은 효과 큐가 비어 있으므로 미리보기 중인 조준 효과를 본다(CurrentAimEffect).
        CardEffect pendingEffect = CurrentAimEffect();
        bool previewableWithoutCaster = pendingEffect != null &&
            (pendingEffect.areaTargetMode == AreaTargetMode.MouseCentered || pendingEffect.RotatesRangeWithFacing);
        if (pendingEffect != null && (isSelectedButtonActive() || previewableWithoutCaster))
        {
            CardEffect effect = pendingEffect;
            if (effect.areaTargetMode != AreaTargetMode.Fixed || effect.RotatesRangeWithFacing)
            {
                hoverRangeButtons.Clear();

                // 범위 칸 계산은 실제 실행(ExecuteAreaEffect)과 같은 GetAreaEffectCells — 여기서는 미리보기의 시전자 칸
                // (선택 칸, 없으면 카드 주인)과 마우스 쪽 방향만 정한다. 방향은 미리보기에만 쓰고 기물의 facing은 바꾸지 않는다
                // (카드를 실제로 쓸 때 ApplyCardEffectNow가 TurnToward로 바꾼다).
                Button ownerButton = GetButtonForPiece(CardCanvas.instance?.ActivePiece);
                Vector2Int casterPos = pos;
                if (effect.areaTargetMode != AreaTargetMode.MouseCentered)
                    casterPos = isSelectedButtonActive() ? selectedButton
                        : (ownerButton != null ? ownerButton.GetLocation() : pos);

                Vector2Int? previewFacing = null;
                if (effect.RotatesRangeWithFacing)
                {
                    Piece caster = effect.areaTargetMode != AreaTargetMode.MouseCentered
                        ? GetPieceAt(casterPos) : CardCanvas.instance?.ActivePiece;
                    Vector2Int from = effect.areaTargetMode != AreaTargetMode.MouseCentered || ownerButton == null
                        ? casterPos : ownerButton.GetLocation();
                    previewFacing = FacingToward(from, pos, caster != null ? caster.facing : Vector2Int.zero);
                }

                hoverRangeButtons.AddRange(GetAreaEffectCells(effect, casterPos, pos, out _, previewFacing));
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

    // ── 바라보는 방향(Piece.facing) ─────────────────────────────
    // 방향이 바뀌는 규칙은 이 두 함수가 전부다. 행동할 때(ApplyCardEffectNow·MovePiece·AutoPiece.PlanNextMove)와 공격받을 때
    // (Board.Combat)가 TurnToward를 부르고, 범위·예고·실행은 Piece.facing만 읽는다.

    // from → to를 바라보는 방향(4방향). |dx|, |dy| 중 더 먼 쪽 축의 부호 방향이다 — (0,0)에서 (1,3)이면 (0,1).
    // 같으면 현재 바라보던 방향과 같은 축으로, 목표 쪽을 본다 — 목표가 (1,1)일 때 현재 (-1,0)이면 (1,0), (0,-1)이면 (0,1),
    // (1,0)·(0,1)이면 그대로. 현재 방향이 없으면 x축이다. from == to면 현재 방향을 그대로 돌려준다.
    public static Vector2Int FacingToward(Vector2Int from, Vector2Int to, Vector2Int current)
    {
        Vector2Int delta = to - from;
        if (delta == Vector2Int.zero) return current;

        int absX = Mathf.Abs(delta.x);
        int absY = Mathf.Abs(delta.y);
        if (absX > absY) return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
        if (absY > absX) return new Vector2Int(0, delta.y > 0 ? 1 : -1);
        return current.y != 0
            ? new Vector2Int(0, delta.y > 0 ? 1 : -1)
            : new Vector2Int(delta.x > 0 ? 1 : -1, 0);
    }

    // piece가 from 칸에서 to 칸을 바라보게 한다(방향이 고정(facingLocked)이면 바꾸지 않는다). 방향이 실제로 바뀔 때만 ①facing을
    // 즉시 갱신하고(논리) ②모델 회전을 motionQueue에 넣는다(연출). 효과 큐는 모션 큐를 기다리지 않으므로 논리 시점에 모델을 바로
    // 돌리면 앞서 큐에 쌓인 연출(이동·공격)이 재생되기 전에 모델이 먼저 돈다 — 큐에 넣어 그 연출 뒤에 돌린다.
    // 코루틴은 facing을 읽지 않고 enqueue 시점에 구한 월드 방향을 받는다. 바뀐 뒤 다른 연출이 모델을 돌려도 facing은 그대로다.
    public void TurnToward(Piece piece, Vector2Int from, Vector2Int to)
    {
        if (piece == null || piece.facingLocked) return;

        Vector2Int dir = FacingToward(from, to, piece.facing);
        if (dir == piece.facing) return;
        piece.facing = dir;

        Vector3 worldDir = WorldDirection(from, dir);
        if (worldDir.sqrMagnitude < 0.0001f) return;
        motionQueue.Enqueue(RotateToFacingCor(piece, worldDir));
        StartMotionQueue();
    }

    // cell 칸에서 보드 좌표 방향 dir로 향하는 월드 방향. 이웃 칸의 월드 좌표 차로 구한다(보드 밖이면 반대쪽 이웃으로).
    Vector3 WorldDirection(Vector2Int cell, Vector2Int dir)
    {
        Vector3 here = GetButtonScript(cell).Piecelocation;
        Vector2Int ahead = cell + dir;
        if (ahead.x >= 0 && ahead.x < N && ahead.y >= 0 && ahead.y < M)
            return GetButtonScript(ahead).Piecelocation - here;
        Vector2Int behind = cell - dir;
        if (behind.x >= 0 && behind.x < N && behind.y >= 0 && behind.y < M)
            return here - GetButtonScript(behind).Piecelocation;
        return Vector3.zero;
    }

    // 바라보는 방향이 바뀐 순간의 모델 회전 — 순수 연출. piece가 큐에서 대기하는 동안 죽어 파괴됐을 수 있다.
    IEnumerator RotateToFacingCor(Piece piece, Vector3 worldDir)
    {
        if (piece != null)
            piece.transform.rotation = Quaternion.LookRotation(worldDir);
        yield break;
    }
}
