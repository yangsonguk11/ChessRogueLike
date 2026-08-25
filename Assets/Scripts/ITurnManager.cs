public interface ITurnManager
{
    TurnState CurrentState { get; }

    void StartPlayerTurn();
    void EndPlayerTurn();
    void EndEnemyTurn();
    void TurnStateProcessing();
    void RollbackStateProcessing();
}
