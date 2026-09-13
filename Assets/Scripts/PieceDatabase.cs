using System.Collections.Generic;
using UnityEngine;

public class PieceDatabase : MonoBehaviour
{
    public static PieceDatabase instance;

    public List<GameObject> PiecePrefabs;

    [Tooltip("pieceName(PieceInfo.PieceName)으로 PieceInfo를 조회하기 위한 목록. " +
        "PiecePrefabs에 등록 안 된 PieceInfo도 포함해, 보상 카드 풀 조회 대상은 전부 등록해둔다.")]
    public List<PieceInfo> PieceInfos;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public GameObject GetPiece(string cardName)
    {
        GameObject c = PiecePrefabs.Find(p => p.name == cardName);
        return c;
    }

    public PieceInfo GetPieceInfo(string pieceName) => PieceInfos.Find(p => p != null && p.PieceName == pieceName);
}
