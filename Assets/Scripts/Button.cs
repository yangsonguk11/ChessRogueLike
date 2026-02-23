using System.Collections;
using UnityEngine;

public class Button : MonoBehaviour
{
    Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;
    bool selected;
    [SerializeField] GameObject piece;
    GameObject board;
    Vector2 location;
    private void Start()
    {
        selected = false;
        defaultScale = transform.localScale;
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

    IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }

    public GameObject GetPiece()
    {
        return piece;
    }

    public void SetPiece(GameObject obj)
    {
        piece = obj;
        piece.transform.position = transform.position;
        piece.transform.parent = gameObject.transform;
    }
    
    public void RemovePiece()
    {
        piece = null;
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
