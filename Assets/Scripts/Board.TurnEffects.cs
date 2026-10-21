using System.Collections.Generic;
using UnityEngine;

// 턴 시작/종료 타이밍에 효과 큐(pendingEffects)에 넣을 효과를 모은다. 실제 처리와 타이밍 순서는 Board.TurnControl.cs의
// 턴 코루틴이 ResolveEffects로 맡는다.
public partial class Board
{
    // teamId 기물들이 가진 TurnEffect 중 phase에 해당하는 것을 큐 뒤에 넣는다. 시전자는 그 턴 효과를 가진 기물.
    void EnqueueTeamTurnEffects(int teamId, TurnPhase phase)
    {
        foreach (Piece piece in GetTeamPieces(teamId))
        {
            for (int i = piece.activeEffects.Count - 1; i >= 0; i--)
            {
                if (piece.activeEffects[i] is not TurnEffect te || te.phase != phase) continue;

                EnqueueEffect(te.cardEffect, piece);

                // 지속시간 감소는 StatusEffect.OnTurnEnd 공용 규칙을 따른다(duration이 음수면 영구 — 감소/만료 없음).
                if (!te.OnTurnEnd(piece))
                {
                    te.OnRemove(piece);
                    piece.activeEffects.RemoveAt(i);
                }
            }
        }
    }

    // teamId 기물들의 독·화상·재생 틱(자기 턴 종료 시)을 큐 뒤에 넣는다. 수치 합산과 지속시간 감소는 Piece.TickStatusEffects가
    // 하고, 피해/회복은 CardEffect로 실행된다 — 독·화상은 비공격 자해(SelfDamage: 피격 시 유물·타격음 없음, 사망 처리 포함),
    // 재생은 자기 회복(Heal). 틱으로 죽은 기물의 남은 틱은 "시전자가 보드에 없으면 건너뜀" 규칙으로 빠진다.
    void EnqueueTeamStatusTicks(int teamId)
    {
        foreach (Piece piece in GetTeamPieces(teamId))
        {
            var (poison, burning, regen) = piece.TickStatusEffects();
            if (poison > 0) EnqueueEffect(StatusTickEffect(EffectType.SelfDamage, poison), piece);
            if (burning > 0) EnqueueEffect(StatusTickEffect(EffectType.SelfDamage, burning), piece);
            if (regen > 0) EnqueueEffect(StatusTickEffect(EffectType.Heal, regen), piece);
        }
    }

    static CardEffect StatusTickEffect(EffectType type, int amount) =>
        new CardEffect { requiredMode = BoardMode.Inspect, type = type, dmg = amount, targetlogic = TargetLogic.self };

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
