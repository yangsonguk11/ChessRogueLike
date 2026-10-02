using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2Int> enemyAlwaysOnRange = new List<Vector2Int>();

    public void ShowAllEnemyRanges()
    {
        ClearAllEnemyRanges();
        foreach (Vector2Int pos in enemyPositions)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null || p is not AutoPiece enemy) continue;

            // 아군 위치 고정 공격은 시전자 기준 오프셋이 아니라 잠가둔 보드 절대 좌표를 그대로 표시한다.
            if (enemy.lockedTargetCells != null && !enemy.IsStunned())
            {
                foreach (Vector2Int target in enemy.lockedTargetCells)
                {
                    GetButtonScript(target).RangeOn(1);
                    enemyAlwaysOnRange.Add(target);
                }
                continue;
            }

            List<Vector2Int> offsets = enemy.GetMoveableButton();
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int target = pos + offset;
                if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                GetButtonScript(target).RangeOn(1);
                enemyAlwaysOnRange.Add(target);
            }
        }
    }

    public void ClearAllEnemyRanges()
    {
        foreach (Vector2Int v in enemyAlwaysOnRange)
            GetButtonScript(v).RangeOff(1);
        enemyAlwaysOnRange.Clear();
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
