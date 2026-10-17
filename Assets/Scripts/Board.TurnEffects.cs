using UnityEngine;

public partial class Board
{
    void ProcessTeamTurnEffects(int teamId, TurnPhase phase)
    {
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p != null && p.teamID == teamId)
                    ExecutePieceTurnEffects(pos, p, phase);
            }
        }
    }

    void ExecutePieceTurnEffects(Vector2Int pos, Piece piece, TurnPhase phase)
    {
        for (int i = piece.activeEffects.Count - 1; i >= 0; i--)
        {
            if (piece.activeEffects[i] is not TurnEffect te || te.phase != phase) continue;

            // 실제 카드가 효과를 적용할 때 쓰는 것과 동일한 함수(ApplyCardEffectNow)를
            // Board.ScheduledEffects.cs의 큐를 통해 순차적으로 재사용한다. 시전자는 이 턴 효과를 가진 기물.
            EnqueueScheduledEffect(pos, te.cardEffect with { caster = piece });

            // 지속시간 감소는 StatusEffect.OnTurnEnd 공용 규칙을 따른다(duration이 음수면 영구 — 감소/만료 없음).
            if (!te.OnTurnEnd(piece))
            {
                te.OnRemove(piece);
                piece.activeEffects.RemoveAt(i);
            }
        }
    }
}
