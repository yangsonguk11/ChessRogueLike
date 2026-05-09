using System.Collections.Generic;
using UnityEngine;

public class LevelDatabase : MonoBehaviour
{
    public static LevelDatabase instance;

    [System.Serializable]
    public class FloorPool
    {
        [Tooltip("이 층에서 랜덤으로 선택될 레벨들")]
        public List<LevelData> levels;
    }

    [Tooltip("층 번호(0부터)에 대응하는 레벨 풀")]
    public List<FloorPool> floorPools;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public LevelData GetLevel(string levelName)
    {
        foreach (var pool in floorPools)
            foreach (var level in pool.levels)
                if (level != null && level.name == levelName)
                    return level;
        return null;
    }

    public string GetRandomLevelName(int floor)
    {
        var level = GetRandomLevel(floor);
        return level != null ? level.name : "";
    }

    public LevelData GetRandomLevel(int floor)
    {
        if (floor < 0 || floor >= floorPools.Count) return null;
        var pool = floorPools[floor];
        if (pool.levels == null || pool.levels.Count == 0) return null;
        return pool.levels[Random.Range(0, pool.levels.Count)];
    }
}
