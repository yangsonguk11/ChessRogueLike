using UnityEngine;

public enum TurnState { Player, Enemy, Processing }
public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;
    public TurnState currentState;// 현재 턴 상태
    public TurnState prevState;   // processing 직전의 턴 상태
    [SerializeField] Board board; // 보드 참조

    void Start()
    {
        StartPlayerTurn();
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void StartPlayerTurn()
    {
        currentState = TurnState.Player;
        // 플레이어가 조작 가능하도록 UI 활성화 등
        Debug.Log("플레이어 턴 시작");
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.Player) return;

        // 보드에 선택된 것들 해제
        board.SendMessage("TurnEnd");

        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        currentState = TurnState.Enemy;
        board.SendMessage("PlayEnemyTurn");
        // 적 AI 로직 실행...
        Debug.Log("적 턴 시작");

        
        // 예: 적 행동이 끝나면 다시 플레이어 턴으로
        // StartCoroutine(EnemyAILogic());
    }
    public void EndEnemyTurn()
    {
        board.SendMessage("TurnEnd");
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
