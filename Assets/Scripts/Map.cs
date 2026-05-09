using System.Collections.Generic;
using UnityEngine;

public enum NodeType { mob, unknown, chest }

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
    public static Map instance;
    public List<NodeRow> mapData = new List<NodeRow>();
    private int totalFloors = 5;
    public int TotalFloors => totalFloors;

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

        // 1. ��� ���� (������ y: 0 ~ 4)
        for (int y = 0; y < totalFloors; y++)
        {
            NodeRow row = new NodeRow();
            int nodeCount = 0;

            if (y == 0 || y == totalFloors - 1) nodeCount = 1; // 1���� 5���� ��� 1��
            else nodeCount = Random.Range(1, 3); // 2~4���� 1~4��

            for (int x = 0; x < nodeCount; x++)
            {
                MapNode node = new MapNode();
                node.x = x;
                node.y = y;
                node.type = (NodeType)Random.Range(0, 3);
                node.levelDataName = LevelDatabase.instance != null
                    ? LevelDatabase.instance.GetRandomLevelName(y)
                    : "";
                row.nodes.Add(node);
            }
            mapData.Add(row);
        }

        // 2. ��� ���� (�� �����)
        for (int y = 0; y < totalFloors - 1; y++) // ������ ���� ���� ���� �����Ƿ� ����
        {
            int nextRowNodeCount = mapData[y + 1].nodes.Count;
            int lastTargetIndex = 0; // ���� ������ ���� �ε��� ���� ����

            for (int x = 0; x < mapData[y].nodes.Count; x++)
            {
                MapNode currentNode = mapData[y].nodes[x];

                // 1��(y=0)�� ���� ���� ��� ���� ����
                if (y == 0)
                {
                    for (int i = 0; i < nextRowNodeCount; i++)
                        currentNode.nextNodes.Add(i);
                }
                // 4��(y=3)�� 5���� ������ ���(�ε��� 0)�� ����
                else if (y == totalFloors - 2)
                {
                    currentNode.nextNodes.Add(0);
                }
                // �߰� ��(2~3��) ���� ����
                else
                {
                    // ���� ����: ���� x��° ���� ���� x-1��° ��尡 �����ߴ� ������ �ε������� ���� ����
                    int connectCount = Random.Range(1, 3); // 1~2�� ����
                    for (int i = 0; i < connectCount; i++)
                    {
                        int targetIndex = Mathf.Clamp(lastTargetIndex + i, 0, nextRowNodeCount - 1);

                        if (!currentNode.nextNodes.Contains(targetIndex))
                        {
                            currentNode.nextNodes.Add(targetIndex);
                            lastTargetIndex = targetIndex; // ���� ���� �ּ� �� �ε������� ����
                        }
                    }
                }
            }

            // [���� ��ġ] ���� �� ��� �� �ƹ����Ե� ���ù��� ���� ��尡 �ִٸ�, 
            // ���� ���� ���� ����� ��忡 ������ ���� (���� ����� ���� ����)
            for (int nextIdx = 0; nextIdx < nextRowNodeCount; nextIdx++)
            {
                bool isTargeted = false;
                foreach (var node in mapData[y].nodes)
                {
                    if (node.nextNodes.Contains(nextIdx)) { isTargeted = true; break; }
                }

                if (!isTargeted)
                {
                    // ���� �ε����� ����� ���� �� ��忡�� ����
                    int nearestSrc = Mathf.Clamp(nextIdx, 0, mapData[y].nodes.Count - 1);
                    mapData[y].nodes[nearestSrc].nextNodes.Add(nextIdx);
                }
            }
        }
        DataManager.Instance.GenerateMap(mapData);
        string output = "";
        foreach(NodeRow data in mapData)
        {
            output += data.Info();
            output += "\n";
        }
        Debug.Log(output);
    }
}