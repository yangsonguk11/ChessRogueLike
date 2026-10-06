using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2Int> enemyAlwaysOnRange = new List<Vector2Int>();
    // 적 예고 범위가 "켜져 있어야 하는" 상태(플레이어 턴 동안). 실제로 그려져 있는지와는 별개 —
    // 집중 표시(FocusPieceRange) 중에는 켜져 있어야 해도 그리지 않고, 집중이 풀릴 때 이 값을 보고 복원한다.
    bool enemyRangesActive = false;

    // ButtonInfo가 기물 정보를 띄우고 있는 동안 true — 다른 기물(적 예고) 범위를 숨기고 그 기물의 범위만 보여준다.
    // 기물 참조 대신 bool로 두는 이유: 집중 중에 그 기물이 죽어 Destroy되면 Unity의 == null이 true가 돼 해제 판단이 꼬인다.
    bool rangeFocused = false;
    List<Vector2Int> focusedRangeCells = new List<Vector2Int>();
    int focusedRangeTeam;

    public void ShowAllEnemyRanges()
    {
        enemyRangesActive = true;
        ClearDrawnEnemyRanges();
        // 집중 표시 중에 기물 사망/기절로 다시 불려도 다른 범위가 섞여 나오지 않게 그리지 않는다 —
        // UnfocusPieceRange가 그 시점의 최신 상태로 다시 그린다.
        if (rangeFocused) return;
        foreach (Vector2Int pos in enemyPositions)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null || p is not AutoPiece enemy) continue;

            foreach (Vector2Int target in GetActionRangeCells(enemy, pos))
            {
                GetButtonScript(target).RangeOn(1);
                enemyAlwaysOnRange.Add(target);
            }
        }
    }

    public void ClearAllEnemyRanges()
    {
        enemyRangesActive = false;
        ClearDrawnEnemyRanges();
    }

    void ClearDrawnEnemyRanges()
    {
        foreach (Vector2Int v in enemyAlwaysOnRange)
            GetButtonScript(v).RangeOff(1);
        enemyAlwaysOnRange.Clear();
    }

    // AutoPiece(적/자동행동 아군)의 다음 행동 예고 칸(보드 안 절대 좌표).
    List<Vector2Int> GetActionRangeCells(AutoPiece auto, Vector2Int pos)
    {
        // 아군 위치 고정 공격은 시전자 기준 오프셋이 아니라 잠가둔 보드 절대 좌표를 그대로 표시한다.
        if (auto.lockedTargetCells != null && !auto.IsStunned())
            return new List<Vector2Int>(auto.lockedTargetCells);

        var cells = new List<Vector2Int>();
        foreach (Vector2Int offset in auto.GetMoveableButton())
        {
            Vector2Int target = pos + offset;
            if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
            cells.Add(target);
        }
        return cells;
    }

    // ShowButtonInfo와 같은 시점에 호출: 다른 기물(적 예고) 범위를 숨기고, drawOwnRange면 pos 기물의 범위를 그린다.
    // drawOwnRange가 false인 경우(선택/카드 사용 중)는 그 기물의 범위를 selectedButtonMovable이 이미 그리고 있다.
    void FocusPieceRange(Vector2Int pos, bool drawOwnRange)
    {
        Piece piece = GetPieceAt(pos);
        if (piece == null)
        {
            UnfocusPieceRange();
            return;
        }

        ClearFocusedRangeCells();
        rangeFocused = true;
        ClearDrawnEnemyRanges();

        // 휴식 지점/상점 같은 오브젝트(teamID 2)는 보여줄 범위가 없다.
        if (!drawOwnRange || (piece.teamID != 0 && piece.teamID != 1)) return;

        focusedRangeTeam = piece.teamID;
        if (piece is AutoPiece auto)
        {
            focusedRangeCells.AddRange(GetActionRangeCells(auto, pos));
        }
        else
        {
            foreach (Vector2Int offset in piece.GetMoveableButton())
            {
                Vector2Int target = pos + offset;
                if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                focusedRangeCells.Add(target);
            }
        }
        foreach (Vector2Int target in focusedRangeCells)
            GetButtonScript(target).RangeOn(focusedRangeTeam);
    }

    // HideButtonInfo와 같은 시점에 호출: 집중 표시를 끄고, 적 예고 범위가 켜져 있어야 하는 상태면 다시 그린다.
    void UnfocusPieceRange()
    {
        ClearFocusedRangeCells();
        if (!rangeFocused) return;
        rangeFocused = false;
        if (enemyRangesActive) ShowAllEnemyRanges();
    }

    void ClearFocusedRangeCells()
    {
        foreach (Vector2Int v in focusedRangeCells)
            GetButtonScript(v).RangeOff(focusedRangeTeam);
        focusedRangeCells.Clear();
    }

    // 플레이어 턴 시작 시 1회: 다음 행동에 아군 위치 고정 공격(CardEffect.lockOnAllyPositions)이 있는 적은
    // 지금 아군 위치를 중심으로 effectRange를 펼친 칸을 잠가둔다. ShowAllEnemyRanges와 분리한 이유 — 그쪽은
    // 턴 도중에도(기물 사망, 기절) 다시 불리므로 거기서 계산하면 아군이 피한 뒤에도 예고가 따라붙는다.
    public void LockEnemyTelegraphs()
    {
        List<Piece> allies = GetAllAllyPieces();
        foreach (Vector2Int pos in enemyPositions)
        {
            if (GetPieceAt(pos) is not AutoPiece enemy) continue;

            Card next = enemy.GetNextMove();
            CardEffect lockEffect = next != null ? next.effects.Find(e => e.lockOnAllyPositions) : null;
            if (lockEffect?.effectRange == null)
            {
                enemy.lockedTargetCells = null;
                continue;
            }

            List<Vector2Int> offsets = lockEffect.effectRange.GetAbleRange();
            var cells = new List<Vector2Int>();
            foreach (Piece ally in allies)
            {
                Vector2Int allyPos = FindPiecePos(ally);
                foreach (Vector2Int offset in offsets)
                {
                    Vector2Int cell = allyPos + offset;
                    if (cell.x < 0 || cell.x >= N || cell.y < 0 || cell.y >= M) continue;
                    if (!cells.Contains(cell)) cells.Add(cell);
                }
            }
            enemy.lockedTargetCells = cells;
        }
    }
}
