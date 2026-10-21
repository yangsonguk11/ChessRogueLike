using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 카드 테스트 씬 전용: 아군 턴이 시작될 때마다(적 턴이 끝나고 돌아올 때 포함) 인스펙터의 testCardNames에 적은
// 카드를 활성 기물의 손패에 넣어준다. 목록이 비어 있으면 CardDatabase에 등록된 모든 카드를 1장씩 넣는다.
public class CardTestHandDealer : MonoBehaviour
{
    [Tooltip("손패에 넣을 카드 ID(클래스 이름 = 프리팹 이름, 예: AttackCard). 같은 이름을 여러 번 적으면 그만큼 들어온다. 비우면 전체 카드.")]
    [SerializeField] List<string> testCardNames = new List<string>();

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
            DealTestCards();
        lastState = current;
    }

    void DealTestCards()
    {
        if (CardCanvas.instance == null || CardDatabase.instance == null) return;
        if (CardCanvas.instance.ActivePiece == null) return;

        // 등록되지 않은 이름은 CardDatabase.SpawnCard가 에러 로그를 남기고 null을 돌려주므로 그 줄만 건너뛴다.
        List<string> names = testCardNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList();
        IEnumerable<string> cardNames = names.Count > 0 ? names : ((ICardDatabase)CardDatabase.instance).GetAllCardNames();
        foreach (string cardName in cardNames)
        {
            IEnumerator reveal = CardCanvas.instance.AddCardDuringCombat(CardCanvas.instance.ActivePiece, cardName, CardPositionZone.Hand);
            if (reveal != null) CardCanvas.instance.StartCoroutine(reveal);
        }
    }
}
