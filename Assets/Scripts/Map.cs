using System.Collections.Generic;
using UnityEngine;

public enum NodeType { Mob, Rest, Unknown, Boss }

[System.Serializable]
public class MapNode
{
    public int x, y;
    public NodeType type;
    public List<int> nextNodes = new List<int>(); // 다음 행(y+1) 노드의 인덱스들
    public string levelDataName; // 이 노드에서 실행할 LevelData 이름
}

[System.Serializable]
public class NodeRow
{
    public List<MapNode> nodes = new List<MapNode>();
}

public class Map : MonoBehaviour
{
    public static Map instance;
    public List<NodeRow> mapData = new List<NodeRow>();
    public int TotalFloors => LevelDatabase.instance != null ? LevelDatabase.instance.floorPools.Count : 5;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (instance != this) return;
        if (DataManager.Instance.LoadMap())
            mapData = DataManager.Instance.currentData.mapData;
        else
            GenerateMap();
    }

    void GenerateMap()
    {
        mapData.Clear();
        int totalFloors = TotalFloors;

        // 1. 노드 생성 (층마다 y: 0 ~ totalFloors-1)
        for (int y = 0; y < totalFloors; y++)
        {
            NodeRow row = new NodeRow();
            int nodeCount = 0;

            if (y == 0 || y == totalFloors - 1) nodeCount = 1; // 첫 층과 마지막 층은 노드 1개
            else nodeCount = Random.Range(1, 3); // 중간 층은 1~2개

            for (int x = 0; x < nodeCount; x++)
            {
                LevelData level = LevelDatabase.instance != null
                    ? LevelDatabase.instance.GetRandomLevel(y)
                    : null;

                MapNode node = new MapNode();
                node.x = x;
                node.y = y;
                node.type = DetermineNodeType(level, y, totalFloors);
                node.levelDataName = level != null ? level.name : "";
                row.nodes.Add(node);
            }
            mapData.Add(row);
        }

        // 2. 노드 간 연결 (다음 층으로)
        for (int y = 0; y < totalFloors - 1; y++) // 마지막 층은 다음 층이 없으므로 제외
        {
            int nextRowNodeCount = mapData[y + 1].nodes.Count;
            int lastTargetIndex = 0; // 이전 노드가 연결한 마지막 인덱스 추적 용도

            for (int x = 0; x < mapData[y].nodes.Count; x++)
            {
                MapNode currentNode = mapData[y].nodes[x];

                // 첫 층(y=0)은 다음 층 노드 전부와 연결
                if (y == 0)
                {
                    for (int i = 0; i < nextRowNodeCount; i++)
                        currentNode.nextNodes.Add(i);
                }
                // 마지막에서 두 번째 층은 다음 층의 유일한 노드(인덱스 0)와 연결
                else if (y == totalFloors - 2)
                {
                    currentNode.nextNodes.Add(0);
                }
                // 중간 층은 순서대로 연결
                else
                {
                    // 연결 방식: 이전 x번째 노드가 이전 x-1번째 노드가 연결했던 마지막 인덱스부터 순서대로 연결
                    int connectCount = Random.Range(1, 3); // 1~2개 연결
                    for (int i = 0; i < connectCount; i++)
                    {
                        int targetIndex = Mathf.Clamp(lastTargetIndex + i, 0, nextRowNodeCount - 1);

                        if (!currentNode.nextNodes.Contains(targetIndex))
                        {
                            currentNode.nextNodes.Add(targetIndex);
                            lastTargetIndex = targetIndex; // 다음 노드 연결 시 이어서 사용
                        }
                    }
                }
            }

            // [안전 장치] 연결 후 다음 층에 아무에게도 연결받지 못한 노드가 있다면,
            // 가장 가까운 인덱스의 현재 층 노드에서 연결을 추가 (고아 노드가 생기는 것을 방지)
            for (int nextIdx = 0; nextIdx < nextRowNodeCount; nextIdx++)
            {
                bool isTargeted = false;
                foreach (var node in mapData[y].nodes)
                {
                    if (node.nextNodes.Contains(nextIdx)) { isTargeted = true; break; }
                }

                if (!isTargeted)
                {
                    // 가장 가까운 인덱스의 현재 층 노드에서 연결
                    int nearestSrc = Mathf.Clamp(nextIdx, 0, mapData[y].nodes.Count - 1);
                    mapData[y].nodes[nearestSrc].nextNodes.Add(nextIdx);
                }
            }
        }
        DataManager.Instance.GenerateMap(mapData);
    }

    // LevelData의 종류(LevelType/EventType)를 보고 맵 아이콘에 쓸 NodeType을 결정
    NodeType DetermineNodeType(LevelData level, int floor, int totalFloors)
    {
        if (floor == totalFloors - 1) return NodeType.Boss;
        if (level == null) return NodeType.Unknown;

        if (level.levelType == LevelData.LevelType.Event)
            return level.eventType == LevelData.EventType.Rest ? NodeType.Rest : NodeType.Unknown;

        return NodeType.Mob;
    }
}
