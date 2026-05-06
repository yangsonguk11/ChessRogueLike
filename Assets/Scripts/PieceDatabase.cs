using System.Collections.Generic;
using UnityEngine;

public class PieceDatabase : MonoBehaviour
{
    public List<GameObject> PiecePrefabs;

    public GameObject GetPiece(string cardName)
    {
        GameObject c = PiecePrefabs.Find(p => p.name == cardName);
        return c;
    }
}
