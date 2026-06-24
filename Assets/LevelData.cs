using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public enum LevelType { Combat, Event }
    public enum EventType { None, Rest, Unknown }

    public int N; // ���� ũ��
    public int M; // ���� ũ��

    [System.Serializable]
    public struct PiecePlacement
    {
        public Vector2Int position;
        public string name; // 비어있으면 플레이어 스폰 위치 마커, 채워져 있으면 PieceDatabase에서 찾아 스폰 (적/NPC/소환물 등 — 스폰된 기물의 teamID==1이면 enemyPositions에 등록됨)
    }

    public List<PiecePlacement> placements;

    [Header("레벨 종류")]
    public LevelType levelType = LevelType.Combat;

    [Tooltip("levelType이 Event일 때 어떤 이벤트인지")]
    public EventType eventType = EventType.None;

    [Tooltip("eventType이 Rest일 때, 휴식 오브젝트를 놓을 위치")]
    public Vector2Int eventObjectPosition;

    public enum DialogueLineType { Line, Choice }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;

        [Tooltip("이 선택지를 고른 후 진행할 dialogueLines의 인덱스 (-1이면 대화 종료)")]
        public int nextLineIndex = -1;
    }

    [System.Serializable]
    public class DialogueLine
    {
        public DialogueLineType type = DialogueLineType.Line;

        [Tooltip("화자 이름")]
        public string speaker;

        [Tooltip("대사 내용")]
        [TextArea]
        public string text;

        [Tooltip("type이 Choice일 때만 사용하는 선택지 목록 (2개 이상 가능)")]
        public List<DialogueChoice> choices;
    }

    [Tooltip("eventType이 Unknown일 때 사용할 대화 내용")]
    public List<DialogueLine> dialogueLines;
}