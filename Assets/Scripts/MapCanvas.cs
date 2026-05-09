using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapCanvas : MonoBehaviour
{
    public static MapCanvas instance;

    CanvasGroup cg;
    MapUI mapUI;
    [SerializeField] GameObject runCompletePanel;

    void Awake()
    {
        instance = this;
        cg = GetComponent<CanvasGroup>();
        mapUI = GetComponentInChildren<MapUI>();
        SetVisible(false);
        runCompletePanel?.SetActive(false);
    }

    IEnumerator Start()
    {
        yield return null; // 한 프레임 대기 — 모든 Start()가 실행된 후 맵 표시
        if ((DataManager.Instance?.currentData.currentFloor ?? -1) < 0)
            Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (mapUI == null)
        {
            Debug.LogError("[MapCanvas] MapUI 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        if (mapUI.mapGenerator == null)
            mapUI.mapGenerator = Map.instance;
        if (mapUI.mapGenerator == null)
        {
            Debug.LogError("[MapCanvas] Map.instance가 없습니다. Map 오브젝트가 씬에 있는지 확인하세요.");
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

    public void ShowRunComplete()
    {
        gameObject.SetActive(true);
        if (mapUI == null)
        {
            Debug.LogError("[MapCanvas] ShowRunComplete: MapUI 없음");
            return;
        }
        if (mapUI.mapGenerator == null)
            mapUI.mapGenerator = Map.instance;
        if (mapUI.mapGenerator == null)
        {
            Debug.LogError("[MapCanvas] ShowRunComplete: Map.instance 없음");
            return;
        }
        mapUI.ClearMap();
        mapUI.DrawMap(runComplete: true);
        runCompletePanel?.SetActive(true);
        SetVisible(true);
    }

    public void RestartGame()
    {
        DataManager.Instance.ResetMapProgress();
        SceneManager.LoadScene("MainScene");
    }

    void SetVisible(bool visible)
    {
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }
}
