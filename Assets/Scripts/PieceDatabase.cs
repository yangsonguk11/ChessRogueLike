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

    // 기물 이름(PieceInfo.PieceName)으로 프리팹을 찾는다 — 레벨 배치(LevelData.placements.name), 소환·합류(PieceInfo),
    // 세이브의 아군(PieceData.pieceName)이 모두 이 이름을 쓴다. 프리팹 파일 이름과는 무관하다.
    public GameObject GetPiece(string pieceName)
    {
        return PiecePrefabs.Find(p => p != null && p.GetComponent<Piece>() is Piece piece
                                      && piece.Info != null && piece.Info.PieceName == pieceName);
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

#if UNITY_EDITOR
    // 기물 이름이 조회 키이므로 등록된 기물끼리 겹치거나 비면 안 된다 — Database.prefab을 수정할 때 바로 경고한다.
    void OnValidate()
    {
        if (PiecePrefabs == null) return;
        var seen = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in PiecePrefabs)
        {
            if (prefab == null) continue;
            Piece piece = prefab.GetComponent<Piece>();
            if (piece == null || piece.Info == null)
            {
                Debug.LogWarning($"[PieceDatabase] '{prefab.name}'에 Piece 또는 PieceInfo가 없습니다.", prefab);
                continue;
            }
            string key = piece.Info.PieceName;
            if (string.IsNullOrEmpty(key))
                Debug.LogWarning($"[PieceDatabase] '{prefab.name}'의 PieceInfo 이름이 비어 있습니다.", prefab);
            else if (seen.TryGetValue(key, out GameObject other))
                Debug.LogWarning($"[PieceDatabase] PieceInfo 이름 '{key}'이(가) '{other.name}'과 '{prefab.name}'에 중복됩니다.", prefab);
            else
                seen[key] = prefab;
        }
    }
#endif
}
