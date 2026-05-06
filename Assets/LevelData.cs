using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public int N; // 가로 크기
    public int M; // 세로 크기

    [System.Serializable]
    public struct PiecePlacement
    {
        public Vector2Int position;
        public int pieceTypeIndex; // Pieces 배열의 인덱스
        public bool isEnemy;
        public string name;
    }

    public List<PiecePlacement> placements;
}