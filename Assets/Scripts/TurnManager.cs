using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TurnState { Player, Enemy, Processing }
public class TurnManager : MonoBehaviour, ITurnManager
{
    public static TurnManager instance;
    [Header("Debug")]
    public TurnState currentState;
    public TurnState prevState;
    public TurnState CurrentState => currentState;
    [Header("References")]
    [SerializeField] Board board;

    // EndPlayerTurn() ~ 다음 StartPlayerTurnCoroutine() 사이(아군 턴 종료 효과 + 자동 아군 턴 처리 구간)를
    // 명시적으로 잠근다. currentState만으로는 부족한 이유: 이 구간에서도 currentState는 여전히
    // Player이거나(자동 아군 행동이 아직 motionQueue를 쓰지 않는 순간) 일시적으로 Processing(prevState는
    // 여전히 Player)일 뿐이라, "지금 실질적으로 플레이어 턴인가" 판정을 prevState만으로 내리면 이 구간에도
    // 입력이 새어 들어간다.
    public bool PlayerInputLocked { get; private set; }

    // 카드 처리(예: 카드 예약)로 인한 잔여 애니메이션 때문에 currentState가 일시적으로 Processing이어도,
    // 원래 턴이 플레이어 턴이었다면(prevState == Player) 계속 입력을 허용한다 — "로직은 이미 끝났고
    // 연출만 재생 중"인 상태에서도 다음 카드를 조작할 수 있어야 하기 때문. 적 턴/자동 아군 턴 구간은
    // PlayerInputLocked로 별도 차단한다.
    public bool IsPlayerActionable =>
        !PlayerInputLocked &&
        (currentState == TurnState.Player || (currentState == TurnState.Processing && prevState == TurnState.Player));

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
        PlayerInputLocked = false;
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
        if (board.CombatEnded) yield break; // 전투가 이미 끝났으면 새 턴을 시작하지 않음
        currentState = TurnState.Player;
        PlayerInputLocked = false;
        AudioManager.instance?.PlayTurnStartPlayer();
        if (isFirstTurn)
        {
            isFirstTurn = false;
            if (AnnouncementUI.instance != null)
            {
                AnnouncementUI.instance.Show("전투 시작", isWarning: false);
                yield return AnnouncementUI.instance.currentRoutine;
            }
        }
        if (AnnouncementUI.instance != null)
        {
            AnnouncementUI.instance.Show("플레이어 턴", isWarning: false);
            yield return AnnouncementUI.instance.currentRoutine;
        }
        board.SendMessage("TurnStart");
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.Player || board.IsEventLevel || board.CombatEnded) return; // 이벤트 레벨/전투 종료 후에는 턴이 흐르지 않음
        PlayerInputLocked = true; // 다음 StartPlayerTurnCoroutine까지(아군 턴 종료 효과 + 자동 아군 턴) 입력 차단
        StartCoroutine(EndPlayerTurnCoroutine());
    }

    IEnumerator EndPlayerTurnCoroutine()
    {
        board.SendMessage("AllyTurnEnd");
        yield return new WaitUntil(() => !board.queuecoroutineworking);
        yield return StartCoroutine(board.PlayAutoAllyTurnCoroutine());
        if (board.CombatEnded) yield break; // 아군 자동 행동 중 전투가 끝났으면 적 턴으로 넘어가지 않음
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        StartCoroutine(StartEnemyTurnCoroutine());
    }

    IEnumerator StartEnemyTurnCoroutine()
    {
        if (board.CombatEnded) yield break; // 전투가 이미 끝났으면 적 턴을 시작하지 않음
        currentState = TurnState.Enemy;
        AudioManager.instance?.PlayTurnStartEnemy();
        if (AnnouncementUI.instance != null)
        {
            AnnouncementUI.instance.Show("적 턴", isWarning: false);
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
