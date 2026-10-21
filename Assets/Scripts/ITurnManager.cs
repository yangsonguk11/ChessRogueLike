public interface ITurnManager
{
    TurnState CurrentState { get; }

    void StartPlayerTurn();
    void EndPlayerTurn();
    void TurnStateProcessing();
    void RollbackStateProcessing();
}
