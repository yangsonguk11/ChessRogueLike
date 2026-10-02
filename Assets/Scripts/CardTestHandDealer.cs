using UnityEngine;

// 카드 테스트 씬 전용: 아군 턴이 시작될 때마다(적 턴이 끝나고 돌아올 때 포함) CardDatabase에 등록된
// 모든 카드를 1장씩 활성 기물의 손패에 넣어준다.
public class CardTestHandDealer : MonoBehaviour
{
    TurnState lastState = TurnState.Enemy;

    void Update()
    {
        if (TurnManager.instance == null) return;

        TurnState current = TurnManager.instance.CurrentState;
        // Processing은 턴이 바뀐 게 아니라 보드 연출이 재생되는 동안만 걸리는 상태다(Board.ProcessQueue).
        // 이걸 턴 전환으로 치면 아군 턴 중 연출(턴 시작 드로우, 카드 사용 등)이 끝날 때마다 Processing→Player를
        // 새 턴으로 오인해 카드를 전부 다시 나눠준다 — 그래서 무시하고 lastState도 갱신하지 않는다.
        if (current == TurnState.Processing) return;
        if (current == TurnState.Player && lastState != TurnState.Player)
            DealAllCards();
        lastState = current;
    }

    void DealAllCards()
    {
        if (CardCanvas.instance == null || CardDatabase.instance == null) return;
        if (CardCanvas.instance.ActivePiece == null) return;

        ICardDatabase cardDb = CardDatabase.instance;
        foreach (string cardName in cardDb.GetAllCardNames())
            CardCanvas.instance.AddCardDuringCombat(cardName, CardPositionZone.Hand);
    }
}
