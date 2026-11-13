using System.Collections.Generic;
using UnityEngine;

// 턴 시작/종료 타이밍에 효과 큐(pendingEffects)에 넣을 효과를 모으고, 처리 뒤 지속 턴을 줄인다. 실제 처리와 타이밍 순서는 Board.TurnControl.cs의
// 턴 코루틴이 ResolveEffects로 맡는다.
public partial class Board
{
    // teamId 기물들의 자기 턴 phase 타이밍 처리 — 모든 상태(상태이상·버프·턴 효과)에 같은 규칙을 쓴다.
    // 걸린 순서대로 각 상태의 TurnEffectAt(phase)를 모아 그 기물을 시전자로 큐 뒤에 넣는다. SumsTurnEffect인 종류(독·화상·재생)는
    // 같은 종류끼리 dmg를 합산해 한 번만 넣는다. 처리 중 죽은 기물의 남은 효과는 "시전자가 보드에 없으면 건너뜀" 규칙으로 빠진다.
    // 지속시간은 여기서 줄이지 않는다 — 효과가 적용된 뒤에 줄어야 하므로(만료될 강화가 이번 턴 효과에는 아직 반영되게),
    // 이번 타이밍에 줄어들 상태 목록을 돌려주고 호출부가 ResolveEffects 뒤에 TickStatusDurations로 줄인다.
    // 수집 시점의 목록이라 효과 처리 중 새로 걸린 상태는 같은 타이밍에 줄지 않는다.
    List<(Piece piece, StatusEffect status)> EnqueueTeamStatusEffects(int teamId, TurnPhase phase)
    {
        var toTick = new List<(Piece, StatusEffect)>();
        foreach (Piece piece in GetTeamPieces(teamId))
        {
            var statuses = new List<StatusEffect>(piece.activeEffects);

            var effects = new List<CardEffect>();
            var sumIndex = new Dictionary<System.Type, int>(); // 합산 종류 → effects 안의 위치
            foreach (StatusEffect status in statuses)
            {
                if (status.TickPhase == phase) toTick.Add((piece, status));
                CardEffect ce = status.TurnEffectAt(phase);
                if (ce == null) continue;
                if (status.SumsTurnEffect && sumIndex.TryGetValue(status.GetType(), out int index))
                {
                    effects[index] = effects[index] with { dmg = effects[index].dmg + ce.dmg };
                    continue;
                }
                if (status.SumsTurnEffect) sumIndex[status.GetType()] = effects.Count;
                effects.Add(ce);
            }
            foreach (CardEffect ce in effects)
                EnqueueEffect(ce, piece);
        }
        return toTick;
    }

    // EnqueueTeamStatusEffects가 돌려준 상태들의 지속시간을 줄이고, 만료되면 제거한다(Piece.RemoveStatusEffect).
    // 그 사이 죽은 기물이나 이미 사라진 상태(정화·소모)는 건너뛴다.
    void TickStatusDurations(List<(Piece piece, StatusEffect status)> toTick)
    {
        foreach (var (piece, status) in toTick)
        {
            if (piece == null || piece.hp <= 0 || !piece.activeEffects.Contains(status)) continue;
            if (!status.TickDuration())
                piece.RemoveStatusEffect(status);
        }
    }

    // teamId 팀의 턴이 끝날 때 상대 팀 기물의 방어도가 사라진다 — 방어도는 상대 턴 동안만 막아 준다(Piece.OnTurnEndOther).
    void ResetOpposingShields(int teamId)
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p != null && p.teamID != teamId) p.OnTurnEndOther();
            }
    }

    List<Piece> GetTeamPieces(int teamId)
    {
        var result = new List<Piece>();
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p != null && p.teamID == teamId)
                    result.Add(p);
            }
        return result;
    }
}
