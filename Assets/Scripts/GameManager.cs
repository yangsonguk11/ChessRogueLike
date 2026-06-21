using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] GameObject Board;
    [SerializeField] GameObject CardCanvas;
    public List<GameObject> Enemylist = new List<GameObject>();

    private bool isQuitting = false;
    private void OnApplicationQuit() { isQuitting = true; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AssignReferences();
    }
    void AssignReferences()
    {
        Board = GameObject.Find("Board");
        CardCanvas = GameObject.Find("CardCanvas");

    }
    public void AddEnemy(GameObject obj)
    {
        Enemylist.Add(obj);
    }

    public void RemoveEnemy(GameObject obj)
    {
        Enemylist.Remove(obj);
        if (Enemylist.Count == 0 && !isQuitting)
            EndBattle();
    }

    void EndBattle()
    {
        int currentFloor = DataManager.Instance.currentData.currentFloor;
        int currentNodeX = DataManager.Instance.currentData.currentNodeX;

        // 맵 선택 없이 시작된 전투(currentNodeX == -1)는 노드 0을 방문 기록으로 저장
        if (currentNodeX < 0 && currentFloor >= 0 &&
            Map.instance != null && Map.instance.mapData.Count > currentFloor &&
            Map.instance.mapData[currentFloor].nodes.Count > 0)
        {
            DataManager.Instance.SetNextLevel(
                DataManager.Instance.currentData.nextLevelName,
                currentFloor, 0);
        }

        // 생존한 플레이어 기물 데이터를 DataManager에 저장 (hp 0 이하는 이미 보드에서 제거됨)
        this.Board?.GetComponent<global::Board>()?.SavePlayerPiecesToDataManager();
        if (Map.instance != null && currentFloor >= Map.instance.TotalFloors - 1)
        {
            if (MapCanvas.instance != null)
                MapCanvas.instance.ShowRunComplete();
            else
                Debug.LogError("[EndBattle] MapCanvas.instance가 null입니다.");
        }
        else
        {
            if (ResultCanvas.Instance != null)
                ResultCanvas.Instance.EnableCanvas();
            else
                Debug.LogError("[EndBattle] ResultCanvas.Instance가 null입니다.");
        }
    }
}
