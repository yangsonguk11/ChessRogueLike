using UnityEngine;

public partial class Board
{
    void MovePiece(Vector2 pos1, Vector2 pos2)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece p1 = button1script.GetPiece().GetComponent<Piece>();
                Piece p2 = piece2.GetComponent<Piece>();
                if (p1.teamID != p2.teamID)
                    MoveAttack(p1, p2, button1script, button2script);
            }
            else
            {
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
        Debug.Log(hpLeft);

        motionQueue.Enqueue(MoveAdjacent(bScript1, bScript2, 1f));
        motionQueue.Enqueue(pScript2.DamageText(dmg));
        if (hpLeft <= 0)
        {
            motionQueue.Enqueue(pScript2.DeathCor());
            motionQueue.Enqueue(PieceMoveCor(GetButtonScript(GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation())), bScript2, 1f));
        }
        StartCoroutine(ProcessQueue());
    }

    void AttackPiece(Vector2 pos1, Vector2 pos2, int dmg)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece pScript2 = piece2.GetComponent<Piece>();
                int hpLeft = pScript2.GetDamage(dmg, AttackType.NormalAttack);

                motionQueue.Enqueue(PieceAttackCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.DamageText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartCoroutine(ProcessQueue());
    }

    void HealPiece(Vector2 pos1, Vector2 pos2, int dmg)
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

                motionQueue.Enqueue(PieceHealCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.HealText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartCoroutine(ProcessQueue());
    }

    void ShieldPiece(Vector2 pos1, Vector2 pos2, int dmg)
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

                motionQueue.Enqueue(PieceShieldCor(button1script, button2script, 1f));
                motionQueue.Enqueue(pScript2.ShieldText(dmg));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartCoroutine(ProcessQueue());
    }
}
