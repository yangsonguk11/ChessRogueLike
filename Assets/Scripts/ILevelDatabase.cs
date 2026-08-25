public interface ILevelDatabase
{
    LevelData GetLevel(string levelName);
    string GetRandomLevelName(int floor);
    LevelData GetRandomLevel(int floor);

    // 등록된 층 수. Map.cs가 floorPools.Count를 직접 읽는 대신 이걸 쓴다.
    int FloorCount { get; }
}
