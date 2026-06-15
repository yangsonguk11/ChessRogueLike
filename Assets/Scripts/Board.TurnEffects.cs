using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    void ProcessAllTurnEffects(TurnPhase phase)
    {
        ProcessTeamTurnEffects(0, phase);
        ProcessTeamTurnEffects(1, phase);
    }

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
        Button casterButton = GetButtonScript(pos);

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

            if (ce.animTrigger != null)
                caster?.TriggerAnim(ce.animTrigger);
            else if (ce.type == EffectType.Damage && casterButton.GetPiece() != null)
                turnEffectQueue.Enqueue(PieceAreaAttackCor(casterButton, 0.6f));

            foreach (var (targetPos, target) in targets)
                EnqueueTurnEffectOnPiece(target, ce, targetPos);
        }
        else
        {
            if (ce.animTrigger != null)
                caster?.TriggerAnim(ce.animTrigger);
            else if (ce.type == EffectType.Damage && casterButton.GetPiece() != null)
                turnEffectQueue.Enqueue(PieceAreaAttackCor(casterButton, 0.5f));
            EnqueueTurnEffectOnPiece(caster, ce, pos);
        }

        if (!turnEffectQueueRunning)
            StartCoroutine(ProcessTurnEffectQueue());
    }

    void EnqueueTurnEffectOnPiece(Piece target, CardEffect ce, Vector2 targetPos)
    {
        switch (ce.type)
        {
            case EffectType.Damage:
                int hpLeft = target.GetDamage(ce.dmg, AttackType.NormalAttack);
                if (target.teamID == 0) playerDamagedThisTurn = true;
                turnEffectQueue.Enqueue(target.DamageText(ce.dmg));
                if (hpLeft <= 0)
                {
                    if (target.teamID == 1) enemyPositions.Remove(targetPos);
                    turnEffectQueue.Enqueue(target.DeathCor());
                }
                break;
            case EffectType.Heal:
                target.GetHeal(ce.dmg, AttackType.NormalAttack);
                turnEffectQueue.Enqueue(target.HealText(ce.dmg));
                break;
            case EffectType.Shield:
                target.GetShield(ce.dmg, AttackType.NormalAttack);
                turnEffectQueue.Enqueue(target.ShieldText(ce.dmg));
                break;
            case EffectType.ColDamageUp:
                target.colDamage += ce.dmg;
                if (ce.animTrigger != null) target.TriggerAnim(ce.animTrigger);
                break;
        }
    }
}
