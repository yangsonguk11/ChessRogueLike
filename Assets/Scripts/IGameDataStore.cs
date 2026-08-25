using System.Collections.Generic;

// DataManager가 실제로 제공하는 기능을 담은 인터페이스. currentData(GameData)를 통째로 노출하는 대신
// 읽기는 프로퍼티로, 쓰기는 의미 있는 동작 단위 메서드로 좁혀서 소비하는 쪽이 currentData 내부 구조를
// 직접 몰라도 되게 한다.
public interface IGameDataStore
{
    IReadOnlyList<PieceData> Pieces { get; }
    IReadOnlyList<string> OwnedRelicNames { get; }
    IReadOnlyList<NodeRow> MapData { get; }
    IReadOnlyList<int> VisitedNodeX { get; }
    string NextLevelName { get; }
    int CurrentFloor { get; }
    int CurrentNodeX { get; }

    void SaveToFile();
    PieceData BuildPieceData(PieceInfo info, List<string> deckCardIDs);
    void AddPiece(PieceInfo info, List<string> deckCardIDs = null);

    // data를 pieceData에 추가하고, 그 항목이 놓인 인덱스를 반환한다 (Board.SpawnPiece 전용).
    int AddPieceData(PieceData data);

    // 생존 기물 목록으로 pieceData 전체를 교체한다 (Board.SavePlayerPiecesToDataManager 전용).
    void SetPieceData(List<PieceData> pieces);

    void AddRelic(string relicName);
    void AddCardToPieceDeck(int pieceIndex, string cardname);
    bool RemoveCardFromDeck(int pieceIndex, int cardIndex);
    void AddGold(int amount);
    bool SpendGold(int amount);
    void GenerateMap(List<NodeRow> mapdata);
    bool LoadMap();
    void SetNextLevel(string levelName, int floor, int nodeX);

    // floor/node 갱신과 저장 없이 nextLevelName만 기록한다 (Board.EnterCombat 전용 — 대화 선택지로
    // 강제 전투 진입 시, 껐다 켜도 그 전투로 복귀하도록 이름만 남겨두는 용도).
    void SetNextLevelName(string levelName);

    void ResetMapProgress();
    void DeleteSaveFile();
}
