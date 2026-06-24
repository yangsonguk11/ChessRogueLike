using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board : MonoBehaviour
{
    public static Board instance;
    public static bool playerDamagedThisTurn;

    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    [SerializeField] GameObject RestObjectPrefab;
    GameObject[,] Buttons;
    [SerializeField] GameObject BoardUICanvas;
    [SerializeField] PieceDatabase piecedatabase;
    [SerializeField] GameObject EventExitButtonObj;
    [SerializeField] DialogueUI dialogueUI;
    public bool IsEventLevel { get; private set; }
    LevelData.EventType currentEventType;
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
        cardSelecting   // 카드 선택 패널 대기 중
    }

    public BoardMode boardmode;

    private void Awake()
    {
        instance = this;
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
        if (!IsEventLevel)
            TurnManager.instance.StartPlayerTurn(); // 이벤트 레벨은 턴이 흐르지 않음 — TurnManager는 기본 Player 상태로 둠
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

        IsEventLevel = data.levelType == LevelData.LevelType.Event;
        currentEventType = data.eventType;
        EventExitButtonObj?.SetActive(false); // 전투 중이거나 이벤트가 아직 끝나지 않았으면 항상 숨김
        dialogueUI?.Hide();
        GameManager.instance?.SetEventLevelUI(IsEventLevel);

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

        // name이 비어있는 배치를 플레이어 스폰 위치로 사용
        List<Vector2> playerSpawns = new List<Vector2>();
        foreach (var p in data.placements)
            if (string.IsNullOrEmpty(p.name)) playerSpawns.Add(new Vector2(p.position.x, p.position.y));

        int spawnIdx = 0;
        foreach (PieceData piecedata in DataManager.Instance.currentData.pieceData)
        {
            Vector2 spawnPos = spawnIdx < playerSpawns.Count ? playerSpawns[spawnIdx] : new Vector2(2, 2);
            GameObject prefab = piecedatabase.GetPiece(piecedata.pieceName);
            if (prefab == null)
            {
                Debug.LogError($"[Board] 플레이어 기물 스폰 실패: '{piecedata.pieceName}' 을(를) PieceDatabase에서 찾을 수 없습니다. (spawnPos={spawnPos})");
                spawnIdx++;
                continue;
            }
            GameObject piece = Instantiate(prefab);
            GetButtonScript(spawnPos).SetPiece(piece);
            piece.GetComponent<Piece>().SetPieceData(piecedata);
            spawnIdx++;
        }

        ClearSelectedButton();
        foreach (var placement in data.placements)
        {
            if (string.IsNullOrEmpty(placement.name)) continue; // 플레이어 스폰 마커는 건너뜀

            GameObject prefab = piecedatabase.GetPiece(placement.name);
            if (prefab == null)
            {
                Debug.LogError($"[Board] 기물 스폰 실패: '{placement.name}' 을(를) PieceDatabase에서 찾을 수 없습니다. (pos={placement.position})");
                continue;
            }
            GameObject piece = Instantiate(prefab);
            GetButtonScript(placement.position).SetPiece(piece);

            // teamID==1(적)인 기물만 enemyPositions에 등록해 적 턴 AI 대상이 되게 함
            if (piece.GetComponent<Piece>().teamID == 1)
                enemyPositions.Add(placement.position);
        }

        if (currentEventType == LevelData.EventType.Rest)
        {
            if (RestObjectPrefab == null)
                Debug.LogError("[Board] 휴식 레벨이지만 RestObjectPrefab이 설정되지 않았습니다.");
            else
            {
                GameObject restObj = Instantiate(RestObjectPrefab);
                GetButtonScript(new Vector2(data.eventObjectPosition.x, data.eventObjectPosition.y)).SetPiece(restObj);
            }
        }
        else if (currentEventType == LevelData.EventType.Unknown)
        {
            dialogueUI?.Show(data.dialogueLines);
        }

        FinishCardUsage();
        ClearSelectedButton();
        CardCanvas.instance.HandtoDiscardAll();
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

    public Piece casterPiece;

    public int CasterColDamage => casterPiece?.ColDamageDelta ?? 0;

    public Piece GetPieceAt(Vector2 pos) => GetButtonScript(pos)?.GetPieceScript();

    public void HoverPieceFromUI(Piece piece)
    {
        GetButtonForPiece(piece)?.MouseEnter();
    }

    public void UnhoverPieceFromUI(Piece piece)
    {
        GetButtonForPiece(piece)?.MouseExit();
    }

    Button GetButtonForPiece(Piece piece)
    {
        if (piece == null) return null;
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Button button = GetButtonScript(new Vector2(x, y));
                if (button.GetPieceScript() == piece) return button;
            }
        return null;
    }

    void ResetPieceMovedThisTurn()
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2(x, y));
                if (p != null && p.teamID == 0)
                    p.movedThisTurn = false;
            }
    }
}
