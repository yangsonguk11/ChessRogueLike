using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    public Map mapGenerator; // 아까 만든 Map 스크립트 참조
    public GameObject nodePrefab;
    public GameObject linePrefab;
    public RectTransform contentParent;

    public float xSpacing = 150f; // 노드 간 가로 간격
    public float ySpacing = 200f; // 층 간 세로 간격

    private List<List<RectTransform>> instantiatedNodes = new List<List<RectTransform>>();

    void Start()
    {
        // Map 스크립트에서 데이터 생성이 완료된 후 호출
        DrawMap();
    }

    public void DrawMap()
    {
        // 1. 노드 생성 및 배치
        for (int y = 0; y < mapGenerator.mapData.Count; y++)
        {
            var rowData = mapGenerator.mapData[y];
            var rowNodes = new List<RectTransform>();

            for (int x = 0; x < rowData.nodes.Count; x++)
            {
                GameObject nodeObj = Instantiate(nodePrefab, contentParent);
                RectTransform rect = nodeObj.GetComponent<RectTransform>();

                // [수정] 3D 오브젝트가 너무 작다면 크기를 강제로 키움
                nodeObj.transform.localScale = Vector3.one * 20f; // 50배 확대 (적절히 조절)

                float totalWidth = (rowData.nodes.Count - 1) * xSpacing;
                float posX = (x * xSpacing) - (totalWidth * 0.5f);
                float posY = y * ySpacing;

                rect.anchoredPosition = new Vector2(posX, posY);
                rowNodes.Add(rect);
            }
            instantiatedNodes.Add(rowNodes);
        }

        // [중요] 2. Content 크기 확장 (스크롤 가능하게 함)
        // 맵 전체 높이에 맞춰 Content의 높이를 조절합니다.
        float mapHeight = (mapGenerator.mapData.Count) * ySpacing;
        contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, mapHeight + 200f);

        // 지도가 위로 뻗어있다면 시작 위치를 아래로 내림
        contentParent.anchoredPosition = Vector2.zero;

        // 2. 선 그리기 (연결)
        for (int y = 0; y < mapGenerator.mapData.Count - 1; y++)
        {
            var rowData = mapGenerator.mapData[y];
            for (int x = 0; x < rowData.nodes.Count; x++)
            {
                var currentNode = rowData.nodes[x];
                var startRT = instantiatedNodes[y][x];

                foreach (int nextIdx in currentNode.nextNodes)
                {
                    var endRT = instantiatedNodes[y + 1][nextIdx];
                    CreateLine(startRT.anchoredPosition, endRT.anchoredPosition);
                }
            }
        }
    }

    void CreateLine(Vector2 start, Vector2 end)
    {
        GameObject lineObj = Instantiate(linePrefab, contentParent);
        lineObj.transform.SetAsFirstSibling(); // 선이 노드 뒤로 가도록 설정

        RectTransform rect = lineObj.GetComponent<RectTransform>();

        // 두 점 사이의 거리와 방향 계산
        Vector2 dir = end - start;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 선의 위치, 크기, 회전 설정
        rect.anchoredPosition = start + (dir * 0.5f); // 중간 지점
        rect.sizeDelta = new Vector2(distance, 5f);    // 길이는 거리만큼, 두께는 5
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}