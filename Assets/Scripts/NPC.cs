using System.Collections.Generic;
using UnityEngine;

// 대화 이벤트 레벨에서 보드에 놓이는 NPC. 대화 내용은 LevelData에서 관리하며, 전투에는 참여하지 않는다.
// teamID는 pieceInfo(예: NPCPieceInfo)에서 가져오므로 0/1과 다른 값(예: 2)으로 설정해둬야 한다.
public class NPC : Piece
{
    [Tooltip("이 NPC를 클릭했을 때 보여줄 대화. 비어있으면 클릭해도 대화가 뜨지 않는다.")]
    public DialogueSO dialogue;

    public override void Awake()
    {
        base.Awake();
        hp = maxhp = 1;
    }

    public override List<Vector2> GetMoveableButton() => new List<Vector2>();
}
