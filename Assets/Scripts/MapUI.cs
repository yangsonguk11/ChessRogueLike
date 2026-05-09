using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    public Map mapGenerator;
    public GameObject nodePrefab;
    public GameObject linePrefab;
    public RectTransform contentParent;

    public float xSpacing = 100f;
    public float ySpacing = 300f;

    private List<List<RectTransform>> instantiatedNodes = new List<List<RectTransform>>();

    void Start() { mapGenerator = Map.instance; }

    public void ClearMap()
    {
        // contentParent의 모든 자식 삭제 (노드 + 연결선)
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
        instantiatedNodes.Clear();
    }

    public void DrawMap(bool runComplete = false, bool viewOnly = false)
    {
        int currentFloor = DataManager.Instance.currentData.currentFloor;
        int currentNodeX = DataManager.Instance.currentData.currentNodeX;
        // currentNodeX가 미설정(-1)이면 첫 번째 노드(0)를 현재 위치로 간주
        int displayNodeX = (currentNodeX < 0 && currentFloor >= 0) ? 0 : currentNodeX;
        var visited = DataManager.Instance.currentData.visitedNodeX;
        float mapHeight = mapGenerator.mapData.Count * ySpacing;

        // 노드 생성 및 초기화
        for (int y = 0; y < mapGenerator.mapData.Count; y++)
        {
            var rowData = mapGenerator.mapData[y];
            var rowNodes = new List<RectTransform>();

            for (int x = 0; x < rowData.nodes.Count; x++)
            {
                GameObject nodeObj = Instantiate(nodePrefab, contentParent, false);
                RectTransform rect = nodeObj.GetComponent<RectTransform>();

                float totalWidth = (rowData.nodes.Count - 1) * xSpacing;
                float posX = (x * xSpacing) - (totalWidth * 0.5f);
                float posY = y * ySpacing - (mapHeight * 0.25f);
                rect.anchoredPosition = new Vector2(posX, posY);

                NodeButton btn = nodeObj.GetComponent<NodeButton>();
                if (btn != null)
                {
                    btn.nodeData = rowData.nodes[x];
                    btn.nodeFloor = y;
                }

                rowNodes.Add(rect);
            }
            instantiatedNodes.Add(rowNodes);
        }

        contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, mapHeight);
        contentParent.anchoredPosition = Vector2.zero;

        // 연결선 그리기
        for (int y = 0; y < mapGenerator.mapData.Count - 1; y++)
        {
            var rowData = mapGenerator.mapData[y];
            for (int x = 0; x < rowData.nodes.Count; x++)
            {
                foreach (int nextIdx in rowData.nodes[x].nextNodes)
                {
                    CreateLine(
                        instantiatedNodes[y][x].anchoredPosition,
                        instantiatedNodes[y + 1][nextIdx].anchoredPosition
                    );
                }
            }
        }

        // 선택 가능 상태 설정
        for (int y = 0; y < instantiatedNodes.Count; y++)
        {
            for (int x = 0; x < instantiatedNodes[y].Count; x++)
            {
                NodeButton btn = instantiatedNodes[y][x].GetComponent<NodeButton>();
                if (btn == null) continue;

                bool isVisited = visited != null && y < visited.Count && visited[y] == x;
                bool isCurrent = !runComplete && (y == currentFloor && x == displayNodeX);
                bool isSelectable = !runComplete && !viewOnly && IsNodeReachable(y, x, currentFloor, currentNodeX);

                btn.selectable = isSelectable;
                btn.SetVisualState(isSelectable, isCurrent, isVisited);
            }
        }
    }

    bool IsNodeReachable(int nodeFloor, int nodeX, int currentFloor, int currentNodeX)
    {
        // 첫 맵 진입: 층 0의 모든 노드 선택 가능
        if (currentFloor == -1)
            return nodeFloor == 0;

        // 현재 층 + 1이어야 함
        if (nodeFloor != currentFloor + 1) return false;
        // 특정 노드를 선택하지 않은 상태면 다음 층 전체 선택 가능
        if (currentNodeX < 0) return true;
        var currentRow = mapGenerator.mapData[currentFloor];
        if (currentNodeX >= currentRow.nodes.Count) return false;
        return currentRow.nodes[currentNodeX].nextNodes.Contains(nodeX);
    }

    void CreateLine(Vector2 start, Vector2 end)
    {
        GameObject lineObj = Instantiate(linePrefab, contentParent);
        lineObj.transform.SetAsFirstSibling();

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        Vector2 dir = end - start;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rect.anchoredPosition = start + (dir * 0.5f);
        rect.sizeDelta = new Vector2(distance, 5f);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
