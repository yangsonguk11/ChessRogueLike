using System.Collections.Generic;
using UnityEngine;

public class PieceDatabase : MonoBehaviour
{
    public static PieceDatabase instance;

    public List<GameObject> PiecePrefabs;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public GameObject GetPiece(string cardName)
    {
        GameObject c = PiecePrefabs.Find(p => p.name == cardName);
        return c;
    }

    // 별도로 관리되는 PieceInfo 목록 없이, 이미 채워져 있는 PiecePrefabs 각각의 Piece.Info에서
    // 바로 찾는다 — 두 목록을 따로 유지하면 한쪽만 갱신됐을 때 보상 풀 조회가 조용히 실패할 수 있다.
    public PieceInfo GetPieceInfo(string pieceName)
    {
        foreach (GameObject prefab in PiecePrefabs)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null && piece.Info != null && piece.Info.PieceName == pieceName)
                return piece.Info;
        }
        return null;
    }
}
