using System.Collections;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    [SerializeField] GameObject[,] Buttons;
    [SerializeField] GameObject[] Pieces;

    [Header("보드 크기")]
    [Min(1)] public int N; //가로
    [Min(1)] public int M; //세로
    public Vector2 selectedButton;
    bool coroutineworking;
    private void Start()
    {
        coroutineworking = false;
        InitBoard();
    }

    void InitBoard()
    {
        Background.transform.localScale = new Vector3(N, M, 1);
        Buttons = new GameObject[N, M];
        Vector3 pos = new Vector3(0,0,0);
        selectedButton = new Vector2(-1, -1);
        for(int x = 0; x < N; x++)
        {
            for(int y = 0; y < M; y++)
            {
                pos.x = 0.5f - N/2f + x; pos.z = 0.5f - M/2f + y;
                GameObject obj = Instantiate(ButtonPrefab, pos, new Quaternion(),gameObject.transform);
                obj.GetComponent<Button>().Init(x, y, gameObject);

                Buttons[x, y] = obj;
            }
        }
        GetButtonScript(new Vector2(2, 2)).SetPiece(Instantiate(Pieces[0]));
    }

    public void ButtonClicked(Vector2 pos)
    {
        if (coroutineworking)
            return;
        if (selectedButton.x < 0 || selectedButton.y < 0)
        {
            selectedButton = pos;
            GetButtonScript(pos).SelectedTrue();
        }
        else if (selectedButton == pos)
        {
            selectedButton = new Vector2(-1, -1);
            GetButtonScript(pos).SelectedFalse();
        }
        else
            MovePiece(selectedButton, pos);

        Debug.Log("selected" + selectedButton);
        Debug.Log("pos" + pos);
    }

    void MovePiece(Vector2 pos1, Vector2 pos2)
    {
        GameObject button1 = GetButton(pos1);
        GameObject button2 = GetButton(pos2);
        Button button1script = button1.GetComponent<Button>();
        Button button2script = button2.GetComponent<Button>();

        if (button1script.GetPiece() == null)
        {
        }
        else
        {
            StartCoroutine(PieceMoveCor(button1script, button2script, 1f));
        }

        selectedButton = new Vector2(-1, -1);
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }
    IEnumerator PieceMoveCor(Button Button1, Button Button2, float moveDuration)
    {
        coroutineworking = true;
        Vector3 pos1 = Button1.Piecelocation;
        Vector3 pos2 = Button2.Piecelocation;
        GameObject piece = Button1.GetPiece();
        float time = 0f;

        while(time < moveDuration)
        {
            time += Time.deltaTime;
            float t = time / moveDuration;

            piece.transform.position = Vector3.Lerp(pos1, pos2, t);
            yield return null;
        }
        piece.transform.position = pos2;
        Button2.SetPiece(Button1.GetPiece());
        Button1.RemovePiece();
        coroutineworking = false;
    }
    GameObject GetButton(Vector2 pos)
    {
        return Buttons[(int)pos.x, (int)pos.y];
    }
    Button GetButtonScript(Vector2 pos)
    {
        return Buttons[(int)pos.x, (int)pos.y].GetComponent<Button>();
    }
}
