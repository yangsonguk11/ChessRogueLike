using UnityEngine;

public interface IGameManager
{
    void SetEventLevelUI(bool isEventLevel);
    void AddEnemy(GameObject obj);
    void RemoveEnemy(GameObject obj);
    void ClearEnemies();
    void AddAlly(GameObject obj);
    void RemoveAlly(GameObject obj);
    void ClearAllies();
    void FinishEventLevel();
}
