using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public GameData currentData = new GameData();
    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "save.json");
            /*
            currentData.deckCardIDs = new List<string>()
            {
                "AttackCard",
                "AttackCard",
                "AttackCard",
                "Move",
                "Move",
                "Move",
                "Heal",
                "Heal",
                "MoveAndAttackCard",
                "MoveAndAttackCard",
            };
            */
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
    }

    public void AddCardOnDeck(string cardname)
    {
        currentData.deckCardIDs.Add(cardname);
        SaveToFile();
    }
}
[System.Serializable]
public class GameData
{
    public int currentHp;
    public int gold;
    public List<string> deckCardIDs;// 카드 프리팹 이름이나 ID 저장
}
