using UnityEngine;

// 카드 테스트 씬 전용: 아군 턴이 시작될 때마다(적 턴이 끝나고 돌아올 때 포함) CardDatabase에 등록된
// 모든 카드를 1장씩 활성 기물의 손패에 넣어준다.
public class CardTestHandDealer : MonoBehaviour
{
    TurnState lastState = TurnState.Enemy;

    void Update()
    {
        if (TurnManager.instance == null) return;

        TurnState current = TurnManager.instance.currentState;
        if (current == TurnState.Player && lastState != TurnState.Player)
            DealAllCards();
        lastState = current;
    }

    void DealAllCards()
    {
        if (CardCanvas.instance == null || CardDatabase.instance == null) return;
        if (CardCanvas.instance.ActivePiece == null) return;

        foreach (GameObject prefab in CardDatabase.instance.cardPrefabs)
        {
            if (prefab == null) continue;
            CardCanvas.instance.AddCardDuringCombat(prefab.name, CardPositionZone.Hand);
        }
    }
}
