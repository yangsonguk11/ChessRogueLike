using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board : MonoBehaviour
{
    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    GameObject[,] Buttons;
    [SerializeField] GameObject BoardUICanvas;
    [SerializeField] PieceDatabase piecedatabase;
    event Action OnButtonSelected;
    event Action OnButtonUnSelected;

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
        OnButtonSelected += OnSelectBoard;
        OnButtonUnSelected += OnUnSelectBoard;
        queuecoroutineworking = false;
        InitBoard(leveldata);
        TurnStart();
    }

    void InitBoard(LevelData data)
    {
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

        foreach (PieceData piecedata in DataManager.Instance.currentData.pieceData)
        {
            Debug.LogFormat("{0} {1}", piecedata.pieceName, piecedatabase.PiecePrefabs[0]);
            GameObject piece = Instantiate(piecedatabase.GetPiece(piecedata.pieceName));
            GetButtonScript(new Vector2(2, 2)).SetPiece(piece);
            piece.GetComponent<Piece>().SetPieceData(piecedata);
        }

        ClearSelectedButton();
        foreach (var placement in data.placements)
        {
            Debug.LogFormat("{0} {1}", piecedatabase, placement.name);
            GameObject piece = Instantiate(piecedatabase.GetPiece(placement.name));
            GetButtonScript(placement.position).SetPiece(piece);

            if (placement.isEnemy)
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
