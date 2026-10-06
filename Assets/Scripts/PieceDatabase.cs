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

    // pieceName 기물의 직업 보상 카드 풀. PieceInfo/Job이 없거나 풀이 비어 있으면 null.
    // 전투 보상(ResultCanvas)과 상점(ShopCanvas)이 같은 기준으로 "이 기물이 받을 수 있는 카드"를 판단하도록 공용으로 둔다.
    public List<string> GetRewardCardPool(string pieceName)
    {
        List<string> pool = GetPieceInfo(pieceName)?.Job?.RewardCardPool;
        return pool != null && pool.Count > 0 ? pool : null;
    }
}
