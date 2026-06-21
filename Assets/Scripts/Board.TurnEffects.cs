using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    void ProcessTeamTurnEffects(int teamId, TurnPhase phase)
    {
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                var pos = new Vector2(x, y);
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p != null && p.teamID == teamId)
                    ExecutePieceTurnEffects(pos, p, phase);
            }
        }
    }

    void ExecutePieceTurnEffects(Vector2 pos, Piece piece, TurnPhase phase)
    {
        for (int i = piece.activeEffects.Count - 1; i >= 0; i--)
        {
            if (piece.activeEffects[i] is not TurnEffect te || te.phase != phase) continue;

            ExecuteTurnEffect(pos, piece, te);

            te.duration--;
            if (te.duration <= 0)
            {
                te.OnRemove(piece);
                piece.activeEffects.RemoveAt(i);
            }
        }
    }

    void ExecuteTurnEffect(Vector2 pos, Piece caster, TurnEffect effect)
    {
        CardEffect ce = effect.cardEffect;

        if (ce.targetlogic == TargetLogic.AllEnemiesInRange || ce.targetlogic == TargetLogic.AllAlliesInRange)
        {
            if (ce.effectRange == null) return;

            var targets = new List<(Vector2 pos, Piece piece)>();
            foreach (Vector2 offset in ce.effectRange.GetAbleRange())
            {
                Vector2 targetPos = pos + offset;
                if (targetPos.x < 0 || targetPos.x >= N || targetPos.y < 0 || targetPos.y >= M) continue;
                Piece target = GetButtonScript(targetPos).GetPieceScript();
                if (target == null) continue;
                if (ce.targetlogic == TargetLogic.AllEnemiesInRange && target.teamID == caster.teamID) continue;
                if (ce.targetlogic == TargetLogic.AllAlliesInRange  && target.teamID != caster.teamID) continue;
                targets.Add((targetPos, target));
            }

            if (targets.Count == 0) return;

            // 데미지 타입은 Board.Combat의 AreaAttackPiece를 그대로 재사용 (피해 적용, 텍스트, 죽음 처리, 범위 표시까지 동일하게 처리됨)
            if (ce.type == EffectType.Damage)
            {
                AreaAttackPiece(pos, targets.ConvertAll(t => t.pos), ce.dmg, ce);
                return;
            }

            var animCoroutines = new List<IEnumerator> { TriggerAnimCor(caster, ce.animTrigger, cardEffect: ce) };
            foreach (var (targetPos, target) in targets)
                animCoroutines.Add(EnqueueTurnEffectOnPiece(target, ce));
            motionQueue.Enqueue(Parallel(animCoroutines.ToArray()));
            StartMotionQueue();
        }
        else
        {
            // 자기 자신 대상 데미지(독/화상 등)는 SelfDamagePiece 재사용
            if (ce.type == EffectType.Damage)
            {
                SelfDamagePiece(pos, ce.dmg, ce);
                return;
            }

            motionQueue.Enqueue(Parallel(
                TriggerAnimCor(caster, ce.animTrigger, cardEffect: ce),
                EnqueueTurnEffectOnPiece(caster, ce)));
            StartMotionQueue();
        }
    }

    IEnumerator EnqueueTurnEffectOnPiece(Piece target, CardEffect ce)
    {
        switch (ce.type)
        {
            case EffectType.Heal:
                target.GetHeal(ce.dmg);
                yield return target.HealText(ce.dmg);
                break;
            case EffectType.Shield:
                target.GetShield(ce.dmg);
                yield return target.ShieldText(ce.dmg);
                break;
            case EffectType.ColDamageUp:
                target.colDamage += ce.dmg;
                if (ce.animTrigger != null) yield return TriggerAnimCor(target, ce.animTrigger, 0.3f, false);
                break;
        }
    }
}
