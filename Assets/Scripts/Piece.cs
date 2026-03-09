using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    [SerializeField] PieceInfo pieceInfo;
    [SerializeField] PieceCanvas pieceCanvas;
    
    public int hp;
    public int colDamage;
    public int teamID;
    private void Awake()
    {
        hp = pieceInfo.MaxHp;
        colDamage = pieceInfo.ColDamage;
        teamID = pieceInfo.TeamID;
    }

    public List<Vector2> GetMoveableButton() { return pieceInfo.RangeInfoSO.GetAbleRange(); }
    public int GetDamage(int damage, AttackType type)
    {
        hp -= damage;

        if (pieceCanvas)
            pieceCanvas.InvokeDamageText(damage);
        return hp;

    }


    public IEnumerator DeathCor()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
