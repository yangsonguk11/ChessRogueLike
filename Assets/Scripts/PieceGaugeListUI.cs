using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieceGaugeListUI : MonoBehaviour
{
    [SerializeField] PieceGaugeItem gaugePrefab;
    [SerializeField] Transform container;

    readonly Dictionary<Piece, PieceGaugeItem> activeGauges = new Dictionary<Piece, PieceGaugeItem>();
    readonly List<Piece> toRemove = new List<Piece>();

    // 목록 표시 순서: 아군(teamID 0)을 위에, 나머지(적)를 아래에 두고, 팀 안에서는 처음 보인 순서대로 쌓는다.
    // 그래서 전투 중 소환된 기물은 자기 팀 블록의 맨 아래에 붙는다.
    readonly List<Piece> joinOrder = new List<Piece>();
    readonly Dictionary<Piece, bool> sortedAsAlly = new Dictionary<Piece, bool>();

    void Awake()
    {
        // 항목 높이는 항목 안의 VerticalLayoutGroup이 내용(숨긴 줄 제외, 줄바꿈 포함)으로 계산한다.
        // 목록이 그 선호 높이를 직접 써야 같은 레이아웃 패스에서 배치가 맞는다 — 항목에 ContentSizeFitter를 두면
        // 목록이 바뀌기 전 높이로 배치한 뒤 항목 크기가 바뀌어, 다른 갱신이 올 때까지 항목이 겹치거나 벌어진다.
        if (container != null && container.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }
    }

    void Update()
    {
        if (Board.instance == null || !Board.instance.boardReady) return;

        List<Piece> currentPieces = CollectAllPieces();
        var currentSet = new HashSet<Piece>(currentPieces);
        bool orderDirty = false;

        toRemove.Clear();
        foreach (var kvp in activeGauges)
        {
            if (kvp.Key == null || !currentSet.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var piece in toRemove)
        {
            if (activeGauges[piece] != null)
                Destroy(activeGauges[piece].gameObject);
            activeGauges.Remove(piece);
            joinOrder.Remove(piece);
            sortedAsAlly.Remove(piece);
            orderDirty = true;
        }

        foreach (var piece in currentPieces)
        {
            if (!activeGauges.TryGetValue(piece, out PieceGaugeItem item))
            {
                item = Instantiate(gaugePrefab, container);
                item.Bind(piece);
                activeGauges[piece] = item;
                joinOrder.Add(piece);
                orderDirty = true;
            }
            if (!sortedAsAlly.TryGetValue(piece, out bool wasAlly) || wasAlly != IsAlly(piece))
                orderDirty = true;
            item.Refresh();
        }

        if (orderDirty) ApplyOrder();
    }

    static bool IsAlly(Piece piece) => piece.teamID == 0;

    void ApplyOrder()
    {
        int index = 0;
        // 아군 먼저, 그다음 적 — 각 팀 안에서는 joinOrder(등장 순) 유지
        for (int pass = 0; pass < 2; pass++)
        {
            bool allyPass = pass == 0;
            foreach (var piece in joinOrder)
            {
                bool ally = IsAlly(piece);
                if (ally != allyPass) continue;
                sortedAsAlly[piece] = ally;
                activeGauges[piece].transform.SetSiblingIndex(index++);
            }
        }
    }

    // 보드를 x→y 순으로 훑는다. 같은 프레임에 새로 보인 기물들은 이 순서대로 joinOrder에 들어간다.
    List<Piece> CollectAllPieces()
    {
        var pieces = new List<Piece>();
        var seen = new HashSet<Piece>();
        for (int x = 0; x < Board.instance.N; x++)
            for (int y = 0; y < Board.instance.M; y++)
            {
                Piece p = Board.instance.GetPieceAt(new Vector2Int(x, y));
                if (p != null && seen.Add(p)) pieces.Add(p);
            }
        return pieces;
    }
}
