using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public GameData currentData = new GameData();
    public PieceInfo basicPieceinfo;
    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "save.json");
            DataManager.Instance.LoadFromFile();
        }
        else { Destroy(gameObject); }
    }

    public void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentData);
        File.WriteAllText(savePath, json);
    }

    public void LoadFromFile()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            currentData = JsonUtility.FromJson<GameData>(json);
            MigrateLegacyDeckIfNeeded();
        }
        else
        {
            if (currentData.mapData == null) currentData.mapData = new List<NodeRow>();
            if (currentData.pieceData == null) currentData.pieceData = new List<PieceData>();
            if (currentData.nextLevelName == null) currentData.nextLevelName = "";
            if (currentData.visitedNodeX == null) currentData.visitedNodeX = new List<int>();

            PieceData defaultPiece = new PieceData
            {
                pieceName = basicPieceinfo.PieceName,
                teamID = basicPieceinfo.TeamID,
                hp = basicPieceinfo.MaxHp,
                maxHp = basicPieceinfo.MaxHp,
                colDamage = basicPieceinfo.ColDamage,
                shieldBonus = basicPieceinfo.ShieldBonus,
                rangeinfoname = basicPieceinfo.RangeInfoSO != null ? basicPieceinfo.RangeInfoSO.name : "",
                deckCardIDs = new List<string>()
                {
                    "ImmobilizeCard",
                    "BloodChargeCard",
                    "DrawCard",
                    "MagicAttackCard",
                    "FetchAttackCard",
                    "LifeDrainCard",
                    "ZoneAttackCard",
                    "SquadTrainingCard",
                }
            };

             PieceData defaultPiece2 = new PieceData
            {
                pieceName = basicPieceinfo.PieceName,
                teamID = basicPieceinfo.TeamID,
                hp = basicPieceinfo.MaxHp,
                maxHp = basicPieceinfo.MaxHp,
                colDamage = basicPieceinfo.ColDamage,
                shieldBonus = basicPieceinfo.ShieldBonus,
                rangeinfoname = basicPieceinfo.RangeInfoSO != null ? basicPieceinfo.RangeInfoSO.name : "",
                deckCardIDs = new List<string>()
                {
                    "ImmobilizeCard",
                    "BloodChargeCard",
                    "DrawCard",
                    "MagicAttackCard",
                    "FetchAttackCard",
                    "LifeDrainCard",
                    "ZoneAttackCard",
                    "SquadTrainingCard",
                }
            };

            currentData.pieceData.Clear();
            currentData.pieceData.Add(defaultPiece);
            currentData.pieceData.Add(defaultPiece2);

            currentData.nextLevelName = "";
            currentData.currentFloor = 0;
            currentData.currentNodeX = -1;

            SaveToFile();
        }
    }

    // 기물별 덱 도입 이전 세이브 호환: 팀 공용이던 legacyDeckCardIDs를 0번 기물의 덱으로 한 번만 옮긴다.
    void MigrateLegacyDeckIfNeeded()
    {
        if (currentData.deckCardIDs == null || currentData.deckCardIDs.Count == 0) return;
        if (currentData.pieceData == null || currentData.pieceData.Count == 0) return;

        PieceData first = currentData.pieceData[0];
        if (first.deckCardIDs == null) first.deckCardIDs = new List<string>();
        if (first.deckCardIDs.Count == 0)
            first.deckCardIDs.AddRange(currentData.deckCardIDs);
        currentData.pieceData[0] = first;

        currentData.deckCardIDs.Clear();
        SaveToFile();
    }

    // cardname을 지정한 기물의 덱에 영구히 추가
    public void AddCardToPieceDeck(int pieceIndex, string cardname)
    {
        if (pieceIndex < 0 || pieceIndex >= currentData.pieceData.Count) return;
        PieceData piece = currentData.pieceData[pieceIndex];
        if (piece.deckCardIDs == null) piece.deckCardIDs = new List<string>();
        piece.deckCardIDs.Add(cardname);
        currentData.pieceData[pieceIndex] = piece;
        SaveToFile();
        CardCanvas.instance?.ShowAddedCard(cardname, CardPositionZone.Discard);
    }

    // 지정한 기물의 덱에서 카드 1장을 영구히 제거
    public bool RemoveCardFromDeck(int pieceIndex, int cardIndex)
    {
        if (pieceIndex < 0 || pieceIndex >= currentData.pieceData.Count) return false;
        PieceData piece = currentData.pieceData[pieceIndex];
        if (piece.deckCardIDs == null || cardIndex < 0 || cardIndex >= piece.deckCardIDs.Count) return false;

        string cardname = piece.deckCardIDs[cardIndex];
        piece.deckCardIDs.RemoveAt(cardIndex);
        currentData.pieceData[pieceIndex] = piece;
        SaveToFile();
        CardCanvas.instance?.ShowRemovedCard(cardname);
        return true;
    }

    public void GenerateMap(List<NodeRow> mapdata)
    {
        currentData.mapData = mapdata;
        SaveToFile();
    }

    public bool LoadMap()
    {
        if (currentData == null || currentData.mapData == null || currentData.mapData.Count == 0)
            return false;
        return true;
    }

    // 다음 전투 세팅: 선택한 노드의 레벨과 위치를 저장
    public void SetNextLevel(string levelName, int floor, int nodeX)
    {
        currentData.nextLevelName = levelName;
        currentData.currentFloor = floor;
        currentData.currentNodeX = nodeX;

        while (currentData.visitedNodeX.Count <= floor)
            currentData.visitedNodeX.Add(-1);
        currentData.visitedNodeX[floor] = nodeX;

        SaveToFile();
    }

    // 맵 진행 상태 초기화 (새 게임 시작 시)
    public void ResetMapProgress()
    {
        currentData.nextLevelName = "";
        currentData.currentFloor = -1;
        currentData.currentNodeX = -1;
        currentData.mapData = new List<NodeRow>();
        currentData.visitedNodeX = new List<int>();
        SaveToFile();
    }

    // 세이브 파일 삭제 후 기본값으로 초기화
    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
            File.Delete(savePath);
        currentData = new GameData();
        LoadFromFile();
    }
}
[System.Serializable]
public class GameData
{
    public int gold;
    // 기물별 고유 덱 도입 이전(팀 공용 덱) 세이브에서만 값이 들어있음. JsonUtility가 필드명으로만 매칭하므로
    // 이전 세이브와의 호환을 위해 이름을 그대로 유지한다. MigrateLegacyDeckIfNeeded가 소비 후 비운다.
    public List<string> deckCardIDs = new List<string>();
    public List<NodeRow> mapData = new List<NodeRow>();
    public List<PieceData> pieceData = new List<PieceData>();
    public string nextLevelName;
    public int currentFloor;
    public int currentNodeX;
    public List<int> visitedNodeX = new List<int>();
}
[System.Serializable]
public struct PieceData
{
    public string pieceName;
    public int teamID;
    public int hp;

    public int maxHp;
    public int colDamage;
    public int shieldBonus;
    public string rangeinfoname;
    public List<string> deckCardIDs;

}
