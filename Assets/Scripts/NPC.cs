using System.Collections.Generic;
using UnityEngine;

// 대화 이벤트 레벨에서 보드에 놓이는 NPC. 클릭 시 보여줄 대화는 이 컴포넌트의 dialogues 필드로 직접 관리하며, 전투에는 참여하지 않는다.
// teamID는 pieceInfo(예: NPCPieceInfo)에서 가져오므로 0/1과 다른 값(예: 2)으로 설정해둬야 한다.
public class NPC : Piece
{
    [SerializeField, Tooltip("클릭할 때마다 순서대로 보여줄 대화 목록. 리스트보다 더 클릭하면 마지막 대화가 반복된다. 비어있으면 클릭해도 대화가 뜨지 않는다.")]
    List<DialogueSO> dialogues;

    // 이 NPC가 클릭되어 dialogue가 실제로 표시된 적이 있는지 여부.
    public bool dialogueTriggered;

    // 이 NPC를 클릭한 횟수. dialogues에서 몇 번째 대화를 보여줄지 정하는 데 쓰인다.
    public int dialogueClickCount;

    // 외부에서 대화를 꺼내갈 때(=클릭으로 표시될 때) dialogueTriggered를 같이 표시하고 클릭 횟수를 늘린다.
    public DialogueSO Dialogue
    {
        get
        {
            if (dialogues == null || dialogues.Count == 0) return null;

            dialogueTriggered = true;
            pieceCanvas?.ClearActionText();

            int index = Mathf.Min(dialogueClickCount, dialogues.Count - 1);
            dialogueClickCount++;
            return dialogues[index];
        }
    }

    public override void Awake()
    {
        base.Awake();
        hp = maxhp = 1;

        if (!dialogueTriggered)
            pieceCanvas?.ShowActionText("!");
    }

    public override List<Vector2> GetMoveableButton() => new List<Vector2>();
}
