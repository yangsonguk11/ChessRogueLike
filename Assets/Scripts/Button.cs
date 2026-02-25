using System.Collections;
using UnityEngine;

public class Button : MonoBehaviour
{
    Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;
    bool selected;
    [SerializeField] GameObject piece;
    [SerializeField] GameObject RangeObj;
    GameObject board;
    Vector2 location;
    Vector3 piecelocation;
    public Vector3 Piecelocation
    {
        get { return piecelocation; }
    }

    private void Start()
    {
        selected = false;
        defaultScale = transform.localScale;
        piecelocation = transform.localPosition;
        piecelocation.y = 0.6f;
    }

    public void Init(int x, int y, GameObject _board)
    {
        location.x = x; location.y = y; board = _board;
    }
    public void MouseEnter()
    {
        ScaleHover();
    }

    public void MouseExit()
    {
        if(!selected) ScaleDefault();
    }
    public void MouseDown()
    {
        board.GetComponent<Board>().ButtonClicked(location);
    }


    public GameObject GetPiece() { return piece; }

    public void SetPiece(GameObject obj)
    {
        piece = obj;
        piece.transform.position = piecelocation;
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

    public void RangeOn(){ RangeObj.SetActive(true); }
    public void RangeOff(){  RangeObj.SetActive(false); }
    IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }
    void ScaleDefault()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale));
    }
    void ScaleHover()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale * hoverScale));
    }
}
