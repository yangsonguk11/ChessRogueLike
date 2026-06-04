using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2> enemyAlwaysOnRange = new List<Vector2>();

    public void ShowAllEnemyRanges()
    {
        ClearAllEnemyRanges();
        foreach (Vector2 pos in enemyPositions)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null || p is not Enemy enemy) continue;

            List<Vector2> offsets = enemy.GetMoveableButton();
            foreach (Vector2 offset in offsets)
            {
                Vector2 target = pos + offset;
                if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                GetButtonScript(target).RangeOn(1);
                enemyAlwaysOnRange.Add(target);
            }
        }
    }

    public void ClearAllEnemyRanges()
    {
        foreach (Vector2 v in enemyAlwaysOnRange)
            GetButtonScript(v).RangeOff(1);
        enemyAlwaysOnRange.Clear();
    }
}
