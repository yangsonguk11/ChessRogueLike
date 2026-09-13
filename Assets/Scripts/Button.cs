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

    public void OnTurnEnd()
    {
        if (piece)
            piece.GetComponent<Piece>().OnTurnEnd();
    }
    public void MouseEnter()
    {
        ScaleHover();
        boardScript?.ButtonHovered(location);
    }

    public void MouseExit()
    {
        if(!selected) ScaleDefault();
        boardScript?.ButtonUnhovered();
    }
    public void MouseDown()
    {
        if (Mouse.current.rightButton.isPressed) return;
        board.GetComponent<Board>().ButtonClicked(location);
    }


    public void SetDeckActive(bool active) => DeckActiveObj?.SetActive(active);
    public void SetCasterIndicator(bool active) => CasterIndicatorObj?.SetActive(active);

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

    int allyRangeRefCount = 0;
    int enemyRangeRefCount = 0;

    public void RangeOn(int teamID)
    {
        if (teamID == 0) { allyRangeRefCount++;  AllyRangeObj.SetActive(true); }
        else             { enemyRangeRefCount++; EnemyRangeObj.SetActive(true); }
    }
    public void RangeOff(int teamID)
    {
        if (teamID == 0)
        {
            allyRangeRefCount = Mathf.Max(0, allyRangeRefCount - 1);
            if (allyRangeRefCount == 0) AllyRangeObj.SetActive(false);
        }
        else
        {
            enemyRangeRefCount = Mathf.Max(0, enemyRangeRefCount - 1);
            if (enemyRangeRefCount == 0) EnemyRangeObj.SetActive(false);
        }
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
