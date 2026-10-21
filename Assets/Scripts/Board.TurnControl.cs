using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 턴 흐름. 각 타이밍은 발동할 효과를 효과 큐(pendingEffects)에 넣고 ResolveEffects로 큐가 빌 때까지 처리한 뒤 다음 타이밍으로
// 넘어간다 — 앞 타이밍의 결과(사망, 새로 걸린 턴 효과 등)가 다음 타이밍의 효과 수집에 반영된다. 승패가 정해지면(IsCombatDecided)
// 다음 타이밍으로 넘어가지 않는다. ResolveEffects는 로직만 기다리므로, 연출 대기는 TurnManager의 기존 대기 지점이 맡는다.
public partial class Board
{
    // 이벤트 레벨에서는 TurnManager가 턴을 시작하지 않으므로 이 코루틴 자체가 호출되지 않음
    public IEnumerator PlayerTurnStartCoroutine()
    {
        TriggerRelicsOnTurnStart();
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        EnqueueTeamTurnEffects(0, TurnPhase.OwnTurnStart);
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        playerDamagedThisTurn = false;
        ResetPieceMovedThisTurn();
        CardCanvas.instance.ProcessTurnStartForAllAllies(GetAllAllyPieces()); // 드로우(드로우 시 훅 발생) + 에너지 회복
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        LockEnemyTelegraphs();
        ShowAllEnemyRanges();
    }

    public IEnumerator AllyTurnEndCoroutine()
    {
        if (!boardReady) yield break;
        ClearAllEnemyRanges();

        // 쓰던 카드부터 정리해 둔다 — 남은 카드 효과 뒤에 턴 종료 효과가 붙지 않게. 턴 종료 버튼은 조준 중인 카드(nowusingCard)를
        // 막지 않으므로, 아직 확정 전이면 사용 처리 대신 손패로 되돌린다(손패는 아래에서 어차피 버려지지만, 소멸·저주 카드가
        // 쓰지도 않았는데 소멸되지 않게). 보드 쪽 잔여 상태는 Board.CancelCardUsage가 정리한다(카드가 없어도 안전).
        if (!effectApplied)
        {
            CardCanvas.instance.CancelCardUsage();
            CancelCardUsage();
        }
        else
        {
            RemoveCardEffects();
            FinishCardUsage();
        }
        ClearSelectedButton();

        EnqueueTeamTurnEffects(0, TurnPhase.OwnTurnEnd);
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        TriggerRelicsOnTurnEnd();
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        EnqueueTeamStatusTicks(0);
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        ResetOpposingShields(0);
        CardCanvas.instance.RestoreThisTurnCosts(); // ThisTurnOnly 코스트 복구
        CardCanvas.instance.ResetAllAllyHands(GetAllAllyPieces());
    }

    IEnumerator EnemyTurnEndCoroutine()
    {
        if (!boardReady) yield break;

        EnqueueTeamTurnEffects(1, TurnPhase.OwnTurnEnd);
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        EnqueueTeamStatusTicks(1);
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        ResetOpposingShields(1);
        ClearSelectedButton();
    }

    void PlayEnemyTurn()
    {
        StartCoroutine(PlayEnemyTurnCoroutine());
    }

    public IEnumerator PlayEnemyTurnCoroutine()
    {
        EnqueueTeamTurnEffects(1, TurnPhase.OwnTurnStart);
        yield return ResolveEffects();
        if (IsCombatDecided) yield break;

        // 턴 시작 효과로 죽은 적이 목록에 남지 않도록 그 처리 뒤에 복사한다.
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
                yield return new WaitUntil(() => !IsResolvingEffects && !queuecoroutineworking);
                if (!enemy.IsStunned())
                    enemy.ChangeMove();
                enemy.ActionText();
            }

            ClearSelectedButton();
        }

        yield return EnemyTurnEndCoroutine();
        if (IsCombatDecided) yield break;
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
                yield return new WaitUntil(() => !IsResolvingEffects && !queuecoroutineworking);
                if (!autoAlly.IsStunned())
                    autoAlly.ChangeMove();
                autoAlly.ActionText();
            }

            ClearSelectedButton();
        }
    }
}
