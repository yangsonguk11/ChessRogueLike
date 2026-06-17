using System.Collections.Generic;
using UnityEngine;

public class PieceGaugeListUI : MonoBehaviour
{
    [SerializeField] PieceGaugeItem gaugePrefab;
    [SerializeField] Transform container;

    readonly Dictionary<Piece, PieceGaugeItem> activeGauges = new Dictionary<Piece, PieceGaugeItem>();
    readonly List<Piece> toRemove = new List<Piece>();

    void Update()
    {
        if (Board.instance == null || !Board.instance.boardReady) return;

        HashSet<Piece> currentPieces = CollectAllPieces();

        toRemove.Clear();
        foreach (var kvp in activeGauges)
        {
            if (kvp.Key == null || !currentPieces.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var piece in toRemove)
        {
            if (activeGauges[piece] != null)
                Destroy(activeGauges[piece].gameObject);
            activeGauges.Remove(piece);
        }

        foreach (var piece in currentPieces)
        {
            if (!activeGauges.TryGetValue(piece, out PieceGaugeItem item))
            {
                item = Instantiate(gaugePrefab, container);
                item.Bind(piece);
                activeGauges[piece] = item;
            }
            item.Refresh();
        }
    }

    HashSet<Piece> CollectAllPieces()
    {
        var pieces = new HashSet<Piece>();
        for (int x = 0; x < Board.instance.N; x++)
            for (int y = 0; y < Board.instance.M; y++)
            {
                Piece p = Board.instance.GetPieceAt(new Vector2(x, y));
                if (p != null) pieces.Add(p);
            }
        return pieces;
    }
}
