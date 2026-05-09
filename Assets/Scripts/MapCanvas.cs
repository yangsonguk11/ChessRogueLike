using UnityEngine;

public class MapCanvas : MonoBehaviour
{
    public static MapCanvas instance;

    CanvasGroup cg;
    MapUI mapUI;

    void Awake()
    {
        instance = this;
        cg = GetComponent<CanvasGroup>();
        mapUI = GetComponentInChildren<MapUI>();
        SetVisible(false);
    }

    public void Show()
    {
        if (mapUI == null)
        {
            Debug.LogError("[MapCanvas] MapUI 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        if (mapUI.mapGenerator == null)
        {
            Debug.LogError("[MapCanvas] MapUI.mapGenerator(Map 컴포넌트)가 연결되지 않았습니다.");
            return;
        }
        if (mapUI.mapGenerator.mapData == null || mapUI.mapGenerator.mapData.Count == 0)
        {
            Debug.LogError("[MapCanvas] 맵 데이터가 비어있습니다.");
            return;
        }

        mapUI.ClearMap();
        mapUI.DrawMap();
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }
}
