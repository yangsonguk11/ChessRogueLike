using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Board
{
    // 새 기물을 보드에 배치한다 — 모든 스폰 경로(전투 시작 배치, 대화 합류, 소환)가 여기를 거친다(이동은 ApplyMoveOccupancy).
    // 기물의 스폰 시 효과(onSpawnCards)를 시전자 기록 사본으로 효과 큐에 넣는다: 카드 처리 중(effectApplied — 소환 카드)이면
    // 그 카드의 이후 효과보다 먼저 실행되게 맨 앞에, 아니면(전투 시작 배치·대화 합류) 스폰 순서대로 뒤에 모아뒀다가
    // 호출부(InitBoard의 전투 시작 처리 / SpawnPiece)가 ProcessNextCardEffect로 처리한다.
    void ApplySpawnOccupancy(Vector2Int pos, GameObject piece)
    {
        GetButtonScript(pos).SetPiece(piece);
        Piece p = piece.GetComponent<Piece>();
        var spawnEffects = p.onSpawnCards.Where(c => c != null).SelectMany(c => c.effects).Select(e => e with { caster = p });
        pendingEffects = new Queue<CardEffect>(effectApplied ? spawnEffects.Concat(pendingEffects) : pendingEffects.Concat(spawnEffects));
    }

    // 이동 애니메이션(PieceMoveCor)이 재생되기 전에, 점유(논리) 상태부터 즉시 확정한다.
    // 카드 예약(다음 카드가 이 칸을 바로 타겟/이동 대상으로 참조) 기능이 성립하려면
    // 시각적 이동이 끝나기 전에도 GetButtonScript(to).GetPieceScript()가 최신값을 반환해야 한다.
    // 반환값(옮겨진 GameObject)은 호출부가 애니메이션 코루틴에 파라미터로 넘길 때 쓴다.
    GameObject ApplyMoveOccupancy(Button from, Button to)
    {
        GameObject piece = from.GetPiece();
        if (piece == null || from == to) return piece;

        to.SetPieceLogical(piece);
        from.RemovePiece();
        RelocateCasterIndicator(from, to);
        RelocatePieceDeckIndicator(from, to);

        if (piece.GetComponent<Piece>() is AutoPiece)
            UpdateAutoPiecePositionList(from.GetLocation(), to.GetLocation());

        return piece;
    }

    // 사망 판정 즉시(사망 연출을 기다리지 않고) 그 칸을 논리적으로 비운다.
    // dead로 대상 일치를 확인해, 죽은 기물이 아직 파괴되지 않은 상태에서 그 사이 다른 기물이
    // 같은 칸으로 이동해왔더라도 방금 도착한 기물을 실수로 지우지 않게 한다.
    void ClearDeadPieceOccupancy(Vector2Int pos, Piece dead)
    {
        if (dead == null) return;
        Button button = GetButtonScript(pos);
        if (button.GetPieceScript() == dead)
            button.RemovePiece();

        if (dead.teamID == 1) enemyPositions.Remove(pos);
        else if (dead is AutoPiece) autoAllyPositions.Remove(pos);

        AddGraveToTeammates(dead);
    }

    // 모든 사망 경로가 위 ClearDeadPieceOccupancy를 거치므로 여기서 한 번에 집계한다. 사망 판정과 같은
    // 호출 스택(로직 단계)에서 즉시 반영되므로, 같은 카드의 다음 효과가 늘어난 무덤을 바로 볼 수 있다.
    // hp <= 0 필터: 같은 광역 공격에서 함께 죽었지만 아직 점유가 해제되지 않은 기물은 '살아있는' 기물이 아니다.
    void AddGraveToTeammates(Piece dead)
    {
        if (dead.deathCounted) return;
        dead.deathCounted = true;
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetButtonScript(new Vector2Int(x, y)).GetPieceScript();
                if (p == null || p == dead || p.teamID != dead.teamID || p.hp <= 0) continue;
                p.AddGrave(1);
            }
        CardCanvas.instance?.UpdateCardInteractability(); // 첫 효과 무덤 조건이 풀렸을 수 있음
    }
}
