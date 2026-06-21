using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    void MovePiece(Vector2 pos1, Vector2 pos2, CardEffect cardEffect = null)
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
                    && MoveAttack(p1, p2, button1script, button2script);
                if (!attacked && IsLockedCasterActive())
                    lockedCaster = pos1; // 아군 충돌, 또는 이동공격 도착 칸이 없어 실패: 이동 실패, 원래 위치로 복구
            }
            else
            {
                motionQueue.Enqueue(MovePieceWithAnim(button1script, button2script, 1f, cardEffect?.animTrigger, cardEffect));
                StartMotionQueue();
            }
        }

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }

    // 이동공격 도착 칸을 찾지 못하면(후보 전부 막힘) false를 반환해 호출부가 일반 이동 실패와 동일하게 처리하게 함.
    bool MoveAttack(Piece pScript1, Piece pScript2, Button bScript1, Button bScript2)
    {
        Vector2 adjacentPos = GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation());
        if (adjacentPos.x < 0) return false; // 도착할 칸이 없음: 공격 취소, 이동 실패로 처리

        int dmg = pScript1.colDamage;
        pScript2.gameObject.transform.rotation = Quaternion.LookRotation(bScript1.Piecelocation - bScript2.Piecelocation);
        bScript1.GetPiece().transform.rotation = Quaternion.LookRotation(bScript2.Piecelocation - bScript1.Piecelocation);

        Vector2 attackerPos = bScript1.GetLocation();
        Vector2 impactPos = bScript2.GetLocation();

        // DirectionalAttackCard와 동일하게, 공격자→대상 방향으로 moveAttackRange를 회전
        // 공격은 이동 후 adjacentPos에서 일어나므로, 방향도 adjacentPos 기준으로 계산해야 함
        Vector2 attackDir = GetSnappedDirection(adjacentPos, impactPos, true);
        List<Vector2> moveAttackOffsets = RotateOffsets(pScript1.GetMoveAttackRange(), attackDir);
        bool isAreaAttack = !(moveAttackOffsets.Count == 1 && moveAttackOffsets[0] == Vector2.zero);
        string attackTrigger = isAreaAttack ? "AreaAttack" : "Attack";

        // TriggerAnimCor도 같은 방향으로 회전해서 표시하도록 Directional8 + currentHoverDirection 재사용
        currentHoverDirection = attackDir;
        CardEffect attackRangeEffect = new CardEffect(Board.BoardMode.command, EffectType.Damage, dmg, TargetLogic.AllEnemiesInRange,
            pScript1.MoveAttackRangeInfoSO, false, AreaTargetMode.Directional8)
            { animTrigger = attackTrigger };

        // 주 타겟(pScript2) 데미지 적용
        int hpLeft = pScript2.GetDamage(dmg);
        if (pScript2.teamID == 0) playerDamagedThisTurn = true;

        // moveAttackRange 내 나머지 적들 수집 + 데미지 적용 (이동 후 도착 위치 기준, 주 타겟은 위에서 이미 처리했으니 제외)
        var splashResults = new List<(Vector2 pos, Piece piece, int hpLeft)>();
        if (isAreaAttack)
        {
            foreach (Vector2 offset in moveAttackOffsets)
            {
                Vector2 pos = adjacentPos + offset;
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

        // 실제 도착 위치로 lockedCaster 수정: 적 생존 시 adjacentPos, 사망 시 impactPos
        if (IsLockedCasterActive())
            lockedCaster = hpLeft <= 0 ? impactPos : adjacentPos;

        // 공격자 + 모든 타겟(주 타겟 + 스플래시)의 트리거/텍스트를 같은 Parallel 그룹으로 묶어서 동시에 재생
        var animCoroutines = new List<IEnumerator>
        {
            TriggerAnimCor(pScript1, attackTrigger, cardEffect: attackRangeEffect),
            TriggerAnimCor(pScript2, hpLeft <= 0 ? "Die" : "Hit", 0.3f, false),
            pScript2.DamageText(dmg)
        };
        foreach (var (pos, p, splashHpLeft) in splashResults)
        {
            animCoroutines.Add(TriggerAnimCor(p, splashHpLeft <= 0 ? "Die" : "Hit", 0.3f, false));
            animCoroutines.Add(p.DamageText(dmg));
        }

        motionQueue.Enqueue(MovePieceWithAnim(bScript1, GetButtonScript(adjacentPos), 1f, "Move"));
        motionQueue.Enqueue(Parallel(animCoroutines.ToArray()));

        if (hpLeft <= 0)
        {
            if (pScript2.teamID == 1) enemyPositions.Remove(impactPos);
            motionQueue.Enqueue(pScript2.DeathCor());
            motionQueue.Enqueue(PieceMoveCor(GetButtonScript(adjacentPos), bScript2, 1f));
        }
        foreach (var (pos, p, splashHpLeft) in splashResults)
        {
            if (splashHpLeft <= 0)
            {
                if (p.teamID == 1) enemyPositions.Remove(pos);
                motionQueue.Enqueue(p.DeathCor());
            }
        }

        int counterDmg = pScript2.TriggerReceiveMoveAttack(pScript1);
        if (counterDmg > 0)
        {
            int attackerHp = pScript1.GetDamage(counterDmg);
            pScript1.TriggerAnim(attackerHp <= 0 ? "Die" : "Hit");
            motionQueue.Enqueue(pScript1.DamageText(counterDmg));
            if (attackerHp <= 0)
            {
                if (pScript1.teamID == 1) enemyPositions.Remove(attackerPos);
                motionQueue.Enqueue(pScript1.DeathCor());
            }
        }

        if (currentActiveCard != null && currentActiveCard.shieldOnMoveAttack && currentActiveCard.moveAttackShieldAmount > 0)
        {
            pScript1.GetShield(currentActiveCard.moveAttackShieldAmount);
            motionQueue.Enqueue(pScript1.ShieldText(currentActiveCard.moveAttackShieldAmount));
        }

        StartMotionQueue();
        return true;
    }

    // pos1의 시전자와 pos2의 대상 기물을 가져온다. 둘 다 있어야 true.
    bool TryGetCasterAndTarget(Vector2 pos1, Vector2 pos2, out Piece caster, out Piece target)
    {
        caster = GetButtonScript(pos1).GetPieceScript();
        target = GetButtonScript(pos2).GetPieceScript();
        return caster != null && target != null;
    }

    void AttackPiece(Vector2 pos1, Vector2 pos2, int dmg, CardEffect cardEffect = null)
    {
        if (TryGetCasterAndTarget(pos1, pos2, out Piece pScript1, out Piece pScript2))
        {
            int hpLeft = pScript2.GetDamage(dmg);
            if (pScript2.teamID == 0) playerDamagedThisTurn = true;

            pScript2.transform.rotation = Quaternion.LookRotation(GetButtonScript(pos1).Piecelocation - GetButtonScript(pos2).Piecelocation);
            pScript1.transform.rotation = Quaternion.LookRotation(GetButtonScript(pos2).Piecelocation - GetButtonScript(pos1).Piecelocation);

            motionQueue.Enqueue(PieceAttackCor(pScript1, pScript2, cardEffect?.animTrigger, hpLeft <= 0 ? "Die" : "Hit", cardEffect,
                new List<IEnumerator> { pScript2.DamageText(dmg) }));
            if (hpLeft <= 0)
            {
                if (pScript2.teamID == 1) enemyPositions.Remove(pos2);
                motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartMotionQueue();
    }

    void HealPiece(Vector2 pos1, Vector2 pos2, int dmg, CardEffect cardEffect = null)
    {
        if (TryGetCasterAndTarget(pos1, pos2, out Piece pScript1, out Piece pScript2))
        {
            int hpLeft = pScript2.GetHeal(dmg);

            motionQueue.Enqueue(PieceHealCor(pScript1, pScript2, cardEffect,
                new List<IEnumerator> { pScript2.HealText(dmg) }));
            if (hpLeft <= 0)
                motionQueue.Enqueue(pScript2.DeathCor());
        }
        StartMotionQueue();
    }

    void SelfDamagePiece(Vector2 casterPos, int dmg, CardEffect cardEffect = null)
    {
        if (casterPos.x < 0 || casterPos.y < 0) return;
        Piece p = GetButtonScript(casterPos).GetPieceScript();
        if (p == null) return;
        int hpLeft = p.GetDamage(dmg);
        if (p.teamID == 0) playerDamagedThisTurn = true;

        motionQueue.Enqueue(Parallel(
            TriggerAnimCor(p, cardEffect?.animTrigger, cardEffect: cardEffect),
            TriggerAnimCor(p, hpLeft <= 0 ? "Die" : "Hit", 0.3f, false),
            p.DamageText(dmg)));
        if (hpLeft <= 0)
        {
            if (p.teamID == 1) enemyPositions.Remove(casterPos);
            motionQueue.Enqueue(p.DeathCor());
        }
        StartMotionQueue();
    }

    void AreaAttackPiece(Vector2 casterPos, List<Vector2> targets, int dmg, CardEffect cardEffect = null)
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

        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetDamage(dmg);
            if (p.teamID == 0) playerDamagedThisTurn = true;
            p.transform.rotation = Quaternion.LookRotation(casterButton.Piecelocation - GetButtonScript(pos).Piecelocation);
            bool died = hpLeft <= 0;
            hitTargets.Add((p, died));
            textCoroutines.Add(p.DamageText(dmg));
            if (died)
            {
                if (p.teamID == 1) enemyPositions.Remove(pos);
                deathCoroutines.Add(p.DeathCor());
            }
            if (cardEffect != null && cardEffect.healOnHit > 0)
                totalHeal += cardEffect.healOnHit;
        }

        motionQueue.Enqueue(PieceAreaAttackCor(caster, hitTargets, cardEffect?.animTrigger, cardEffect, textCoroutines));
        foreach (var d in deathCoroutines)
            motionQueue.Enqueue(d);

        if (totalHeal > 0 && caster != null)
        {
            caster.GetHeal(totalHeal);
            motionQueue.Enqueue(caster.HealText(totalHeal));
        }

        StartMotionQueue();
    }

    void AreaShieldPiece(List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        Piece caster = casterBtn.GetPieceScript();
        var shieldedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            p.GetShield(dmg);
            shieldedPieces.Add(p);
            textCoroutines.Add(p.ShieldText(dmg));
        }

        motionQueue.Enqueue(PieceAreaShieldCor(caster, shieldedPieces, cardEffect?.animTrigger, cardEffect, textCoroutines));

        StartMotionQueue();
    }

    void AreaHealPiece(List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        Piece caster = casterBtn.GetPieceScript();
        var healedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            p.GetHeal(dmg);
            healedPieces.Add(p);
            textCoroutines.Add(p.HealText(dmg));
        }

        motionQueue.Enqueue(PieceAreaHealCor(caster, healedPieces, cardEffect?.animTrigger, cardEffect, textCoroutines));

        StartMotionQueue();
    }

    void ShieldPiece(Vector2 pos1, Vector2 pos2, int dmg, CardEffect cardEffect = null)
    {
        if (TryGetCasterAndTarget(pos1, pos2, out Piece pScript1, out Piece pScript2))
        {
            int hpLeft = pScript2.GetShield(dmg);

            motionQueue.Enqueue(PieceShieldCor(pScript1, pScript2, cardEffect,
                new List<IEnumerator> { pScript2.ShieldText(dmg) }));
            if (hpLeft <= 0)
                motionQueue.Enqueue(pScript2.DeathCor());
        }
        StartMotionQueue();
    }
}
