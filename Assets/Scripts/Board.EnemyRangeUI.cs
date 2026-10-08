using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // 4순위 "평상시" 범위: 플레이어 턴 동안의 적 예고 범위. 표시는 RefreshRangeDisplay가 한다.
    List<Vector2Int> enemyAlwaysOnRange = new List<Vector2Int>();

    public void ShowAllEnemyRanges()
    {
        enemyAlwaysOnRange.Clear();
        foreach (Vector2Int pos in enemyPositions)
        {
            if (GetPieceAt(pos) is AutoPiece enemy)
                enemyAlwaysOnRange.AddRange(GetActionRangeCells(enemy, pos));
        }
        RefreshRangeDisplay();
    }

    public void ClearAllEnemyRanges()
    {
        enemyAlwaysOnRange.Clear();
        RefreshRangeDisplay();
    }

    // AutoPiece(적/자동행동 아군)의 다음 행동 예고 칸(보드 안 절대 좌표).
    List<Vector2Int> GetActionRangeCells(AutoPiece auto, Vector2Int pos)
    {
        // 아군 위치 고정 공격은 시전자 기준 오프셋이 아니라 잠가둔 보드 절대 좌표를 그대로 표시한다.
        if (auto.lockedTargetCells != null && !auto.IsStunned())
            return new List<Vector2Int>(auto.lockedTargetCells);
        return CellsInBoard(pos, auto.GetMoveableButton());
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
