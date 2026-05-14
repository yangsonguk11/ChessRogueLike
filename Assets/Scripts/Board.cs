using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board : MonoBehaviour
{
    public static Board instance;
    public static bool playerMovedThisTurn;
    public static bool playerDamagedThisTurn;

    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    GameObject[,] Buttons;
    [SerializeField] GameObject BoardUICanvas;
    [SerializeField] PieceDatabase piecedatabase;
    event Action OnButtonSelected;
    event Action OnButtonUnSelected;
    public bool boardReady = false;

    public List<Vector2> enemyPositions = new List<Vector2>();
    [Header("보드 크기")]
    [Min(1)] public int N;
    [Min(1)] public int M;
    [SerializeField] LevelData leveldata;
    public Grid grid;

    Vector2 _selectedButton;
    Vector2 selectedButton
    {
        get { return _selectedButton; }
        set
        {
            _selectedButton = value;
            if (isSelectedButtonActive()) OnButtonSelected?.Invoke();
            else OnButtonUnSelected?.Invoke();
        }
    }

    public enum BoardMode
    {
        Inspect,
        command,
        targeting,
        enemy
    }

    public BoardMode boardmode;

    private void Awake()
    {
        instance = this;
        playerMovedThisTurn = false;
        OnButtonSelected += OnSelectBoard;
        OnButtonUnSelected += OnUnSelectBoard;
        queuecoroutineworking = false;
    }

    private void Start()
    {
        if ((DataManager.Instance?.currentData.currentFloor ?? -1) < 0)
            return; // 맵에서 노드를 아직 선택하지 않음 — 보드 초기화 건너뜀

        var pieceData = DataManager.Instance.currentData.pieceData;
        if (pieceData == null || pieceData.Count == 0)
        {
            Debug.LogError("[Board] pieceData가 비어있습니다. 플레이어 캐릭터 없이 게임을 진행할 수 없습니다.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        boardReady = true;
        InitBoard(ResolveLevelData());
        TurnStart();
    }

    public void SavePlayerPiecesToDataManager()
    {
        if (!boardReady) return;
        var surviving = new List<PieceData>();
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                Piece p = GetButtonScript(new Vector2(x, y)).GetPieceScript();
                if (p != null && p.teamID == 0)
                    surviving.Add(p.GetPieceData());
            }
        }
        DataManager.Instance.currentData.pieceData = surviving;
        DataManager.Instance.SaveToFile();
    }

    LevelData ResolveLevelData()
    {
        if (LevelDatabase.instance == null) return leveldata;

        string nextLevel = DataManager.Instance?.currentData.nextLevelName;

        // 맵에서 노드를 선택한 경우: 저장된 레벨 이름으로 로드
        if (!string.IsNullOrEmpty(nextLevel))
        {
            LevelData loaded = LevelDatabase.instance.GetLevel(nextLevel);
            if (loaded != null) return loaded;
        }

        // 첫 전투(nextLevelName 없음): LevelDatabase 0층에서 랜덤 선택
        LevelData firstLevel = LevelDatabase.instance.GetRandomLevel(0);
        if (firstLevel != null) return firstLevel;

        return leveldata; // LevelDatabase가 비어있을 때 Inspector 기본값
    }

    void InitBoard(LevelData data)
    {
        // 이전 씬에서 남은 적 목록 초기화 (DontDestroyOnLoad인 GameManager의 리스트)
        GameManager.instance?.Enemylist.Clear();
        enemyPositions.Clear();

        N = data.N;
        M = data.M;
        Background.transform.localScale = new Vector3(N, M, 1);
        Buttons = new GameObject[N, M];
        grid = GetComponent<Grid>();

        float offsetX = (N * grid.cellSize.x + (N - 1) * grid.cellGap.x) / 2f;
        float offsetY = (M * grid.cellSize.y + (M - 1) * grid.cellGap.y) / 2f;
        grid.transform.position = new Vector3(-offsetX + (grid.cellSize.x / 2f), 0, -offsetY + (grid.cellSize.y / 2f));

        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                Vector3 pos = grid.CellToWorld(new Vector3Int(x, 0, y));
                GameObject obj = Instantiate(ButtonPrefab, pos, new Quaternion(), gameObject.transform);
                obj.GetComponent<Button>().Init(x, y, gameObject);
                Buttons[x, y] = obj;
            }
        }

        // isEnemy == false 배치를 플레이어 스폰 위치로 사용
        List<Vector2> playerSpawns = new List<Vector2>();
        foreach (var p in data.placements)
            if (!p.isEnemy) playerSpawns.Add(new Vector2(p.position.x, p.position.y));

        int spawnIdx = 0;
        foreach (PieceData piecedata in DataManager.Instance.currentData.pieceData)
        {
            Vector2 spawnPos = spawnIdx < playerSpawns.Count ? playerSpawns[spawnIdx] : new Vector2(2, 2);
            GameObject piece = Instantiate(piecedatabase.GetPiece(piecedata.pieceName));
            GetButtonScript(spawnPos).SetPiece(piece);
            piece.GetComponent<Piece>().SetPieceData(piecedata);
            spawnIdx++;
        }

        ClearSelectedButton();
        foreach (var placement in data.placements)
        {
            if (!placement.isEnemy) continue; // 플레이어 스폰 마커는 건너뜀

            Debug.LogFormat("{0} {1}", piecedatabase, placement.name);
            GameObject piece = Instantiate(piecedatabase.GetPiece(placement.name));
            GetButtonScript(placement.position).SetPiece(piece);
            enemyPositions.Add(placement.position);
        }

        FinishCardUsage();
        ClearSelectedButton();
        CardCanvas.instance.HandtoDiscardAll();
    }

    GameObject GetButton(Vector2 pos)
    {
        return Buttons[(int)pos.x, (int)pos.y];
    }

    Button GetButtonScript(Vector2 pos)
    {
        return Buttons[(int)pos.x, (int)pos.y].GetComponent<Button>();
    }

    void ClearSelectedButton()
    {
        if (selectedButton.x != -1 && selectedButton.y != -1)
            GetButtonScript(selectedButton).SelectedFalse();
        selectedButton = new Vector2(-1, -1);
    }

    bool isSelectedButtonActive()
    {
        return selectedButton.x >= 0 && selectedButton.y >= 0;
    }
}
