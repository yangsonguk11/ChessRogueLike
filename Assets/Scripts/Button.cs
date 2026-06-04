using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Button : MonoBehaviour, ISelectable
{
    [SerializeField] GameObject piece;
    [SerializeField] GameObject AllyRangeObj;
    [SerializeField] GameObject EnemyRangeObj;
    [SerializeField] GameObject SelectedObj;
    GameObject board;
    Board boardScript;
    Vector2 location;
    Vector3 piecelocation;
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
        piecelocation = boardScript.grid.GetCellCenterWorld(new Vector3Int((int)location.x, 0, (int)location.y));
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


    public GameObject GetPiece() { if (piece) return piece; else return null; }
    public Piece GetPieceScript() { if (piece) return piece.GetComponent<Piece>(); else return null; }

    public void SetPiece(GameObject obj)
    {
        piece = obj;
        piece.transform.position = transform.position;
        piece.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        piece.transform.parent = gameObject.transform;
    }
    
    public void RemovePiece()
    {
        piece = null;
    }

    public bool IsSelectable()
    {
        if (piece != null)
            return true;
        return false;
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
    public Vector2 GetLocation() { return location; }
    void SetLocation(int _x, int _y)
    {
        location.x = _x; location.y = _y;
    }

    public Vector3 defaultScale;
    public float hoverScale = 1.1f;
    float speed = 10f;
    public IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }
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
