using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // 이벤트 레벨에서는 TurnManager가 턴을 시작하지 않으므로 이 메서드 자체가 호출되지 않음
    void TurnStart()
    {
        TriggerRelicsOnTurnStart();
        ProcessTeamTurnEffects(0, TurnPhase.OwnTurnStart);
        playerDamagedThisTurn = false;
        ResetPieceMovedThisTurn();
        CardCanvas.instance.ProcessTurnStartForAllAllies(GetAllAllyPieces());
        ShowAllEnemyRanges();
    }

    public void AllyTurnEnd()
    {
        if (!boardReady) return;
        ClearAllEnemyRanges();
        ProcessTeamTurnEffects(0, TurnPhase.OwnTurnEnd);
        TriggerRelicsOnTurnEnd();
        TurnEnd(0);
        FinishCardUsage();
        ClearSelectedButton();
        CardCanvas.instance.RestoreThisTurnCosts(); // ThisTurnOnly 코스트 복구
        CardCanvas.instance.ResetAllAllyHands(GetAllAllyPieces());
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
        Vector2Int pos = new Vector2Int(0, 0);
        for (int i = 0; i < N; i++)
        {
            pos.x = i;
            for (int j = 0; j < M; j++)
            {
                pos.y = j;
                Piece pp = GetButtonScript(pos).GetPieceScript();
                if (pp != null)
                {
                    if (pp.teamID == teamid) pp.OnTurnEnd();
                    else pp.OnTurnEndOther();

                    if (pp.hp <= 0)
                    {
                        if (pp.teamID == 1) enemyPositions.Remove(pos);
                        else if (pp is AutoPiece) autoAllyPositions.Remove(pos);
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
        List<Vector2Int> currentEnemies = new List<Vector2Int>(enemyPositions);

        foreach (Vector2Int pos in currentEnemies)
        {
            Piece p = GetButtonScript(pos).GetPiece()?.GetComponent<Piece>();
            if (p == null || p is not AutoPiece enemy) continue;

            selectedButton = pos;
            // 기절 상태면 GetNextMove()가 원래 예고했던 카드 대신 스턴 카드를 반환한다.
            // ChangeMove()는 기절이 아닐 때만 호출해서, 기절이 풀리면 원래 예고했던 행동이 그대로 이어지게 한다.
            Card card = enemy.GetNextMove();

            if (card != null)
            {
                UseCard(card);
                yield return new WaitUntil(() => pendingEffects.Count == 0 && !queuecoroutineworking);
                if (!enemy.IsStunned())
                    enemy.ChangeMove();
                enemy.ActionText();
            }

            ClearSelectedButton();
        }

        TurnManager.instance.EndEnemyTurn();
        TurnManager.instance.StartPlayerTurn();
    }

    // 플레이어 턴 종료 직후 · 적 턴 시작 전에 자동행동 아군을 순서대로 행동시킨다.
    // PlayEnemyTurnCoroutine과 달리 다음 단계 진행(적 턴 시작)은 호출부(TurnManager)가 맡는다.
    public IEnumerator PlayAutoAllyTurnCoroutine()
    {
        List<Vector2Int> currentAutoAllies = new List<Vector2Int>(autoAllyPositions);

        foreach (Vector2Int pos in currentAutoAllies)
        {
            Piece p = GetButtonScript(pos).GetPiece()?.GetComponent<Piece>();
            if (p == null || p is not AutoPiece autoAlly) continue;

            selectedButton = pos;
            Card card = autoAlly.GetNextMove();

            if (card != null)
            {
                UseCard(card);
                yield return new WaitUntil(() => pendingEffects.Count == 0 && !queuecoroutineworking);
                if (!autoAlly.IsStunned())
                    autoAlly.ChangeMove();
                autoAlly.ActionText();
            }

            ClearSelectedButton();
        }
    }
}
