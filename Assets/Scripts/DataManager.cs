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

    // ���Ϸ� ����
    public void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentData);
        File.WriteAllText(savePath, json);
        Debug.Log("���� ���� �Ϸ� " + Application.persistentDataPath);
    }

    // ���Ͽ��� �ҷ�����
    public void LoadFromFile()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            currentData = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            if (currentData.deckCardIDs == null) currentData.deckCardIDs = new List<string>();
            if (currentData.mapData == null) currentData.mapData = new List<NodeRow>();
            if (currentData.pieceData == null) currentData.pieceData = new List<PieceData>();
            if (currentData.nextLevelName == null) currentData.nextLevelName = "";
            if (currentData.visitedNodeX == null) currentData.visitedNodeX = new List<int>();
            currentData.deckCardIDs = new List<string>()
            {
                "AttackCard",
                "MoveCard",
                "ChargeCard",
                "ColDamageAttackCard",
                "ColDamageUpCard",
                "DefenseCard",
                "DefensiveStanceCard",
                "DirectionalAttackCard",
                "DrawCard",
                "EnemyAttackCard",
                "EnemyMoveCard",
                "FlameThrowingCard",
                "HealCard",
                "HeavyAttackCard",
                "HeavyShieldCard",
                "LifeDrainCard",
                "MoveAndAttackCard",
                "MoveAndDrawCard",
                "PersistentShieldCard",
                "SafeMoveCard",
                "ShieldCycleCard",
                "TempColDamageUpCard",
            };

            PieceData defaultPiece = new PieceData
            {
                pieceName = basicPieceinfo.PieceName,
                teamID = basicPieceinfo.TeamID,
                hp = basicPieceinfo.MaxHp,
                maxHp = basicPieceinfo.MaxHp,
                colDamage = basicPieceinfo.ColDamage,
                rangeinfoname = basicPieceinfo.RangeInfoSO != null ? basicPieceinfo.RangeInfoSO.name : ""
            };

            if (currentData.pieceData.Count > 0)
                currentData.pieceData[0] = defaultPiece;
            else
                currentData.pieceData.Add(defaultPiece);

            currentData.nextLevelName = "";
            currentData.currentFloor = 0;
            currentData.currentNodeX = -1;

            SaveToFile();
        }
    }

    public void AddCardOnDeck(string cardname)
    {
        currentData.deckCardIDs.Add(cardname);
        SaveToFile();
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
}
[System.Serializable]
public class GameData
{
    public int gold;
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
    public string rangeinfoname;

}
