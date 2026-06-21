using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
    public void OnClickEndTurn()
    {
        TurnManager.instance.EndPlayerTurn();
    }
}
