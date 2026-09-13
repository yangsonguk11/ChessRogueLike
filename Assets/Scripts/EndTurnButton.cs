using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
    // 이 프로젝트에는 보드 타일용 커스텀 Button 클래스가 전역 네임스페이스에 있어서
    // UnityEngine.UI.Button을 반드시 전체 이름으로 써야 한다.
    [SerializeField] UnityEngine.UI.Button button;

    void Awake()
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
    }

    void Update()
    {
        if (button != null)
            button.interactable = CanEndTurn();
    }

    bool CanEndTurn()
    {
        // 카드 예약 기능으로 여러 카드의 연출(pendingCardFlights)이 동시에 밀려 있을 수 있으므로,
        // isCardEffecting(로직 완료 여부)뿐 아니라 그 연출이 전부 끝났는지도 함께 확인한다 — 턴 종료는
        // 되돌리기 어려운 동작이라 보수적으로(애니메이션까지 전부 드레인될 때까지) 막아둔다.
        return TurnManager.instance != null && TurnManager.instance.CurrentState == TurnState.Player
            && Board.instance != null && !Board.instance.queuecoroutineworking && !Board.instance.CombatEnded
            && !(CardCanvas.instance != null && CardCanvas.instance.isCardEffecting)
            && !(CardCanvas.instance != null && CardCanvas.instance.HasPendingCardFlights);
    }

    public void OnClickEndTurn()
    {
        if (!CanEndTurn()) return;
        AudioManager.instance?.PlayButtonClick();
        TurnManager.instance.EndPlayerTurn();
    }
}
