using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
    public void OnClickEndTurn()
    {
        // 씬이 바뀌어도 언제나 현재 활성화된 GameManager를 찾음
        TurnManager.instance.EndPlayerTurn();
    }
}
