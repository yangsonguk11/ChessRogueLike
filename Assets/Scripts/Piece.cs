using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Piece : MonoBehaviour
{
    [SerializeField] PieceInfo pieceInfo;
    public PieceCanvas pieceCanvas;
    
    public int hp;
    public int colDamage;
    public int teamID;
    public virtual void Awake()
    {
        hp = pieceInfo.MaxHp;
        colDamage = pieceInfo.ColDamage;
        teamID = pieceInfo.TeamID;
    }

    public List<Vector2> GetMoveableButton() { return pieceInfo.RangeInfoSO.GetAbleRange(); }
    public int GetDamage(int damage, AttackType type)
    {
        hp -= damage;

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
    public IEnumerator DeathCor()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
