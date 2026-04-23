using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] GameObject Board;
    [SerializeField] GameObject CardCanvas;
    public List<GameObject> Enemylist = new List<GameObject>();
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
        // 씬 전환 이벤트 구독 (유니티에서 씬이 바뀌면 자동으로 호출됨)
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
        if (Enemylist.Count == 0)
            EndBattle();
    }

    void EndBattle()
    {
        Debug.Log("Battle Ended");
        ResultCanvas.Instance.EnableCanvas();
    }
}
