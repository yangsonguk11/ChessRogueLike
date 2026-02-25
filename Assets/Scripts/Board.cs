using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] GameObject Background;
    [SerializeField] GameObject ButtonPrefab;
    [SerializeField] GameObject[,] Buttons;
    [SerializeField] GameObject[] Pieces;

    event Action OnButtonSelected;
    event Action OnButtonUnSelected;
    [Header("보드 크기")]
    [Min(1)] public int N; //세로
    [Min(1)] public int M; //가로
    Vector2 _selectedButton;
    Vector2 selectedButton
    {
        get { return _selectedButton; }
        set { _selectedButton = value; if (isSelectedButtonActive()) OnButtonSelected.Invoke(); else OnButtonUnSelected.Invoke(); }
    }
    List<Vector2> selectedButtonMovable = new List<Vector2>();
    bool coroutineworking;
    private void Start()
    {
        OnButtonSelected += ShowMovableButtons;
        OnButtonUnSelected += HideMovableButtons;
        coroutineworking = false;
        InitBoard();
    }

    void InitBoard()
    {
        Background.transform.localScale = new Vector3(N, M, 1);
        Buttons = new GameObject[N, M];
        Vector3 pos = new Vector3(0,0,0);
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
        ClearSelectedButton();
        GetButtonScript(new Vector2(2, 2)).SetPiece(Instantiate(Pieces[0]));
    }

    public void ButtonClicked(Vector2 pos)
    {
        if (coroutineworking)
            return;
        if (selectedButton.x < 0 || selectedButton.y < 0)
        {
            if (GetButtonScript(pos).IsSelectable())
            {
                selectedButton = pos;
                GetButtonScript(pos).SelectedTrue();
            }
        }
        else if (selectedButton == pos)
        {
            ClearSelectedButton();
            GetButtonScript(pos).SelectedFalse();
        }
        else
        {
            if(selectedButtonMovable.Contains(pos))
                MovePiece(selectedButton, pos);
        }

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

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }
    IEnumerator PieceMoveCor(Button Button1, Button Button2, float moveDuration)
    {
        coroutineworking = true;
        Vector3 pos1 = Button1.Piecelocation;
        Vector3 pos2 = Button2.Piecelocation;
        GameObject piece = Button1.GetPiece();
        Debug.Log(pos2-pos1);
        piece.transform.rotation = Quaternion.LookRotation(pos1 - pos2);
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

    void ClearSelectedButton()
    {
        selectedButton = new Vector2(-1, -1);
    }

    bool isSelectedButtonActive()
    {
        if (selectedButton.x < 0 || selectedButton.y < 0)
            return false;
        else
            return true;
    }
    void ShowMovableButtons()
    {
        GameObject p = GetButtonScript(selectedButton).GetPiece();

        if (p == null)
            return;

        Piece piece = GetButtonScript(selectedButton).GetPiece().GetComponent<Piece>();
        List<Vector2> list = piece.GetMoveableButton();
        selectedButtonMovable.Clear();

        foreach(Vector2 v in list)
        {
            Vector2 m = selectedButton + v;
            Debug.Log(selectedButton);
            Debug.Log(v);
            if (m.x < 0 || m.x >= N || m.y < 0 || m.y >= M)
                continue;
            GetButtonScript(m).RangeOn();
            selectedButtonMovable.Add(m);
        }
        /*
        for(int i = (int)org.x - 1; i <= org.x + 1; i++)
        {
            if (i < 0 || i >= N)
                continue;
            for(int j = (int)org.y - 1; j <= org.y + 1; j++)
            {
                if (j < 0 || j >= M)
                    continue;
                GetButtonScript(new Vector2(i, j)).RangeOn();
            }
        }
        */
    }
    void HideMovableButtons()
    {
        foreach (Vector2 v in selectedButtonMovable)
        {
            GetButtonScript(v).RangeOff();
        }

    }
}
