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
        // �� ��ȯ �̺�Ʈ ���� (����Ƽ���� ���� �ٲ�� �ڵ����� ȣ���)
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
        currentState = TurnState.Player;
        // �÷��̾ ���� �����ϵ��� UI Ȱ��ȭ ��
        Debug.Log("�÷��̾� �� ����");
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
        currentState = TurnState.Enemy;
        board.SendMessage("PlayEnemyTurn");
        // �� AI ���� ����...
        Debug.Log("�� �� ����");

        
        // ��: �� �ൿ�� ������ �ٽ� �÷��̾� ������
        // StartCoroutine(EnemyAILogic());
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
