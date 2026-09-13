using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    void MovePiece(Vector2Int pos1, Vector2Int pos2, CardEffect cardEffect = null)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        Piece movingPiece = button1script.GetPieceScript();
        if (movingPiece != null && movingPiece.teamID == 0)
            movingPiece.movedThisTurn = true;

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece p1 = button1script.GetPiece().GetComponent<Piece>();
                Piece p2 = piece2.GetComponent<Piece>();
                bool attacked = p1.teamID != p2.teamID && !(cardEffect?.noMoveAttack ?? false)
                    && MoveAttack(p1, p2, button1script, button2script, cardEffect);
                if (!attacked && IsLockedCasterActive())
                    lockedCaster = pos1; // 아군 충돌, 또는 이동공격 도착 칸이 없어 실패: 이동 실패, 원래 위치로 복구
            }
            else
            {
                GameObject movingObj = ApplyMoveOccupancy(button1script, button2script);
                motionQueue.Enqueue(MovePieceWithAnim(movingObj, button1script, button2script, 1f, cardEffect?.animTrigger, cardEffect));
                StartMotionQueue();
            }
        }

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }

    // 이동공격 도착 칸을 찾지 못하면(후보 전부 막힘) false를 반환해 호출부가 일반 이동 실패와 동일하게 처리하게 함.
    bool MoveAttack(Piece pScript1, Piece pScript2, Button bScript1, Button bScript2, CardEffect cardEffect = null)
    {
        Vector2Int adjacentPos = GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation());
        if (adjacentPos.x < 0) return false; // 도착할 칸이 없음: 공격 취소, 이동 실패로 처리

        int dmg = pScript1.colDamage;

        Vector2Int impactPos = bScript2.GetLocation();

        // 공격자가 인접 칸까지 이동 — 애니메이션이 재생되기 전에 점유부터 동기로 확정해서,
        // 이 카드효과가 끝나는 즉시(다음 카드가 예약되더라도) 최신 보드 상태를 참조할 수 있게 한다.
        GameObject attackerObj = ApplyMoveOccupancy(bScript1, GetButtonScript(adjacentPos));

        // DirectionalAttackCard와 동일하게, 공격자→대상 방향으로 moveAttackRange를 회전
        // 공격은 이동 후 adjacentPos에서 일어나므로, 방향도 adjacentPos 기준으로 계산해야 함
        Vector2Int attackDir = GetSnappedDirection(adjacentPos, impactPos, true);
        List<Vector2Int> moveAttackOffsets = RotateOffsets(pScript1.GetMoveAttackRange(), attackDir);
        bool isAreaAttack = !(moveAttackOffsets.Count == 1 && moveAttackOffsets[0] == Vector2Int.zero);
        string attackTrigger = isAreaAttack ? "AreaAttack" : "Attack";

        // TriggerAnimCor도 같은 방향으로 회전해서 표시하도록 Directional8 + currentHoverDirection 재사용
        currentHoverDirection = attackDir;
        CardEffect attackRangeEffect = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = dmg,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = pScript1.MoveAttackRangeInfoSO,
            lockCasterForNext = false,
            areaTargetMode = AreaTargetMode.Directional8,
            animTrigger = attackTrigger,
        };

        // 주 타겟(pScript2) 데미지 적용
        int hpLeft = pScript2.GetDamage(dmg);
        if (pScript2.teamID == 0) playerDamagedThisTurn = true;

        // moveAttackRange 내 나머지 적들 수집 + 데미지 적용 (이동 후 도착 위치 기준, 주 타겟은 위에서 이미 처리했으니 제외)
        var splashResults = new List<(Vector2Int pos, Piece piece, int hpLeft)>();
        if (isAreaAttack)
        {
            foreach (Vector2Int offset in moveAttackOffsets)
            {
                Vector2Int pos = adjacentPos + offset;
                if (pos == impactPos) continue;
                if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p == null || p.teamID == pScript1.teamID) continue;

                int splashHpLeft = p.GetDamage(dmg);
                if (p.teamID == 0) playerDamagedThisTurn = true;
                p.transform.rotation = Quaternion.LookRotation(bScript1.Piecelocation - GetButtonScript(pos).Piecelocation);
                splashResults.Add((pos, p, splashHpLeft));
            }
        }

        // 실제 도착 위치: 적 생존 시 adjacentPos, 사망 시 impactPos(공격자가 계속 이동)
        Vector2Int finalAttackerPos = hpLeft <= 0 ? impactPos : adjacentPos;
        if (IsLockedCasterActive())
            lockedCaster = finalAttackerPos;

        // 모든 타겟(주 타겟 + 스플래시)의 트리거/텍스트를 모아서, 공격자 애니메이션의 실제 타격
        // 프레임(Animation Event)에 맞춰 재생 — PlayCasterAndTargetReaction이 동기화를 담당.
        var targetCoroutines = new List<IEnumerator>
        {
            TriggerAnimCor(pScript2, hpLeft <= 0 ? "Die" : "Hit", 0.3f, false),
            pScript2.DamageText(dmg)
        };
        if (hpLeft <= 0) targetCoroutines.Add(pScript2.PieceDeathSound());
        foreach (var (pos, p, splashHpLeft) in splashResults)
        {
            targetCoroutines.Add(TriggerAnimCor(p, splashHpLeft <= 0 ? "Die" : "Hit", 0.3f, false));
            targetCoroutines.Add(p.DamageText(dmg));
            if (splashHpLeft <= 0) targetCoroutines.Add(p.PieceDeathSound());
        }

        // 시전자 자신에게 걸리는 보너스(실드/회복)도 같은 타격 프레임에 맞춰 같이 재생되도록 targetCoroutines에 합류시킨다.
        if (currentActiveCard != null && currentActiveCard.shieldOnMoveAttack && currentActiveCard.moveAttackShieldAmount > 0)
        {
            int shieldAfter = pScript1.GetShield(currentActiveCard.moveAttackShieldAmount);
            targetCoroutines.Add(pScript1.ShieldText(currentActiveCard.moveAttackShieldAmount));
            targetCoroutines.Add(pScript1.ShieldVisualOn(shieldAfter));
        }
        if (cardEffect != null && cardEffect.healOnHit)
        {
            int totalDmgDealt = dmg * (1 + splashResults.Count);
            if (totalDmgDealt > 0)
            {
                int healed = pScript1.GetHeal(totalDmgDealt);
                targetCoroutines.Add(pScript1.HealText(healed));
            }
        }

        // 목적지(impactPos)에 적이 있어 그 직전 칸(adjacentPos)까지만 이동 — "부딪혀서 멈춘" 펀치 이펙트 재생
        motionQueue.Enqueue(MovePieceWithAnim(attackerObj, bScript1, GetButtonScript(adjacentPos), 1f, "Move", bumpOnArrival: true));
        // 이동 스텝(PieceMoveCor)이 이동 방향으로 회전을 덮어써버리므로, 공격 애니메이션이 재생되기
        // 직전(이동 완료 후)에 공격자/피격자를 다시 서로 마주보게 회전시킨다.
        motionQueue.Enqueue(RotateForMoveAttack(pScript1, pScript2, adjacentPos, bScript2));
        motionQueue.Enqueue(PlayCasterAndTargetReaction(pScript1, attackTrigger, Parallel(targetCoroutines.ToArray()), attackRangeEffect));

        if (hpLeft <= 0)
        {
            ClearDeadPieceOccupancy(impactPos, pScript2);
            ApplyMoveOccupancy(GetButtonScript(adjacentPos), bScript2); // 공격자가 빈 자리까지 계속 이동
            motionQueue.Enqueue(pScript2.DeathCor());
            motionQueue.Enqueue(PieceMoveCor(attackerObj, GetButtonScript(adjacentPos), bScript2, 1f));
            TriggerOnKillEffect(impactPos, pScript1, cardEffect);
        }
        foreach (var (pos, p, splashHpLeft) in splashResults)
        {
            if (splashHpLeft <= 0)
            {
                ClearDeadPieceOccupancy(pos, p);
                motionQueue.Enqueue(p.DeathCor());
                TriggerOnKillEffect(hpLeft <= 0 ? impactPos : adjacentPos, pScript1, cardEffect);
            }
        }

        // 반격(가시 등)은 본체 공격과 별개의 시점에 일어나는 반응이라 자체 Parallel로 한 항목만 큐에 넣는다 —
        // TriggerAnim을 즉시(동기) 호출하던 예전 방식은 애니메이션이 큐 순번을 기다리는 사운드보다 먼저
        // 재생돼 타이밍이 어긋났다. TriggerAnimCor로 바꿔 애니메이션과 DamageText(사운드)가 같은 큐 항목
        // 안에서 함께 시작되게 한다.
        int counterDmg = pScript2.TriggerReceiveMoveAttack(pScript1);
        if (counterDmg > 0)
        {
            int attackerHp = pScript1.GetDamage(counterDmg);
            var counterReaction = new List<IEnumerator>
            {
                TriggerAnimCor(pScript1, attackerHp <= 0 ? "Die" : "Hit", 0.3f, false),
                pScript1.DamageText(counterDmg, isCounter: true)
            };
            if (attackerHp <= 0) counterReaction.Add(pScript1.PieceDeathSound());
            motionQueue.Enqueue(Parallel(counterReaction.ToArray()));
            if (attackerHp <= 0)
            {
                // attackerPos(이동 전 위치)가 아니라 공격자의 최종 위치(finalAttackerPos) 기준으로 지워야
                // 한다 — 점유가 이미 이동 애니메이션보다 앞서 확정돼 있으므로(위 Step A/B).
                ClearDeadPieceOccupancy(finalAttackerPos, pScript1);
                motionQueue.Enqueue(pScript1.DeathCor());
            }
        }

        StartMotionQueue();
        return true;
    }

    // 이동 스텝(PieceMoveCor)이 이동 방향으로 덮어쓴 회전을, 공격 애니메이션이 재생되기 직전(이동
    // 완료 후)에 다시 공격자↔피격자가 서로 마주보는 방향으로 되돌린다. attackerPos는 이동이 끝난
    // 실제 도착 칸(adjacentPos) — bScript1은 이동 후엔 빈 칸이라 위치 기준으로 쓸 수 없다.
    IEnumerator RotateForMoveAttack(Piece attacker, Piece defender, Vector2Int attackerPos, Button defenderButton)
    {
        Vector3 attackerWorldPos = GetButtonScript(attackerPos).Piecelocation;
        defender.transform.rotation = Quaternion.LookRotation(attackerWorldPos - defenderButton.Piecelocation);
        attacker.transform.rotation = Quaternion.LookRotation(defenderButton.Piecelocation - attackerWorldPos);
        yield break;
    }

    // 처치가 확정된 시점에 호출: OnKill 유물을 발동시키고, cardEffect에 onKillEffect가 설정돼 있으면
    // 시전자를 대상으로 그 효과도 실행한다(예: 처치 시 ColDamageUp). 기존 4개 호출 지점을 그대로 재사용.
    void TriggerOnKillEffect(Vector2Int casterPos, Piece caster, CardEffect cardEffect)
    {
        TriggerRelicsOnKill(caster);
        if (caster == null || cardEffect?.onKillEffect == null) return;
        ExecuteCardEffectOnPiece(casterPos, caster, cardEffect.onKillEffect);
    }

    void AttackPiece(Vector2Int pos1, Vector2Int pos2, int dmg, CardEffect cardEffect = null)
    {
        // pos1 == pos2: 자기 자신에게 거는 예약 Damage 효과(예: StatusEffectType.TurnDamageStart/End의
        // DoT 틱 — CreateStatusEffect가 targetlogic=self로 만들어 EnqueueScheduledEffect를 통해 자기
        // 자신을 targetPos로 넘김). 캐스터와 피격자가 같은 기물이므로 회전/공격 애니메이션 없이
        // 피격자의 Hit/Die 반응만 재생한다.
        if (pos1 == pos2)
        {
            Piece target = GetButtonScript(pos2).GetPieceScript();
            if (target == null) { StartMotionQueue(); return; }

            int hpLeftDirect = target.GetDamage(dmg);
            if (target.teamID == 0) playerDamagedThisTurn = true;

            var directReaction = new List<IEnumerator>
            {
                TriggerAnimCor(target, hpLeftDirect <= 0 ? "Die" : "Hit", 0.3f, false),
                target.DamageText(dmg)
            };
            StatusEffect directStatusEffect = ApplyStatusEffect(target, cardEffect); // 즉시 적용
            if (directStatusEffect != null)
                directReaction.Add(target.StatusTextReaction(directStatusEffect.DisplayName, directStatusEffect.IsBuff, directStatusEffect.EffectColor));
            if (hpLeftDirect <= 0) directReaction.Add(target.PieceDeathSound());
            motionQueue.Enqueue(Parallel(directReaction.ToArray()));
            if (hpLeftDirect <= 0)
            {
                ClearDeadPieceOccupancy(pos2, target);
                motionQueue.Enqueue(target.DeathCor());
            }
            StartMotionQueue();
            return;
        }

        Piece pScript1 = GetButtonScript(pos1).GetPieceScript();
        if (pScript1 == null) { StartMotionQueue(); return; }

        Piece pScript2 = GetButtonScript(pos2).GetPieceScript();
        if (pScript2 == null)
        {
            // 피격자가 없는 헛스윙: 시전자 공격 애니메이션만 재생하고 실제 효과는 적용하지 않음 (카드는 정상 소모됨)
            // 회전은 TriggerAnimCor에 넘기는 attackTargetPos를 통해 PlayAttackPunchFallback이 처리한다.
            motionQueue.Enqueue(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect, attackTargetPos: pos2));
            StartMotionQueue();
            return;
        }

        int hpLeft = pScript2.GetDamage(dmg);
        if (pScript2.teamID == 0) playerDamagedThisTurn = true;

        pScript2.transform.rotation = Quaternion.LookRotation(GetButtonScript(pos1).Piecelocation - GetButtonScript(pos2).Piecelocation);
        pScript1.transform.rotation = Quaternion.LookRotation(GetButtonScript(pos2).Piecelocation - GetButtonScript(pos1).Piecelocation);

        var extra = new List<IEnumerator> { pScript2.DamageText(dmg) };
        StatusEffect statusEffect = ApplyStatusEffect(pScript2, cardEffect); // 즉시 적용
        if (statusEffect != null)
            extra.Add(pScript2.StatusTextReaction(statusEffect.DisplayName, statusEffect.IsBuff, statusEffect.EffectColor));
        if (cardEffect != null && cardEffect.healOnHit && dmg > 0)
        {
            int healed = pScript1.GetHeal(dmg);
            extra.Add(pScript1.HealText(healed));
        }
        motionQueue.Enqueue(PieceAttackCor(pScript1, pScript2, cardEffect?.animTrigger, hpLeft <= 0 ? "Die" : "Hit", cardEffect, extra));
        if (hpLeft <= 0)
        {
            ClearDeadPieceOccupancy(pos2, pScript2);
            motionQueue.Enqueue(pScript2.DeathCor());
            TriggerOnKillEffect(pos1, pScript1, cardEffect);
        }

        StartMotionQueue();
    }

    void HealPiece(Vector2Int pos1, Vector2Int pos2, int dmg, CardEffect cardEffect = null)
    {
        Piece pScript1 = GetButtonScript(pos1).GetPieceScript();
        if (pScript1 == null) { StartMotionQueue(); return; }

        Piece pScript2 = GetButtonScript(pos2).GetPieceScript();
        if (pScript2 == null)
        {
            // 대상이 없는 헛스윙: 시전자 애니메이션만 재생
            motionQueue.Enqueue(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect));
            StartMotionQueue();
            return;
        }

        int healed = pScript2.GetHeal(dmg);

        var healExtra = new List<IEnumerator> { pScript2.HealText(healed) };
        StatusEffect healStatusEffect = ApplyStatusEffect(pScript2, cardEffect); // 즉시 적용
        if (healStatusEffect != null)
            healExtra.Add(pScript2.StatusTextReaction(healStatusEffect.DisplayName, healStatusEffect.IsBuff, healStatusEffect.EffectColor));
        motionQueue.Enqueue(PieceHealCor(pScript1, pScript2, cardEffect, healExtra));

        StartMotionQueue();
    }

    // 피격자 반응(Hit/Die + DamageText)만 재생 — 캐스터=피격자라 별도 캐스터 애니메이션은 없음.
    void SelfDamagePiece(Vector2Int casterPos, int dmg)
    {
        if (casterPos.x < 0 || casterPos.y < 0) return;
        Piece p = GetButtonScript(casterPos).GetPieceScript();
        if (p == null) return;
        int hpLeft = p.GetDamage(dmg);
        if (p.teamID == 0) playerDamagedThisTurn = true;

        var selfDamageReaction = new List<IEnumerator>
        {
            TriggerAnimCor(p, hpLeft <= 0 ? "Die" : "Hit", 0.3f, false),
            p.DamageText(dmg)
        };
        if (hpLeft <= 0) selfDamageReaction.Add(p.PieceDeathSound());
        motionQueue.Enqueue(Parallel(selfDamageReaction.ToArray()));
        if (hpLeft <= 0)
        {
            ClearDeadPieceOccupancy(casterPos, p);
            motionQueue.Enqueue(p.DeathCor());
        }
        StartMotionQueue();
    }

    void AreaAttackPiece(Vector2Int casterPos, List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        if (casterPos.x < 0 || casterPos.y < 0 || targets.Count == 0) return;

        Button casterButton = GetButtonScript(casterPos);
        if (casterButton.GetPiece() == null) return;

        Piece caster = casterButton.GetPieceScript();

        if (targets.Count > 0)
            casterButton.GetPiece().transform.rotation = Quaternion.LookRotation(GetButtonScript(targets[0]).Piecelocation - casterButton.Piecelocation);

        int totalHeal = 0;
        var hitTargets = new List<(Piece piece, bool died)>();
        var textCoroutines = new List<IEnumerator>();
        var deathCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetDamage(dmg);
            if (p.teamID == 0) playerDamagedThisTurn = true;
            p.transform.rotation = Quaternion.LookRotation(casterButton.Piecelocation - GetButtonScript(pos).Piecelocation);
            bool died = hpLeft <= 0;
            hitTargets.Add((p, died));
            textCoroutines.Add(p.DamageText(dmg));
            StatusEffect areaStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (areaStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(areaStatusEffect.DisplayName, areaStatusEffect.IsBuff, areaStatusEffect.EffectColor));
            if (died)
            {
                ClearDeadPieceOccupancy(pos, p);
                deathCoroutines.Add(p.DeathCor());
                TriggerOnKillEffect(casterPos, caster, cardEffect);
            }
            if (cardEffect != null && cardEffect.healOnHit)
                totalHeal += dmg;
        }

        if (totalHeal > 0 && caster != null)
        {
            int healed = caster.GetHeal(totalHeal);
            textCoroutines.Add(caster.HealText(healed));
        }

        motionQueue.Enqueue(PieceAreaAttackCor(caster, hitTargets, cardEffect?.animTrigger, cardEffect, textCoroutines));
        foreach (var d in deathCoroutines)
            motionQueue.Enqueue(d);

        StartMotionQueue();
    }

    // 캐스터(시전자) 없이 여러 기물에게 동시에 피해를 준다. 카드가 아니라 DamageAllAllies처럼 특정
    // 기물이 아닌 보드/이벤트 자체에서 발생하는, 애초에 캐스터 개념이 없는 전역 효과 전용.
    void AreaAttackPiece(List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        if (targets.Count == 0) return;

        var hitTargets = new List<(Piece piece, bool died)>();
        var textCoroutines = new List<IEnumerator>();
        var deathCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetDamage(dmg);
            if (p.teamID == 0) playerDamagedThisTurn = true;
            bool died = hpLeft <= 0;
            hitTargets.Add((p, died));
            textCoroutines.Add(p.DamageText(dmg));
            StatusEffect areaStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (areaStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(areaStatusEffect.DisplayName, areaStatusEffect.IsBuff, areaStatusEffect.EffectColor));
            if (died)
            {
                ClearDeadPieceOccupancy(pos, p);
                deathCoroutines.Add(p.DeathCor());
            }
        }

        motionQueue.Enqueue(PieceAreaAttackCor(null, hitTargets, cardEffect?.animTrigger, cardEffect, textCoroutines));
        foreach (var d in deathCoroutines)
            motionQueue.Enqueue(d);

        StartMotionQueue();
    }

    void AreaShieldPiece(List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        Piece caster = casterBtn.GetPieceScript();
        var shieldedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int shieldAfter = p.GetShield(dmg);
            shieldedPieces.Add(p);
            textCoroutines.Add(p.ShieldText(dmg));
            textCoroutines.Add(p.ShieldVisualOn(shieldAfter));
            StatusEffect shieldStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (shieldStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(shieldStatusEffect.DisplayName, shieldStatusEffect.IsBuff, shieldStatusEffect.EffectColor));
        }

        motionQueue.Enqueue(PieceAreaShieldCor(caster, shieldedPieces, cardEffect?.animTrigger, cardEffect, textCoroutines));

        StartMotionQueue();
    }

    void AreaHealPiece(List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        Piece caster = casterBtn.GetPieceScript();
        var healedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int healed = p.GetHeal(dmg);
            healedPieces.Add(p);
            textCoroutines.Add(p.HealText(healed));
            StatusEffect healAreaStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (healAreaStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(healAreaStatusEffect.DisplayName, healAreaStatusEffect.IsBuff, healAreaStatusEffect.EffectColor));
        }

        motionQueue.Enqueue(PieceAreaHealCor(caster, healedPieces, cardEffect?.animTrigger, cardEffect, textCoroutines));

        StartMotionQueue();
    }

    void ShieldPiece(Vector2Int pos1, Vector2Int pos2, int dmg, CardEffect cardEffect = null)
    {
        Piece pScript1 = GetButtonScript(pos1).GetPieceScript();
        if (pScript1 == null) { StartMotionQueue(); return; }

        Piece pScript2 = GetButtonScript(pos2).GetPieceScript();
        if (pScript2 == null)
        {
            // 대상이 없는 헛스윙: 시전자 애니메이션만 재생
            motionQueue.Enqueue(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect));
            StartMotionQueue();
            return;
        }

        int resultingShield = pScript2.GetShield(dmg);

        var shieldExtra = new List<IEnumerator> { pScript2.ShieldText(dmg), pScript2.ShieldVisualOn(resultingShield) };
        StatusEffect shieldPieceStatusEffect = ApplyStatusEffect(pScript2, cardEffect); // 즉시 적용
        if (shieldPieceStatusEffect != null)
            shieldExtra.Add(pScript2.StatusTextReaction(shieldPieceStatusEffect.DisplayName, shieldPieceStatusEffect.IsBuff, shieldPieceStatusEffect.EffectColor));
        motionQueue.Enqueue(PieceShieldCor(pScript1, pScript2, cardEffect, shieldExtra));

        StartMotionQueue();
    }
}
