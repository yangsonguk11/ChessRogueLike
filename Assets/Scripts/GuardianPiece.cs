using System.Collections.Generic;
using UnityEngine;

// 수호자(Enemy-Guardian, L2-Guardian): 평소엔 actionCards(주변 동료 방어막)를 순환하다가, 자신 외의 같은 팀 기물이
// 모두 쓰러지면 enragedActionCards(이동 후 주변 광역 피해 — EnemyMoveAndBlastCard)로 패턴을 영구히 바꾼다. 전환은 마지막 동료의 사망 판정
// 즉시(Board.ClearDeadPieceOccupancy → OnTeammateDied) 일어나므로, 플레이어 턴에 동료를 처치하면 바뀐 예고를
// 보고 대응할 수 있다.
public class GuardianPiece : AutoPiece
{
    public List<Card> enragedActionCards;

    bool enraged;

    public override void Awake()
    {
        base.Awake();
        foreach (Card card in enragedActionCards)
            if (card != null) card.previewCasterOverride = this;
    }

    public override void OnTeammateDied(int livingTeammates)
    {
        if (enraged || livingTeammates > 0 || hp <= 0) return;
        enraged = true;
        SwitchActionCards(enragedActionCards);
        Board.instance?.EnqueueBoardAnimation(StatusTextReaction("격노", true, new Color(1f, 0.27f, 0.27f)));
    }
}
