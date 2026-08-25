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

            // 실제 카드가 효과를 적용할 때 쓰는 것과 동일한 함수(ApplyCardEffectNow)를
            // Board.ScheduledEffects.cs의 큐를 통해 순차적으로 재사용한다.
            EnqueueScheduledEffect(pos, te.cardEffect);

            te.duration--;
            if (te.duration <= 0)
            {
                te.OnRemove(piece);
                piece.activeEffects.RemoveAt(i);
            }
        }
    }

    // 시전자(caster) 기준으로 임의의 CardEffect 하나를 즉시 실행. onKillEffect(처치 시 효과)와
    // 영구 스탯 강화(RequestPieceSelection 콜백)가 공유해서 사용한다. TurnEffect/유물은 더 많은
    // EffectType을 지원하는 ApplyCardEffectNow(Board.CardEffect.cs)를 Board.ScheduledEffects.cs의
    // 큐로 재사용하도록 옮겨갔다 — 이 함수는 지원 EffectType이 더 좁으니(Heal/Shield/ColDamageUp류 등)
    // 새로 추가하는 예약 효과는 가능하면 EnqueueScheduledEffect 쪽을 쓰는 게 낫다.
    void ExecuteCardEffectOnPiece(Vector2 pos, Piece caster, CardEffect ce)
    {
        if (ce.targetlogic == TargetLogic.AllEnemiesInRange || ce.targetlogic == TargetLogic.AllAlliesInRange
            || ce.targetlogic == TargetLogic.AllPiecesInRange)
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
                animCoroutines.Add(EnqueueCardEffectOnPiece(target, ce));
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
                EnqueueCardEffectOnPiece(caster, ce)));
            StartMotionQueue();
        }
    }

    IEnumerator EnqueueCardEffectOnPiece(Piece target, CardEffect ce)
    {
        switch (ce.type)
        {
            case EffectType.Heal:
                int healed = target.GetHeal(ce.dmg);
                yield return target.HealText(healed);
                break;
            case EffectType.Shield:
                target.GetShield(ce.dmg);
                yield return target.ShieldText(ce.dmg);
                break;
            case EffectType.ColDamageUp:
                target.AddColDamage(ce.dmg);
                CardCanvas.instance?.RefreshAllCardViews();
                if (ce.animTrigger != null) yield return TriggerAnimCor(target, ce.animTrigger, 0.3f, false);
                break;
            case EffectType.BaseColDamageUp:
                // colDamageBonus도 함께 올려 이번 전투뿐 아니라 다음 전투로도 이어지게 함 (GetPieceData가 colDamageBonus를 저장)
                target.AddColDamage(ce.dmg, permanent: true);
                CardCanvas.instance?.RefreshAllCardViews();
                if (ce.animTrigger != null) yield return TriggerAnimCor(target, ce.animTrigger, 0.3f, false);
                break;
            case EffectType.ShieldBonusUp:
                target.AddShieldBonus(ce.dmg);
                CardCanvas.instance?.RefreshAllCardViews();
                if (ce.animTrigger != null) yield return TriggerAnimCor(target, ce.animTrigger, 0.3f, false);
                break;
            case EffectType.BaseShieldBonusUp:
                // shieldBonusBonus도 함께 올려 이번 전투뿐 아니라 다음 전투로도 이어지게 함 (GetPieceData가 shieldBonusBonus를 저장)
                target.AddShieldBonus(ce.dmg, permanent: true);
                CardCanvas.instance?.RefreshAllCardViews();
                if (ce.animTrigger != null) yield return TriggerAnimCor(target, ce.animTrigger, 0.3f, false);
                break;
            case EffectType.RestoreEnergy:
                // 코스트(에너지)는 기물별이 아니라 아군 전체가 공유하는 자원이라 target 대신 CardCanvas의 currentenergy를 직접 갱신
                if (CardCanvas.instance != null)
                    CardCanvas.instance.currentenergy = Mathf.Min(CardCanvas.instance.currentenergy + ce.dmg, CardCanvas.instance.maxenergy);
                if (ce.animTrigger != null) yield return TriggerAnimCor(target, ce.animTrigger, 0.3f, false);
                break;
        }
    }
}
