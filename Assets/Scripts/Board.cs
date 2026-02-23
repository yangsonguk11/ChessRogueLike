using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    [SerializeField] GameObject[,] Buttons;
    [Header("보드 크기")]
    [Min(1)] public int N; //가로
    [Min(1)] public int M; //세로
    public Vector2 selectedButton;
    private void Start()
    {
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
    }

    public void ButtonClicked(Vector2 pos)
    {
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
            button2script.SetPiece(button1script.GetPiece());
            button1script.RemovePiece();
        }

        selectedButton = new Vector2(-1, -1);
        button1script.SelectedFalse();
        button2script.SelectedFalse();
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
