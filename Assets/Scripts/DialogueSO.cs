using System.Collections.Generic;
using UnityEngine;

// NPC나 LevelData가 공유해서 참조하는 대화 한 세트.
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue")]
public class DialogueSO : ScriptableObject
{
    public enum LineType { Line, Choice }

    [System.Serializable]
    public class Choice
    {
        public string choiceText;

        [Tooltip("이 선택지를 고른 후 진행할 lines의 인덱스 (-1이면 대화 종료). triggerCombat이 설정되어 있으면 무시된다.")]
        public int nextLineIndex = -1;

        [Tooltip("설정하면 이 선택지를 고르는 즉시 대화를 닫고 지정한 전투 레벨로 진입한다 (씬을 다시 로드함).")]
        public LevelData triggerCombat;

        [Tooltip("0이면 회복 없음, 양수면 그 수치만큼, 음수(예: -1)면 아군 전체를 최대 HP까지(풀힐) 회복시킨다.")]
        public int healAmount;

        [Tooltip("0보다 크면 이 선택지를 고르는 즉시 아군 전체에 이 수치만큼 피해를 준다.")]
        public int damageAmount;

        [Tooltip("비어있지 않으면 이 목록 중 하나를 무작위로 골라 덱에 추가한다 (CardDatabase에 등록된 카드 이름).")]
        public List<string> cardPool;
    }

    [System.Serializable]
    public class Line
    {
        public LineType type = LineType.Line;

        [Tooltip("화자 이름")]
        public string speaker;

        [Tooltip("대사 내용")]
        [TextArea]
        public string text;

        [Tooltip("type이 Choice일 때만 사용하는 선택지 목록 (2개 이상 가능)")]
        public List<Choice> choices;
    }

    public List<Line> lines;
}
