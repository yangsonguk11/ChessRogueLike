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

    // 파일로 저장
    public void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentData);
        File.WriteAllText(savePath, json);
        Debug.Log("게임 저장 완료 " + Application.persistentDataPath);
    }

    // 파일에서 불러오기
    public void LoadFromFile()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            currentData = JsonUtility.FromJson<GameData>(json);
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
        Debug.LogFormat("{0} {1}", currentData, currentData.mapData);
        if (currentData == null || currentData.mapData.Count == 0)
            return false;
        else
            return true;
    }
}
[System.Serializable]
public class GameData
{
    public int gold;
    public List<string> deckCardIDs;// 카드 프리팹 이름이나 ID 저장
    public List<NodeRow> mapData;   // 지도 정보 저장
    public List<PieceData> pieceData; // 플레이어 기물 저장
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
