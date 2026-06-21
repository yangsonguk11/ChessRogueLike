using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public enum LevelType { Combat, Event }
    public enum EventType { None, Rest }

    public int N; // ���� ũ��
    public int M; // ���� ũ��

    [System.Serializable]
    public struct PiecePlacement
    {
        public Vector2Int position;
        public int pieceTypeIndex; // Pieces �迭�� �ε���
        public bool isEnemy;
        public string name;
    }

    public List<PiecePlacement> placements;

    [Header("레벨 종류")]
    public LevelType levelType = LevelType.Combat;

    [Tooltip("levelType이 Event일 때 어떤 이벤트인지")]
    public EventType eventType = EventType.None;

    [Tooltip("eventType이 None이 아닐 때, 이벤트 오브젝트(예: 휴식 오브젝트)를 놓을 위치")]
    public Vector2Int eventObjectPosition;
}