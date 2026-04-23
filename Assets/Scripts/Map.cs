using System.Collections.Generic;
using UnityEngine;

public enum NodeType { mob, unknown, chest }

[System.Serializable]
public class MapNode
{
    public int x, y;
    public NodeType type;
    public List<int> nextNodes = new List<int>(); // 다음 층(y+1) 노드의 인덱스들
}

[System.Serializable]
public class NodeRow
{
    public List<MapNode> nodes = new List<MapNode>();

    public string Info()
    {
        string result = "";

        foreach (MapNode m in nodes) {
            result += string.Format("{0} {1} //", m.x, m.y);
            foreach(int i in m.nextNodes)
            {
                result += string.Format(" {0}", i);
            }
        }
        return result;
    }
}

public class Map : MonoBehaviour
{
    public List<NodeRow> mapData = new List<NodeRow>();
    private int totalFloors = 5;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        mapData.Clear();

        // 1. 노드 생성 (세로축 y: 0 ~ 4)
        for (int y = 0; y < totalFloors; y++)
        {
            NodeRow row = new NodeRow();
            int nodeCount = 0;

            if (y == 0 || y == totalFloors - 1) nodeCount = 1; // 1층과 5층은 노드 1개
            else nodeCount = Random.Range(1, 5); // 2~4층은 1~4개

            for (int x = 0; x < nodeCount; x++)
            {
                MapNode node = new MapNode();
                node.x = x;
                node.y = y;
                node.type = (NodeType)Random.Range(0, 3);
                row.nodes.Add(node);
            }
            mapData.Add(row);
        }

        // 2. 노드 연결 (길 만들기)
        for (int y = 0; y < totalFloors - 1; y++) // 마지막 층은 다음 층이 없으므로 제외
        {
            int nextRowNodeCount = mapData[y + 1].nodes.Count;
            int lastTargetIndex = 0; // 교차 방지를 위한 인덱스 제한 변수

            for (int x = 0; x < mapData[y].nodes.Count; x++)
            {
                MapNode currentNode = mapData[y].nodes[x];

                // 1층(y=0)은 다음 층의 모든 노드와 연결
                if (y == 0)
                {
                    for (int i = 0; i < nextRowNodeCount; i++)
                        currentNode.nextNodes.Add(i);
                }
                // 4층(y=3)은 5층의 유일한 노드(인덱스 0)와 연결
                else if (y == totalFloors - 2)
                {
                    currentNode.nextNodes.Add(0);
                }
                // 중간 층(2~3층) 연결 로직
                else
                {
                    // 교차 방지: 현재 x번째 노드는 이전 x-1번째 노드가 연결했던 마지막 인덱스부터 연결 시작
                    int connectCount = Random.Range(1, 3); // 1~2개 연결
                    for (int i = 0; i < connectCount; i++)
                    {
                        int targetIndex = Mathf.Clamp(lastTargetIndex + i, 0, nextRowNodeCount - 1);

                        if (!currentNode.nextNodes.Contains(targetIndex))
                        {
                            currentNode.nextNodes.Add(targetIndex);
                            lastTargetIndex = targetIndex; // 다음 노드는 최소 이 인덱스부터 연결
                        }
                    }
                }
            }

            // [안전 장치] 다음 층 노드 중 아무에게도 선택받지 못한 노드가 있다면, 
            // 현재 층의 가장 가까운 노드에 강제로 연결 (길이 끊기는 현상 방지)
            for (int nextIdx = 0; nextIdx < nextRowNodeCount; nextIdx++)
            {
                bool isTargeted = false;
                foreach (var node in mapData[y].nodes)
                {
                    if (node.nextNodes.Contains(nextIdx)) { isTargeted = true; break; }
                }

                if (!isTargeted)
                {
                    // 가장 인덱스가 가까운 현재 층 노드에게 연결
                    int nearestSrc = Mathf.Clamp(nextIdx, 0, mapData[y].nodes.Count - 1);
                    mapData[y].nodes[nearestSrc].nextNodes.Add(nextIdx);
                }
            }
        }

        string output = "";
        foreach(NodeRow data in mapData)
        {
            output += data.Info();
            output += "\n";
        }
        Debug.Log(output);
    }
}