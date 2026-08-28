using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DataManager : MonoBehaviour, IGameDataStore
{
    public static DataManager Instance;

    // TitleScene에는 DataManager가 없어 Instance가 null일 수 있다. 그 상태로 시작 기물을 선택하면
    // MainMenuCanvas가 이 키로 선택한 인덱스를 PlayerPrefs에 남기고, MainScene에서 DataManager가
    // 처음 생성될 때 LoadFromFile()이 이 값을 읽어 적용한다.
    public const string PendingStartingPieceIndexPrefKey = "PendingStartingPieceIndex";

    public GameData currentData = new GameData();
    public PieceInfo basicPieceinfo;
    public PieceInfo summonerPieceinfo;
    private string savePath;

    public IReadOnlyList<PieceData> Pieces => currentData.pieceData;
    public IReadOnlyList<string> OwnedRelicNames => currentData.ownedRelicNames;
    public IReadOnlyList<NodeRow> MapData => currentData.mapData;
    public IReadOnlyList<int> VisitedNodeX => currentData.visitedNodeX;
    public string NextLevelName => currentData.nextLevelName;
    public int CurrentFloor => currentData.currentFloor;
    public int CurrentNodeX => currentData.currentNodeX;

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

    // 실제 파일 기록은 SetNextLevel(맵에서 다음 노드를 골라 다음 전투로 넘어가는 시점)에서만 호출된다.
    // 그 외의 currentData 변경(카드 획득/제거, 기물 추가, 생존 기물 동기화 등)은 메모리에만 반영되고
    // 다음 노드를 선택할 때 한 번에 저장된다.
    public void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentData);
        File.WriteAllText(savePath, json);
        LogPieceDecks();
    }

    void LogPieceDecks()
    {
        if (currentData.pieceData == null) return;
        for (int i = 0; i < currentData.pieceData.Count; i++)
        {
            PieceData piece = currentData.pieceData[i];
            string deck = piece.deckCardIDs != null ? string.Join(", ", piece.deckCardIDs) : "";
            Debug.Log($"[DataManager] 저장: {i}번 기물({piece.pieceName})의 덱 [{deck}]");
        }
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
            PieceInfo startingPiece = basicPieceinfo;
            if (PlayerPrefs.HasKey(PendingStartingPieceIndexPrefKey))
            {
                startingPiece = ResolveStartingPieceInfo(PlayerPrefs.GetInt(PendingStartingPieceIndexPrefKey));
                PlayerPrefs.DeleteKey(PendingStartingPieceIndexPrefKey);
            }
            InitializeDefaultData(startingPiece);
        }
    }

    // 새 게임 시작 시 기본 데이터(기물 2장, 기본 유물, 진행 상태)를 구성한다. startingPiece로 지정한 기물 2장으로 로스터를 채운다.
    void InitializeDefaultData(PieceInfo startingPiece)
    {
        if (currentData.mapData == null) currentData.mapData = new List<NodeRow>();
        if (currentData.pieceData == null) currentData.pieceData = new List<PieceData>();
        if (currentData.nextLevelName == null) currentData.nextLevelName = "";
        if (currentData.visitedNodeX == null) currentData.visitedNodeX = new List<int>();

        List<string> defaultDeck = new List<string>()
        {
            "AttackCard",
            "AttackCard",
            "AttackCard",
            "DefenseCard",
            "DefenseCard",
            "SummonCard",
            "MoveCard",
            "MoveCard",
            "StunCard",
        };
        List<string> deck = startingPiece.DefaultDeckCardIDs != null && startingPiece.DefaultDeckCardIDs.Count > 0
            ? startingPiece.DefaultDeckCardIDs
            : defaultDeck;

        currentData.pieceData.Clear();
        currentData.pieceData.Add(BuildPieceData(startingPiece, deck));

        if (currentData.ownedRelicNames == null) currentData.ownedRelicNames = new List<string>();
        currentData.ownedRelicNames.Add("ShieldRelic");
        currentData.ownedRelicNames.Add("VampiricFangRelic");

        currentData.nextLevelName = "";
        currentData.currentFloor = 0;
        currentData.currentNodeX = -1;
    }

    // 세이브 삭제 후, 지정한 기물 2장으로 시작 로스터를 구성한다 (대체 시작 기물 테스트/선택용)
    public void ResetSaveWithStartingPiece(PieceInfo startingPiece)
    {
        if (startingPiece == null) return;
        if (File.Exists(savePath)) File.Delete(savePath);
        currentData = new GameData();
        InitializeDefaultData(startingPiece);
    }

    // startingPieceIndex로 시작 기물을 선택해 리셋한다 (0: 기본 기물, 1: 소환사)
    public void ResetSaveWithStartingPiece(int startingPieceIndex)
    {
        ResetSaveWithStartingPiece(ResolveStartingPieceInfo(startingPieceIndex));
    }

    PieceInfo ResolveStartingPieceInfo(int startingPieceIndex) => startingPieceIndex switch
    {
        0 => basicPieceinfo,
        1 => summonerPieceinfo,
        _ => basicPieceinfo,
    };

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
    }

    // PieceInfo + 시작 덱으로 PieceData를 조립 (기본 스탯 그대로, hp는 만피로 시작)
    public PieceData BuildPieceData(PieceInfo info, List<string> deckCardIDs)
    {
        return new PieceData
        {
            pieceName = info.PieceName,
            teamID = info.TeamID,
            hp = info.MaxHp,
            maxHp = info.MaxHp,
            colDamage = info.ColDamage,
            shieldBonus = info.ShieldBonus,
            rangeinfoname = info.RangeInfoSO != null ? info.RangeInfoSO.name : "",
            deckCardIDs = deckCardIDs != null ? new List<string>(deckCardIDs) : new List<string>()
        };
    }

    // 새 기물을 세이브에 영구히 추가 (전투 밖 이벤트로 즉시 영입하거나, 전투 중 합류한 기물을 별도로 등록할 때 사용).
    // deckCardIDs를 생략하면 PieceInfo에 지정된 기본 덱을 사용한다.
    public void AddPiece(PieceInfo info, List<string> deckCardIDs = null)
    {
        if (info == null) return;
        currentData.pieceData.Add(BuildPieceData(info, deckCardIDs ?? info.DefaultDeckCardIDs));
    }

    public int AddPieceData(PieceData data)
    {
        currentData.pieceData.Add(data);
        return currentData.pieceData.Count - 1;
    }

    public void SetPieceData(List<PieceData> pieces)
    {
        currentData.pieceData = pieces;
    }

    // relicName을 소유 유물 목록에 영구히 추가 (Board.LoadOwnedRelics가 다음 전투 시작 시 반영)
    public void AddRelic(string relicName)
    {
        if (currentData.ownedRelicNames == null) currentData.ownedRelicNames = new List<string>();
        currentData.ownedRelicNames.Add(relicName);
    }

    // cardname을 지정한 기물의 덱에 영구히 추가
    public void AddCardToPieceDeck(int pieceIndex, string cardname)
    {
        if (pieceIndex < 0 || pieceIndex >= currentData.pieceData.Count) return;
        PieceData piece = currentData.pieceData[pieceIndex];
        if (piece.deckCardIDs == null) piece.deckCardIDs = new List<string>();
        piece.deckCardIDs.Add(cardname);
        currentData.pieceData[pieceIndex] = piece;
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
        CardCanvas.instance?.ShowRemovedCard(cardname);
        return true;
    }

    // gold를 지급한다. 호출부는 아직 없음 — 언제 얼마나 지급할지는 이후 결정.
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        currentData.gold += amount;
    }

    // gold가 충분하면 차감하고 true, 부족하면 아무 것도 하지 않고 false를 반환한다.
    public bool SpendGold(int amount)
    {
        if (amount < 0) return false;
        if (currentData.gold < amount) return false;
        currentData.gold -= amount;
        return true;
    }

    public void GenerateMap(List<NodeRow> mapdata)
    {
        currentData.mapData = mapdata;
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

    public void SetNextLevelName(string levelName)
    {
        currentData.nextLevelName = levelName;
    }

    // 맵 진행 상태 초기화 (새 게임 시작 시)
    public void ResetMapProgress()
    {
        currentData.nextLevelName = "";
        currentData.currentFloor = -1;
        currentData.currentNodeX = -1;
        currentData.mapData = new List<NodeRow>();
        currentData.visitedNodeX = new List<int>();
    }

    // 세이브 파일 삭제 후 기본값으로 초기화
    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
            File.Delete(savePath);
        PlayerPrefs.DeleteKey(PendingStartingPieceIndexPrefKey);
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
    public List<string> ownedRelicNames = new List<string>(); // 소유한 유물 이름들(RelicDatabase 조회 키)
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
    public int colDamageBonus;
    public int shieldBonus;
    public int shieldBonusBonus;
    public string rangeinfoname;
    public List<string> deckCardIDs;

}
