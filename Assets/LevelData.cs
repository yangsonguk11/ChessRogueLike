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

    [Tooltip("eventType이 Unknown일 때 레벨 시작과 동시에 보여줄 대화. 비어있으면 시작 시 대화를 띄우지 않는다.")]
    public DialogueSO dialogue;
}