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
                if (p1.teamID != p2.teamID && !(cardEffect?.noMoveAttack ?? false))
                    MoveAttack(p1, p2, button1script, button2script);
                else if (IsLockedCasterActive())
                    lockedCaster = pos1; // 아군 충돌: 이동 실패, 원래 위치로 복구
            }
            else
            {
                motionQueue.Enqueue(MovePieceWithAnim(button1script, button2script, 1f, cardEffect?.animTrigger));
                StartMotionQueue();
            }
        }

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }

    void MoveAttack(Piece pScript1, Piece pScript2, Button bScript1, Button bScript2)
    {
        int dmg = pScript1.colDamage;
        pScript2.gameObject.transform.rotation = Quaternion.LookRotation(bScript1.Piecelocation - bScript2.Piecelocation);
        bScript1.GetPiece().transform.rotation = Quaternion.LookRotation(bScript2.Piecelocation - bScript1.Piecelocation);

        Vector2 adjacentPos = GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation());
        Vector2 attackerPos = bScript1.GetLocation();
        Vector2 impactPos = bScript2.GetLocation();

        // DirectionalAttackCard와 동일하게, 공격자→대상 방향으로 moveAttackRange를 회전
        Vector2 attackDir = GetSnappedDirection(attackerPos, impactPos, true);
        List<Vector2> moveAttackOffsets = RotateOffsets(pScript1.GetMoveAttackRange(), attackDir);
        bool isAreaAttack = !(moveAttackOffsets.Count == 1 && moveAttackOffsets[0] == Vector2.zero);
        string attackTrigger = isAreaAttack ? "AreaAttack" : "Attack";

        // TriggerAnimCor도 같은 방향으로 회전해서 표시하도록 Directional8 + currentHoverDirection 재사용
        currentHoverDirection = attackDir;
        CardEffect attackRangeEffect = new CardEffect(Board.BoardMode.command, EffectType.Damage, dmg, TargetLogic.AllEnemiesInRange,
            pScript1.MoveAttackRangeInfoSO, false, AreaTargetMode.Directional8)
            { animTrigger = attackTrigger };

        // 주 타겟(pScript2) 데미지 적용
        int hpLeft = pScript2.GetDamage(dmg, AttackType.MoveAttack);
        if (pScript2.teamID == 0) playerDamagedThisTurn = true;

        // moveAttackRange 내 나머지 적들 수집 + 데미지 적용 (공격자 위치 기준, 주 타겟은 위에서 이미 처리했으니 제외)
        var splashResults = new List<(Vector2 pos, Piece piece, int hpLeft)>();
        if (isAreaAttack)
        {
            foreach (Vector2 offset in moveAttackOffsets)
            {
                Vector2 pos = attackerPos + offset;
                if (pos == impactPos) continue;
                if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p == null || p.teamID == pScript1.teamID) continue;

                int splashHpLeft = p.GetDamage(dmg, AttackType.MoveAttack);
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

        motionQueue.Enqueue(MoveAdjacent(bScript1, bScript2, 1f, "Move"));
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
            int attackerHp = pScript1.GetDamage(counterDmg, AttackType.MoveAttack);
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
            pScript1.GetShield(currentActiveCard.moveAttackShieldAmount, AttackType.MoveAttack);
            motionQueue.Enqueue(pScript1.ShieldText(currentActiveCard.moveAttackShieldAmount));
        }

        StartMotionQueue();
    }

    void AttackPiece(Vector2 pos1, Vector2 pos2, int dmg, CardEffect cardEffect = null)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece pScript1 = button1script.GetPieceScript();
                Piece pScript2 = piece2.GetComponent<Piece>();
                int hpLeft = pScript2.GetDamage(dmg, AttackType.NormalAttack);
                if (pScript2.teamID == 0) playerDamagedThisTurn = true;

                piece2.transform.rotation = Quaternion.LookRotation(button1script.Piecelocation - button2script.Piecelocation);
                pScript2.TriggerAnim(hpLeft <= 0 ? "Die" : "Hit");

                if (cardEffect?.animTrigger != null)
                    button1script.GetPiece().transform.rotation = Quaternion.LookRotation(button2script.Piecelocation - button1script.Piecelocation);
                else
                    motionQueue.Enqueue(PieceAttackCor(button1script, button2script, 1f));
                StartCoroutine(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect));
                motionQueue.Enqueue(pScript2.DamageText(dmg));
                if (hpLeft <= 0)
                {
                    if (pScript2.teamID == 1) enemyPositions.Remove(pos2);
                    motionQueue.Enqueue(pScript2.DeathCor());
                }
            }
        }
        StartMotionQueue();
    }

    void HealPiece(Vector2 pos1, Vector2 pos2, int dmg, CardEffect cardEffect = null)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece pScript2 = piece2.GetComponent<Piece>();
                int hpLeft = pScript2.GetHeal(dmg, AttackType.NormalAttack);

                pScript2.TriggerAnim("Heal");

                if (cardEffect?.animTrigger == null)
                    motionQueue.Enqueue(PieceHealCor(button1script, button2script, 1f));
                StartCoroutine(TriggerAnimCor(button1script.GetPieceScript(), cardEffect?.animTrigger, cardEffect: cardEffect));
                motionQueue.Enqueue(pScript2.HealText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartMotionQueue();
    }

    void SelfDamagePiece(Vector2 casterPos, int dmg, CardEffect cardEffect = null)
    {
        if (casterPos.x < 0 || casterPos.y < 0) return;
        Piece p = GetButtonScript(casterPos).GetPieceScript();
        if (p == null) return;
        int hpLeft = p.GetDamage(dmg, AttackType.NormalAttack);
        if (p.teamID == 0) playerDamagedThisTurn = true;
        p.TriggerAnim(hpLeft <= 0 ? "Die" : "Hit");
        StartCoroutine(TriggerAnimCor(p, cardEffect?.animTrigger));
        motionQueue.Enqueue(p.DamageText(dmg));
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

        if (cardEffect?.animTrigger != null)
        {
            if (targets.Count > 0)
                casterButton.GetPiece().transform.rotation = Quaternion.LookRotation(GetButtonScript(targets[0]).Piecelocation - casterButton.Piecelocation);
        }
        else
            motionQueue.Enqueue(PieceAreaAttackCor(casterButton, 1f));
        StartCoroutine(TriggerAnimCor(caster, cardEffect?.animTrigger, cardEffect: cardEffect));

        int totalHeal = 0;

        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetDamage(dmg, AttackType.NormalAttack);
            if (p.teamID == 0) playerDamagedThisTurn = true;
            p.transform.rotation = Quaternion.LookRotation(casterButton.Piecelocation - GetButtonScript(pos).Piecelocation);
            p.TriggerAnim(hpLeft <= 0 ? "Die" : "Hit");
            motionQueue.Enqueue(p.DamageText(dmg));
            if (hpLeft <= 0)
            {
                if (p.teamID == 1) enemyPositions.Remove(pos);
                motionQueue.Enqueue(p.DeathCor());
            }
            if (cardEffect != null && cardEffect.healOnHit > 0)
                totalHeal += cardEffect.healOnHit;
        }

        if (totalHeal > 0 && caster != null)
        {
            caster.GetHeal(totalHeal, AttackType.NormalAttack);
            motionQueue.Enqueue(caster.HealText(totalHeal));
        }

        StartMotionQueue();
    }

    void AreaShieldPiece(List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        if (cardEffect?.animTrigger != null)
            casterBtn.GetPieceScript()?.TriggerAnim(cardEffect.animTrigger);
        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            p.GetShield(dmg, AttackType.NormalAttack);
            p.TriggerAnim("Shield");
            if (cardEffect?.animTrigger == null)
                motionQueue.Enqueue(PieceShieldCor(casterBtn, GetButtonScript(pos), 1f));
            motionQueue.Enqueue(p.ShieldText(dmg));
        }
        StartMotionQueue();
    }

    void AreaHealPiece(List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        StartCoroutine(TriggerAnimCor(casterBtn.GetPieceScript(), cardEffect?.animTrigger, cardEffect: cardEffect));
        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetHeal(dmg, AttackType.NormalAttack);
            p.TriggerAnim("Heal");
            if (cardEffect?.animTrigger == null)
                motionQueue.Enqueue(PieceHealCor(casterBtn, GetButtonScript(pos), 1f));
            motionQueue.Enqueue(p.HealText(dmg));
        }
        StartMotionQueue();
    }

    void ShieldPiece(Vector2 pos1, Vector2 pos2, int dmg, CardEffect cardEffect = null)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece pScript2 = piece2.GetComponent<Piece>();
                int hpLeft = pScript2.GetShield(dmg, AttackType.NormalAttack);

                pScript2.TriggerAnim("Shield");
                if (cardEffect?.animTrigger != null)
                    button1script.GetPieceScript()?.TriggerAnim(cardEffect.animTrigger);
                else
                    motionQueue.Enqueue(PieceShieldCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.ShieldText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartMotionQueue();
    }
}
