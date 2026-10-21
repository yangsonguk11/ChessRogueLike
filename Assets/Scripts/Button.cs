using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Button : MonoBehaviour, ISelectable
{
    [SerializeField] GameObject piece;
    [SerializeField] GameObject AllyRangeObj;
    [SerializeField] GameObject EnemyRangeObj;
    [SerializeField] GameObject SelectedObj;
    [SerializeField] GameObject DeckActiveObj; // CardCanvas가 이 기물의 덱을 보여주고 있을 때 표시
    [SerializeField] GameObject CasterIndicatorObj; // 이 기물이 지금 든 카드의 시전자일 때 표시
    GameObject board;
    Board boardScript;
    Vector2Int location;
    public Vector3 Piecelocation
    {
        get {
            return transform.position;
        }
    }

    public bool _selected;
    public bool selected { get { return _selected; } set { _selected = value; } }

    private void Awake()
    {
        selected = false;
        defaultScale = transform.localScale;
    }

    public void Init(int x, int y, GameObject _board)
    {
        SetLocation(x, y); board = _board;
        boardScript = _board.GetComponent<Board>();
    }

    public void MouseEnter()
    {
        ScaleHover();
        boardScript?.ButtonHovered(location);
        boardScript?.ShowTileTooltip(this);
    }

    public void MouseExit()
    {
        if(!selected) ScaleDefault();
        boardScript?.ButtonUnhovered();
        boardScript?.HideTileTooltip();
    }
    public void MouseDown()
    {
        if (Mouse.current.rightButton.isPressed) return;
        board.GetComponent<Board>().ButtonClicked(location);
    }


    public void SetDeckActive(bool active) => DeckActiveObj?.SetActive(active);

    // 범위 표시(파랑=아군 범위, 빨강=적 범위, 초록=시전자). 무엇을 켤지는 Board.RefreshRangeDisplay가
    // 우선순위에 따라 정해서 모든 칸에 한꺼번에 적용한다 — 칸이 직접 상태를 쌓지 않는다.
    // edges/corners는 RangeHatch 셰이더가 범위 영역의 바깥쪽에만 테두리를 그리도록 넘기는 마스크다(Board.RangeOutline).
    public void SetRangeVisual((bool on, int edges, int corners) ally, (bool on, int edges, int corners) enemy,
        (bool on, int edges, int corners) caster)
    {
        ApplyRangeLayer(AllyRangeObj, ally);
        ApplyRangeLayer(EnemyRangeObj, enemy);
        ApplyRangeLayer(CasterIndicatorObj, caster);
    }

    static readonly int EdgeMaskId = Shader.PropertyToID("_EdgeMask");
    static readonly int CornerMaskId = Shader.PropertyToID("_CornerMask");
    MaterialPropertyBlock rangeBlock;

    void ApplyRangeLayer(GameObject layer, (bool on, int edges, int corners) state)
    {
        if (layer == null) return;
        layer.SetActive(state.on);
        if (!state.on) return;

        Renderer rangeRenderer = layer.GetComponent<Renderer>();
        if (rangeRenderer == null) return;
        rangeBlock ??= new MaterialPropertyBlock();
        rangeRenderer.GetPropertyBlock(rangeBlock);
        rangeBlock.SetFloat(EdgeMaskId, state.edges);
        rangeBlock.SetFloat(CornerMaskId, state.corners);
        rangeRenderer.SetPropertyBlock(rangeBlock);
    }

    public GameObject GetPiece() { if (piece) return piece; else return null; }
    public Piece GetPieceScript() { if (piece) return piece.GetComponent<Piece>(); else return null; }

    public void SetPiece(GameObject obj)
    {
        SetPieceLogical(obj);
        SnapPieceToCell();
        AttachPieceVisual(obj);
    }

    // 점유(논리) 대입만 — transform은 건드리지 않는다. 이동 애니메이션이 재생되는 동안에도
    // "이 칸엔 이제 이 기물이 있다"는 판정을 즉시 성립시키기 위해 시각적 이동과 분리해서 쓴다.
    public void SetPieceLogical(GameObject obj)
    {
        piece = obj;
    }

    // 월드 좌표를 유지한 채(순간이동 없이) 부모만 이 버튼으로 옮긴다 — 이동 트윈이 끝난 뒤 호출용.
    public void AttachPieceVisual(GameObject obj)
    {
        obj.transform.SetParent(transform, true);
    }

    // 현재 piece를 이 칸의 정확한 위치로 즉시 스냅 — 트윈 종료 마무리, 또는 순간이동이 맞는 배치용.
    public void SnapPieceToCell()
    {
        if (piece != null)
            piece.transform.position = transform.position;
    }

    public void RemovePiece()
    {
        piece = null;
    }

    public bool IsSelectable()
    {
        return piece != null;
    }
    public void SelectedFalse()
    {
        selected = false;
        ScaleDefault();
    }
    public void SelectedTrue()
    {
        selected = true;
        ScaleHover();
    }

    public Vector2Int GetLocation() { return location; }
    void SetLocation(int _x, int _y)
    {
        location.x = _x; location.y = _y;
    }

    public Vector3 defaultScale;
    public float hoverScale = 1.1f;
    float speed = 10f;
    public IEnumerator ScaleTo(Vector3 target) => ScaleAnimator.ScaleTo(transform, target, speed);
    public void ScaleDefault()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale));
        SelectedObj.SetActive(false);
    }
    public void ScaleHover()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale * hoverScale));
        SelectedObj.SetActive(true);
    }
}
