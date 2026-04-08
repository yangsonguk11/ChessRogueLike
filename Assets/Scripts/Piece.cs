using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Piece : MonoBehaviour
{
    [SerializeField] PieceInfo pieceInfo;
    public PieceCanvas pieceCanvas;

    public new string name;
    public int hp;
    public int maxhp;
    public int colDamage;
    public int teamID;
    public virtual void Awake()
    {
        name = pieceInfo.PieceName;
        hp = pieceInfo.MaxHp;
        maxhp = pieceInfo.MaxHp;
        colDamage = pieceInfo.ColDamage;
        teamID = pieceInfo.TeamID;
    }

    public virtual List<Vector2> GetMoveableButton() { return pieceInfo.RangeInfoSO.GetAbleRange(); }
    public int GetDamage(int damage, AttackType type)
    {
        hp -= damage;

        return hp;
    }
    public int GetHeal(int damage, AttackType type)
    {
        hp += damage;
        if (hp > maxhp)
            hp = maxhp;
        return hp;
    }
    public virtual void ActionText()
    {

    }
    public IEnumerator DamageText(int damage)
    {
        yield return new WaitForSeconds(1f);
        pieceCanvas.InvokeDamageText(damage);
    }

    public IEnumerator HealText(int damage)
    {
        yield return new WaitForSeconds(1f);
        pieceCanvas.InvokeDamageText(damage);
    }

    public IEnumerator DeathCor()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
