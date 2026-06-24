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

    bool isFirstTurn = true;

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
        isFirstTurn = true;
    }
    void AssignReferences()
    {
        board = GameObject.Find("Board").GetComponent<Board>();
    }
    public void StartPlayerTurn()
    {
        StartCoroutine(StartPlayerTurnCoroutine());
    }

    // 이벤트 레벨에서는 Board가 이 코루틴을 아예 호출하지 않음 (턴이 흐르지 않음)
    IEnumerator StartPlayerTurnCoroutine()
    {
        currentState = TurnState.Player;
        if (isFirstTurn)
        {
            isFirstTurn = false;
            if (AnnouncementUI.instance != null)
            {
                AnnouncementUI.instance.Show("전투 시작");
                yield return AnnouncementUI.instance.currentRoutine;
            }
        }
        if (AnnouncementUI.instance != null)
        {
            AnnouncementUI.instance.Show("플레이어 턴");
            yield return AnnouncementUI.instance.currentRoutine;
        }
        board.SendMessage("TurnStart");
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.Player || board.IsEventLevel) return; // 이벤트 레벨은 턴이 흐르지 않음
        StartCoroutine(EndPlayerTurnCoroutine());
    }

    IEnumerator EndPlayerTurnCoroutine()
    {
        board.SendMessage("AllyTurnEnd");
        yield return new WaitUntil(() => !board.queuecoroutineworking);
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        StartCoroutine(StartEnemyTurnCoroutine());
    }

    IEnumerator StartEnemyTurnCoroutine()
    {
        currentState = TurnState.Enemy;
        if (AnnouncementUI.instance != null)
        {
            AnnouncementUI.instance.Show("적 턴");
            yield return AnnouncementUI.instance.currentRoutine;
        }
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
