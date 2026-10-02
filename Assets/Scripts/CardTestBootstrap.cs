using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 카드 테스트 씬 전용: Board.Start()가 실행되기 전에(스크립트 실행 순서를 앞당겨) 아군을 1기로 줄이고
// 강제 진입 레벨(testLevel)을 지정한다. 실제 세이브 파일은 건드리지 않도록 시작 전 내용을 백업해뒀다가
// 씬을 나갈 때 그대로 복원한다 (전투 중 적 처치로 인한 자동 저장 등으로부터 실제 세이브를 보호).
[DefaultExecutionOrder(-10000)]
public class CardTestBootstrap : MonoBehaviour
{
    [SerializeField] LevelData testLevel;

    // 카드 테스트 씬이 떠 있는 동안 true — 전투가 끝나도 보상/결과 화면 흐름(GameManager.FinishLevel)을 타지 않는다.
    // GameManager가 DontDestroyOnLoad라 씬을 나갈 때(OnDestroy) 반드시 다시 내려야 한다.
    public static bool IsActive { get; private set; }

    void Awake() => IsActive = true;

    string savePath;
    string originalSaveJson;
    bool hasBackup;

    void Start()
    {
        savePath = Path.Combine(Application.persistentDataPath, "save.json");
        if (File.Exists(savePath))
        {
            originalSaveJson = File.ReadAllText(savePath);
            hasBackup = true;
        }

        GameData data = DataManager.Instance.currentData;
        data.currentFloor = 0;
        data.currentNodeX = -1;
        data.nextLevelName = "";

        PieceData ally = data.pieceData[0];
        ally.hp = ally.maxHp;
        ally.deckCardIDs = new List<string>();
        data.pieceData = new List<PieceData> { ally };

        Board.pendingLevel = testLevel;
    }

    void OnApplicationQuit() => RestoreSave();
    void OnDestroy()
    {
        IsActive = false;
        RestoreSave();
    }

    void RestoreSave()
    {
        if (!hasBackup) return;
        File.WriteAllText(savePath, originalSaveJson);
        hasBackup = false;
    }
}
