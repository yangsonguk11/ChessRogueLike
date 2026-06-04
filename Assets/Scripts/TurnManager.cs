using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TurnState { Player, Enemy, Processing }
public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;
    [Header("Debug")]
    public TurnState currentState;
    public TurnState prevState;
    [Header("References")]
    [SerializeField] Board board;

    void Start()
    {
        //StartPlayerTurn();
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
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
        currentState = TurnState.Player;
        prevState = TurnState.Player;
    }
    void AssignReferences()
    {
        board = GameObject.Find("Board").GetComponent<Board>();
    }
    public void StartPlayerTurn()
    {
        StartCoroutine(StartPlayerTurnCoroutine());
    }

    IEnumerator StartPlayerTurnCoroutine()
    {
        currentState = TurnState.Player;
        Debug.Log("플레이어 턴 시작");
        if (TurnAnnouncementUI.instance != null)
            yield return TurnAnnouncementUI.instance.ShowRoutine("플레이어 턴");
        board.SendMessage("TurnStart");
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.Player) return;
        StartCoroutine(EndPlayerTurnCoroutine());
    }

    IEnumerator EndPlayerTurnCoroutine()
    {
        board.SendMessage("AllyTurnEnd");
        yield return new WaitUntil(() => !board.turnEffectQueueRunning);
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        StartCoroutine(StartEnemyTurnCoroutine());
    }

    IEnumerator StartEnemyTurnCoroutine()
    {
        currentState = TurnState.Enemy;
        Debug.Log("적 턴 시작");
        if (TurnAnnouncementUI.instance != null)
            yield return TurnAnnouncementUI.instance.ShowRoutine("적 턴");
        board.SendMessage("PlayEnemyTurn");
    }
    public void EndEnemyTurn()
    {
        board.SendMessage("EnemyTurnEnd");
    }
    public void TurnStateProcessing()
    {
        prevState = currentState;
        currentState = TurnState.Processing;
    }
    public void RollbackStateProcessing()
    {
        currentState = prevState;
    }
}
