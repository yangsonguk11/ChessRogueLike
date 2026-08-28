using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class Board : MonoBehaviour
{
    public static Board instance;
    public static bool playerDamagedThisTurn;

    // 대화 선택지 등에서 강제로 진입시킬 전투. 씬 재로드 전에 채워두고, Start()에서 한 번 소비된다.
    public static LevelData pendingLevel;

    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    [SerializeField] GameObject RestObjectPrefab;
    [SerializeField] GameObject ShopObjectPrefab;
    GameObject[,] Buttons;
    [SerializeField] GameObject BoardUICanvas;
    [SerializeField] PieceDatabase piecedatabase;
    [SerializeField] GameObject EventExitButtonObj;
    [SerializeField] DialogueUI dialogueUI;
    public bool IsEventLevel { get; private set; }
    public LevelData currentLevelData { get; private set; }
    LevelData.EventType currentEventType;
    event Action OnButtonSelected;
    event Action OnButtonUnSelected;
    public bool boardReady = false;

    public List<Vector2Int> enemyPositions = new List<Vector2Int>();
    public List<Vector2Int> autoAllyPositions = new List<Vector2Int>();
    [Header("보드 크기")]
    [Min(1)] public int N;
    [Min(1)] public int M;
    [SerializeField] LevelData leveldata;
    public Grid grid;

    Vector2Int _selectedButton;
    Vector2Int selectedButton
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
        if ((DataManager.Instance?.CurrentFloor ?? -1) < 0)
            return; // 맵에서 노드를 아직 선택하지 않음 — 보드 초기화 건너뜀

        var pieceData = DataManager.Instance.Pieces;
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
        InitBoard(ResolveLevelData(pendingLevel));
        pendingLevel = null;
        if (!IsEventLevel)
            TurnManager.instance.StartPlayerTurn(); // 이벤트 레벨은 턴이 흐르지 않음 — TurnManager는 기본 Player 상태로 둠
    }

    // 대화 선택지 등에서 호출: 지정한 전투 레벨로 즉시 진입시키기 위해 씬을 재로드한다.
    public void EnterCombat(LevelData level)
    {
        if (level == null) return;
        DataManager.Instance.SetNextLevelName(level.name); // 껐다 켜도 전투 레벨로 복귀하도록 저장
        SavePlayerPiecesToDataManager(); // 대화 중 받은 피해/회복이 다음 씬에 그대로 이어지도록 먼저 저장
        pendingLevel = level;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // teamID==0인 모든 기물을 회복시킨다. amount가 음수면 각자의 최대 HP까지(풀힐), 0 이상이면 그 수치만큼만 회복시킨다.
    // caster는 힐 애니메이션의 시전자로만 쓰이며 없어도(null) 동작한다 (대화 선택지 등 보드에 선택된 기물이 없는 상황 포함).
    public void HealAllAllies(Piece caster = null, int amount = -1)
    {
        var healedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p == null || p.teamID != 0) continue;

                int requestedAmount = amount < 0 ? p.maxhp - p.hp : amount;
                int healed = p.GetHeal(requestedAmount);
                healedPieces.Add(p);
                textCoroutines.Add(p.HealText(healed));
            }

        if (healedPieces.Count == 0) return;

        motionQueue.Enqueue(PieceAreaHealCor(caster, healedPieces, null, null, textCoroutines));
        StartMotionQueue();
    }

    // teamID==0인 모든 기물에게 amount만큼 피해를 준다. 시전자가 없는 AreaAttackPiece 오버로드를 사용한다.
    public void DamageAllAllies(int amount)
    {
        if (amount <= 0) return;

        var targets = new List<Vector2Int>();
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p != null && p.teamID == 0)
                    targets.Add(new Vector2Int(x, y));
            }

        AreaAttackPiece(targets, amount);
    }

    // 보드 위 생존 기물들의 스탯을 currentData.pieceData로 동기화한다. 덱(deckCardIDs)은 손패/버림/덱 더미를
    // 다시 스캔하지 않고 pieceDataIndex로 이전 저장값을 그대로 이어받으므로(Piece.GetPieceData 참고),
    // 새로 채워질 리스트에서의 위치로 각 기물의 pieceDataIndex를 갱신해줘야 다음 저장에서도 계속 맞물린다.
    public void SavePlayerPiecesToDataManager()
    {
        if (!boardReady) return;
        var surviving = new List<PieceData>();
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                Piece p = GetButtonScript(new Vector2Int(x, y)).GetPieceScript();
                if (p == null || p.teamID != 0) continue;

                PieceData data = p.GetPieceData(); // 이전 pieceDataIndex로 저장된 덱을 읽어온다
                p.pieceDataIndex = surviving.Count; // 새 리스트에서 자기가 놓일 위치로 갱신
                surviving.Add(data);
            }
        }
        DataManager.Instance.SetPieceData(surviving);
    }

    // 씬 재로드 너머로 직접 넘겨받은 LevelData가 있으면 그걸 우선 사용한다 (대화 선택지로 강제 진입하는 전투 등).
    LevelData ResolveLevelData(LevelData explicitLevel)
    {
        return explicitLevel != null ? explicitLevel : ResolveLevelData();
    }

    LevelData ResolveLevelData()
    {
        if (LevelDatabase.instance == null) return leveldata;

        string nextLevel = DataManager.Instance?.NextLevelName;

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
        GameManager.instance?.ClearEnemies();
        GameManager.instance?.ClearAllies();
        enemyPositions.Clear();
        autoAllyPositions.Clear();

        currentLevelData = data;
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
        List<Vector2Int> playerSpawns = new List<Vector2Int>();
        foreach (var p in data.placements)
            if (string.IsNullOrEmpty(p.name)) playerSpawns.Add(p.position);

        int spawnIdx = 0;
        Piece firstAlly = null;
        foreach (PieceData piecedata in DataManager.Instance.Pieces)
        {
            Vector2Int spawnPos = spawnIdx < playerSpawns.Count ? playerSpawns[spawnIdx] : new Vector2Int(2, 2);
            GameObject prefab = piecedatabase.GetPiece(piecedata.pieceName);
            if (prefab == null)
            {
                Debug.LogError($"[Board] 플레이어 기물 스폰 실패: '{piecedata.pieceName}' 을(를) PieceDatabase에서 찾을 수 없습니다. (spawnPos={spawnPos})");
                spawnIdx++;
                continue;
            }
            GameObject piece = Instantiate(prefab);
            GetButtonScript(spawnPos).SetPiece(piece);
            Piece pieceScript = piece.GetComponent<Piece>();
            pieceScript.pieceDataIndex = spawnIdx;
            pieceScript.SetPieceData(piecedata);
            if (firstAlly == null) firstAlly = pieceScript;
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
                GetButtonScript(data.eventObjectPosition).SetPiece(restObj);
            }
        }
        else if (currentEventType == LevelData.EventType.Shop)
        {
            if (ShopObjectPrefab == null)
                Debug.LogError("[Board] 상점 레벨이지만 ShopObjectPrefab이 설정되지 않았습니다.");
            else
            {
                GameObject shopObj = Instantiate(ShopObjectPrefab);
                GetButtonScript(data.eventObjectPosition).SetPiece(shopObj);
            }
            // 아직 팔 물건이 없어 상호작용할 게 없으므로, Rest처럼 뭔가 사용해야 나가기 버튼이 뜨는 방식 대신
            // 진입 즉시 나갈 수 있게 한다(소프트락 방지). 실제 구매 기능이 생기면 이 부분을 재검토.
            EventExitButtonObj?.SetActive(true);
        }
        else if (currentEventType == LevelData.EventType.Unknown)
        {
            dialogueUI?.Show(data.dialogue);
        }

        if (firstAlly != null)
            CardCanvas.instance.SetActivePiece(firstAlly);

        FinishCardUsage();
        ClearSelectedButton();
        CardCanvas.instance.ResetAllAllyHands(GetAllAllyPieces());

        LoadOwnedRelics(); // 이벤트 레벨에서도 보유 유물 아이콘은 보여준다
        if (!IsEventLevel)
            TriggerRelicsOnCombatStart();
    }

    // 보드 위 teamID==0(아군) 기물 전체를 수집한다. 기물별 손패/덱 처리(턴 시작/종료, 카드 뷰 갱신 등)에 쓰인다.
    public List<Piece> GetAllAllyPieces()
    {
        var result = new List<Piece>();
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p != null && p.teamID == 0)
                    result.Add(p);
            }
        return result;
    }

    // 대화 선택지 등에서 새 기물을 보드에 즉시 스폰한다 (합류가 바로 보이도록).
    // 덱이 더 이상 스캔으로 세이브에 반영되지 않으므로, 여기서 currentData.pieceData에 직접 등록한다.
    public bool SpawnPiece(PieceInfo info, List<string> deckCardIDs = null)
    {
        if (info == null) return false;

        Vector2Int? spawnPos = FindEmptyCell();
        if (spawnPos == null)
        {
            Debug.LogError("[Board] 새 기물을 스폰할 빈 칸이 없습니다.");
            return false;
        }

        GameObject prefab = piecedatabase.GetPiece(info.PieceName);
        if (prefab == null)
        {
            Debug.LogError($"[Board] 기물 스폰 실패: '{info.PieceName}' 을(를) PieceDatabase에서 찾을 수 없습니다.");
            return false;
        }

        PieceData data = DataManager.Instance.BuildPieceData(info, deckCardIDs ?? info.DefaultDeckCardIDs);
        int dataIndex = DataManager.Instance.AddPieceData(data);

        GameObject piece = Instantiate(prefab);
        GetButtonScript(spawnPos.Value).SetPiece(piece);
        Piece pieceScript = piece.GetComponent<Piece>();
        pieceScript.pieceDataIndex = dataIndex;
        pieceScript.SetPieceData(data);
        return true;
    }

    Vector2Int? FindEmptyCell()
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
                if (GetPieceAt(new Vector2Int(x, y)) == null)
                    return new Vector2Int(x, y);
        return null;
    }

    Button GetButtonScript(Vector2Int pos)
    {
        return Buttons[pos.x, pos.y].GetComponent<Button>();
    }

    void ClearSelectedButton()
    {
        if (selectedButton.x != -1 && selectedButton.y != -1)
            GetButtonScript(selectedButton).SelectedFalse();
        selectedButton = new Vector2Int(-1, -1);
    }

    bool isSelectedButtonActive()
    {
        return selectedButton.x >= 0 && selectedButton.y >= 0;
    }

    public Piece casterPiece;

    // baseColDamage(강화 전 수치)를 뺀 나머지 — 영구 강화분(colDamageBonus)과 이번 전투의 임시 버프를 합친 값
    public int CasterColDamage => CardCanvas.instance?.ActivePiece?.ColDamageDelta ?? 0;
    public int CasterShieldBonus => CardCanvas.instance?.ActivePiece?.ShieldBonusDelta ?? 0;
    // useColDamageAsDmg 카드(예: ColDamageAttackCard)용 — 강화 전 기본치까지 포함한 이동공격력 전체 수치
    public int CasterFullColDamage => CardCanvas.instance?.ActivePiece?.colDamage ?? 0;

    public Piece GetPieceAt(Vector2Int pos) => GetButtonScript(pos)?.GetPieceScript();

    public void HoverPieceFromUI(Piece piece)
    {
        GetButtonForPiece(piece)?.MouseEnter();
    }

    public void UnhoverPieceFromUI(Piece piece)
    {
        GetButtonForPiece(piece)?.MouseExit();
    }

    // CardCanvas가 이 기물의 덱을 보여주고 있음을 그 기물이 놓인 칸 아래에 표시/해제한다.
    // 끌 때 피스의 "현재" 위치를 다시 찾으면 그 사이 피스가 이동한 경우 엉뚱한 칸을 끄게 되어(원래 칸은 계속
    // 켜진 채로 남음) 켰던 버튼 자체를 기억해뒀다가 그대로 끈다. 이동은 PieceMoveCor에서 같이 옮겨준다.
    Button pieceDeckIndicatorButton;

    public void SetPieceDeckIndicator(Piece piece, bool active)
    {
        if (active)
        {
            pieceDeckIndicatorButton = GetButtonForPiece(piece);
            pieceDeckIndicatorButton?.SetDeckActive(true);
        }
        else
        {
            pieceDeckIndicatorButton?.SetDeckActive(false);
            pieceDeckIndicatorButton = null;
        }
    }

    // 시전자 표시와 마찬가지로, 덱 표시가 켜진 버튼(button1)에서 다른 칸(button2)으로 피스가 실제로
    // 이동했을 때 표시도 함께 옮긴다.
    void RelocatePieceDeckIndicator(Button button1, Button button2)
    {
        if (pieceDeckIndicatorButton != button1) return;
        button1.SetDeckActive(false);
        button2.SetDeckActive(true);
        pieceDeckIndicatorButton = button2;
    }

    Button GetButtonForPiece(Piece piece)
    {
        if (piece == null) return null;
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Button button = GetButtonScript(new Vector2Int(x, y));
                if (button.GetPieceScript() == piece) return button;
            }
        return null;
    }

    void ResetPieceMovedThisTurn()
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p != null && p.teamID == 0)
                    p.movedThisTurn = false;
            }
    }
}
