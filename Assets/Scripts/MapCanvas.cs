using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapCanvas : MonoBehaviour
{
    public static MapCanvas instance;

    CanvasGroup cg;
    MapUI mapUI;
    [SerializeField] GameObject runCompletePanel;
    bool _activatingNormally = false;
    bool _lastWasInteractive = false;

    void Awake()
    {
        instance = this;
        cg = GetComponent<CanvasGroup>();
        mapUI = GetComponentInChildren<MapUI>();
        runCompletePanel?.SetActive(false);
    }

    void OnEnable()
    {
        if (_activatingNormally) return;
        DrawAndShow(_lastWasInteractive);
    }

    IEnumerator Start()
    {
        yield return null;
        if ((DataManager.Instance?.currentData.currentFloor ?? -1) < 0)
            Show();
    }

    public void Show()
    {
        _lastWasInteractive = true;
        _activatingNormally = true;
        gameObject.SetActive(true);
        _activatingNormally = false;

        if (mapUI == null) { Debug.LogError("[MapCanvas] MapUI 컴포넌트를 찾을 수 없습니다."); return; }
        if (mapUI.mapGenerator == null) mapUI.mapGenerator = Map.instance;
        if (mapUI.mapGenerator == null) { Debug.LogError("[MapCanvas] Map.instance가 없습니다."); return; }
        if (mapUI.mapGenerator.mapData == null || mapUI.mapGenerator.mapData.Count == 0) { Debug.LogError("[MapCanvas] 맵 데이터가 비어있습니다."); return; }

        DrawAndShow(true);
    }

    public void ShowRunComplete()
    {
        _lastWasInteractive = true;
        _activatingNormally = true;
        gameObject.SetActive(true);
        _activatingNormally = false;

        if (mapUI == null) { Debug.LogError("[MapCanvas] ShowRunComplete: MapUI 없음"); return; }
        if (mapUI.mapGenerator == null) mapUI.mapGenerator = Map.instance;
        if (mapUI.mapGenerator == null) { Debug.LogError("[MapCanvas] ShowRunComplete: Map.instance 없음"); return; }

        mapUI.ClearMap();
        mapUI.DrawMap(runComplete: true);
        runCompletePanel?.SetActive(true);
        SetVisible(true, interactable: true);
    }

    void DrawAndShow(bool interactive)
    {
        if (mapUI == null) return;
        if (mapUI.mapGenerator == null) mapUI.mapGenerator = Map.instance;
        if (mapUI.mapGenerator == null) return;
        if (mapUI.mapGenerator.mapData == null || mapUI.mapGenerator.mapData.Count == 0) return;

        mapUI.ClearMap();
        mapUI.DrawMap(viewOnly: !interactive);
        SetVisible(true, interactable: interactive);
    }

    public void RestartGame()
    {
        DataManager.Instance.ResetMapProgress();
        SceneManager.LoadScene("MainScene");
    }

    void SetVisible(bool visible, bool interactable = true)
    {
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible && interactable;
    }
}
