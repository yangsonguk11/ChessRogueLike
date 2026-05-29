using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    void TurnStart()
    {
        ProcessTeamTurnEffects(0, TurnPhase.OwnTurnStart);
        playerMovedThisTurn = false;
        playerDamagedThisTurn = false;
        CardCanvas.instance.DrawTurnStartCards();
        CardCanvas.instance.GetMaxEnergy();
    }

    public void AllyTurnEnd()
    {
        if (!boardReady) return;
        ProcessTeamTurnEffects(0, TurnPhase.OwnTurnEnd);
        TurnEnd(0);
        FinishCardUsage();
        ClearSelectedButton();
        CardCanvas.instance.RestoreThisTurnCosts(); // ThisTurnOnly 코스트 복구
        CardCanvas.instance.HandtoDiscardAll();
    }

    public void EnemyTurnEnd()
    {
        if (!boardReady) return;
        ProcessTeamTurnEffects(1, TurnPhase.OwnTurnEnd);
        TurnEnd(1);
        ClearSelectedButton();
    }

    void TurnEnd(int teamid)
    {
        Vector2 pos = new Vector2(0, 0);
        for (int i = 0; i < N; i++)
        {
            pos.x = i;
            for (int j = 0; j < M; j++)
            {
                pos.y = j;
                Piece pp = GetButtonScript(pos).GetPieceScript();
                if (pp != null)
                {
                    Debug.LogFormat("{0} {1}", pp.teamID, teamid);
                    if (pp.teamID == teamid) pp.OnTurnEnd();
                    else pp.OnTurnEndOther();

                    if (pp.hp <= 0)
                    {
                        if (pp.teamID == 1) enemyPositions.Remove(pos);
                        StartCoroutine(pp.DeathCor());
                    }
                }
            }
        }
    }

    void PlayEnemyTurn()
    {
        StartCoroutine(PlayEnemyTurnCoroutine());
    }

    public IEnumerator PlayEnemyTurnCoroutine()
    {
        List<Vector2> currentEnemies = new List<Vector2>(enemyPositions);

        foreach (Vector2 pos in currentEnemies)
        {
            Piece p = GetButtonScript(pos).GetPiece()?.GetComponent<Piece>();
            if (p == null || p is not Enemy enemy) continue;

            selectedButton = pos;
            Card card = enemy.GetNextMove();

            if (card != null && !enemy.IsStunned())
            {
                UseCard(card);
                yield return new WaitUntil(() => pendingEffects.Count == 0 && !queuecoroutineworking && !turnEffectQueueRunning);
                enemy.ChangeMove();
                enemy.ActionText();
            }
            else if (enemy.IsStunned())
            {
                enemy.ActionText();
            }

            yield return new WaitForSeconds(0.5f);
            ClearSelectedButton();
        }

        TurnManager.instance.EndEnemyTurn();
        TurnManager.instance.StartPlayerTurn();
    }
}
