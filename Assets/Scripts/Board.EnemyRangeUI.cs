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
            if (p == null || p is not Enemy enemy) continue;

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
}
