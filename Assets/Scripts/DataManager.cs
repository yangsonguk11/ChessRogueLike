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
            // 구버전 세이브 파일 호환: null 리스트 초기화
            if (currentData.deckCardIDs == null) currentData.deckCardIDs = new List<string>();
            if (currentData.mapData == null) currentData.mapData = new List<NodeRow>();
            if (currentData.pieceData == null) currentData.pieceData = new List<PieceData>();
            if (currentData.nextLevelName == null) currentData.nextLevelName = "";
        }
        else
        {
            currentData.deckCardIDs = new List<string>()
            {
                "AttackCard",
                "AttackCard",
                "AttackCard",
                "Move",
                "Move",
                "Move",
                "Defense",
                "Defense",
                "Defense",
                "MoveAndAttackCard",
            };

            PieceData defaultPiece = new PieceData
            {
                pieceName = "Black Knight",
                teamID = 0,
                hp = 10,
                maxHp = 10,
                colDamage = 3,
                rangeinfoname = "BasicRangeInfoSO"
            };

            if (currentData.pieceData.Count > 0)
                currentData.pieceData[0] = defaultPiece;
            else
                currentData.pieceData.Add(defaultPiece);

            currentData.nextLevelName = "";
            currentData.currentFloor = -1;
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
        SaveToFile();
    }

    // 맵 진행 상태 초기화 (새 게임 시작 시)
    public void ResetMapProgress()
    {
        currentData.nextLevelName = "";
        currentData.currentFloor = -1;
        currentData.currentNodeX = -1;
        currentData.mapData = new List<NodeRow>();
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
    public string nextLevelName;      // 다음 전투에서 로드할 LevelData 이름
    public int currentFloor;          // 마지막으로 선택한 노드의 층 번호 (-1 = 맵 진입 전)
    public int currentNodeX;          // 마지막으로 선택한 노드의 X 위치
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
