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
            playerMovedThisTurn = true;

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
                movingPiece?.TriggerAnim("Move");
                if (cardEffect?.animationClip != null)
                    motionQueue.Enqueue(PieceCustomAnimCor(button1script.GetPiece(), cardEffect.animationClip));
                else
                    motionQueue.Enqueue(PieceMoveCor(button1script, button2script, 1f));
                StartCoroutine(ProcessQueue());
            }
        }

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }

    void MoveAttack(Piece pScript1, Piece pScript2, Button bScript1, Button bScript2)
    {
        int dmg = pScript1.colDamage;
        int hpLeft = pScript2.GetDamage(dmg, AttackType.MoveAttack);
        pScript1.TriggerAnim("Attack");
        pScript2.gameObject.transform.rotation = Quaternion.LookRotation(bScript1.Piecelocation - bScript2.Piecelocation);
        pScript2.TriggerAnim(hpLeft <= 0 ? "Die" : "Hit");
        if (pScript2.teamID == 0) playerDamagedThisTurn = true;
        Debug.Log(hpLeft);

        Vector2 adjacentPos = GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation());

        // 실제 도착 위치로 lockedCaster 수정: 적 생존 시 adjacentPos, 사망 시 bScript2
        if (IsLockedCasterActive())
            lockedCaster = hpLeft <= 0 ? bScript2.GetLocation() : adjacentPos;

        motionQueue.Enqueue(MoveAdjacent(bScript1, bScript2, 1f));
        motionQueue.Enqueue(pScript2.DamageText(dmg));
        if (hpLeft <= 0)
        {
            if (pScript2.teamID == 1) enemyPositions.Remove(bScript2.GetLocation());
            motionQueue.Enqueue(pScript2.DeathCor());
            motionQueue.Enqueue(PieceMoveCor(GetButtonScript(adjacentPos), bScript2, 1f));
        }

        int counterDmg = pScript2.TriggerReceiveMoveAttack(pScript1);
        if (counterDmg > 0)
        {
            int attackerHp = pScript1.GetDamage(counterDmg, AttackType.MoveAttack);
            pScript1.TriggerAnim(attackerHp <= 0 ? "Die" : "Hit");
            motionQueue.Enqueue(pScript1.DamageText(counterDmg));
            if (attackerHp <= 0)
            {
                if (pScript1.teamID == 1) enemyPositions.Remove(bScript1.GetLocation());
                motionQueue.Enqueue(pScript1.DeathCor());
            }
        }

        if (currentActiveCard != null && currentActiveCard.shieldOnMoveAttack && currentActiveCard.moveAttackShieldAmount > 0)
        {
            pScript1.GetShield(currentActiveCard.moveAttackShieldAmount, AttackType.MoveAttack);
            motionQueue.Enqueue(pScript1.ShieldText(currentActiveCard.moveAttackShieldAmount));
        }

        StartCoroutine(ProcessQueue());
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

                if (cardEffect?.animationClip != null)
                    motionQueue.Enqueue(PieceCustomAnimCor(button1script.GetPiece(), cardEffect.animationClip));
                else if (pScript1 != null && pScript1.HasAnimator())
                {
                    button1script.GetPiece().transform.rotation = Quaternion.LookRotation(button2script.Piecelocation - button1script.Piecelocation);
                    pScript1.TriggerAnim("Attack");
                }
                else
                    motionQueue.Enqueue(PieceAttackCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.DamageText(dmg));
                if (hpLeft <= 0)
                {
                    if (pScript2.teamID == 1) enemyPositions.Remove(pos2);
                    motionQueue.Enqueue(pScript2.DeathCor());
                }
            }
        }
        StartCoroutine(ProcessQueue());
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

                if (cardEffect?.animationClip != null)
                    motionQueue.Enqueue(PieceCustomAnimCor(button1script.GetPiece(), cardEffect.animationClip));
                else
                    motionQueue.Enqueue(PieceHealCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.HealText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartCoroutine(ProcessQueue());
    }

    void SelfDamagePiece(Vector2 casterPos, int dmg)
    {
        if (casterPos.x < 0 || casterPos.y < 0) return;
        Piece p = GetButtonScript(casterPos).GetPieceScript();
        if (p == null) return;
        int hpLeft = p.GetDamage(dmg, AttackType.NormalAttack);
        if (p.teamID == 0) playerDamagedThisTurn = true;
        p.TriggerAnim(hpLeft <= 0 ? "Die" : "Hit");
        motionQueue.Enqueue(p.DamageText(dmg));
        if (hpLeft <= 0)
        {
            if (p.teamID == 1) enemyPositions.Remove(casterPos);
            motionQueue.Enqueue(p.DeathCor());
        }
        StartCoroutine(ProcessQueue());
    }

    void AreaAttackPiece(Vector2 casterPos, List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        if (casterPos.x < 0 || casterPos.y < 0 || targets.Count == 0) return;

        Button casterButton = GetButtonScript(casterPos);
        if (casterButton.GetPiece() == null) return;

        Piece caster = casterButton.GetPieceScript();

        if (cardEffect?.animationClip != null)
            motionQueue.Enqueue(PieceCustomAnimCor(casterButton.GetPiece(), cardEffect.animationClip));
        else if (caster != null && caster.HasAnimator())
            caster.TriggerAnim("Attack");
        else
            motionQueue.Enqueue(PieceAreaAttackCor(casterButton, 1f));

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

        StartCoroutine(ProcessQueue());
    }

    void AreaShieldPiece(List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            p.GetShield(dmg, AttackType.NormalAttack);
            p.TriggerAnim("Shield");
            if (cardEffect?.animationClip != null)
                motionQueue.Enqueue(PieceCustomAnimCor(casterBtn.GetPiece(), cardEffect.animationClip));
            else
                motionQueue.Enqueue(PieceShieldCor(casterBtn, GetButtonScript(pos), 1f));
            motionQueue.Enqueue(p.ShieldText(dmg));
        }
        StartCoroutine(ProcessQueue());
    }

    void AreaHealPiece(List<Vector2> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        foreach (Vector2 pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetHeal(dmg, AttackType.NormalAttack);
            p.TriggerAnim("Heal");
            if (cardEffect?.animationClip != null)
                motionQueue.Enqueue(PieceCustomAnimCor(casterBtn.GetPiece(), cardEffect.animationClip));
            else
                motionQueue.Enqueue(PieceHealCor(casterBtn, GetButtonScript(pos), 1f));
            motionQueue.Enqueue(p.HealText(dmg));
        }
        StartCoroutine(ProcessQueue());
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

                if (cardEffect?.animationClip != null)
                    motionQueue.Enqueue(PieceCustomAnimCor(button1script.GetPiece(), cardEffect.animationClip));
                else
                    motionQueue.Enqueue(PieceShieldCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.ShieldText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartCoroutine(ProcessQueue());
    }
}
