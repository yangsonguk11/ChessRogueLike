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
    Queue<IEnumerator> motionQueue = new Queue<IEnumerator>();
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
        GetButtonScript(new Vector2(3, 3)).SetPiece(Instantiate(Pieces[1]));
    }

    public void ButtonClicked(Vector2 pos)
    {
        if (coroutineworking)
            return;
        if (selectedButton.x < 0 || selectedButton.y < 0)   //선택된 칸이 비어있을 때
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
            if (GetButtonScript(selectedButton).GetPiece().GetComponent<Piece>().teamID == 1) { }   //적이 선택되었을 때
            else if (selectedButtonMovable.Contains(pos))
                MovePiece(selectedButton, pos);
        }

    }

    void MovePiece(Vector2 pos1, Vector2 pos2)      //기물이 이동을 시도
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
            GameObject Piece1 = button1script.GetPiece();
            GameObject Piece2 = button2script.GetPiece();
            if (Piece2)
            {
                if(Piece2.GetComponent<Piece>().teamID == 1)
                    MoveAttack(button1script.GetPiece().GetComponent<Piece>(), button2script.GetPiece().GetComponent<Piece>(), button1script, button2script);
            }
            else 
            {
                motionQueue.Enqueue(PieceMoveCor(button1script, button2script, 1f));
                StartCoroutine(ProcessQueue());
            }
        }

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }
    void MoveAttack(Piece pScript1, Piece pScript2, Button bScript1, Button bScript2)
    {
        int dmg = pScript1.colDamage;
        int hpLeft = pScript2.GetDamage(dmg, AttackType.MoveAttack);
        if (hpLeft <= 0)
        {
            motionQueue.Enqueue(MoveAdjacent(bScript1, bScript2, 1f));
            motionQueue.Enqueue(pScript2.DeathCor());
            motionQueue.Enqueue(PieceMoveCor(GetButtonScript(GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation())), bScript2, 1f));
        }
        else
            motionQueue.Enqueue(MoveAdjacent(bScript1, bScript2, 1f));
        StartCoroutine(ProcessQueue());
    }
    IEnumerator PieceMoveCor(Button Button1, Button Button2, float moveDuration)
    {
        Vector3 pos1 = Button1.Piecelocation;
        Vector3 pos2 = Button2.Piecelocation;
        GameObject piece = Button1.GetPiece();
        if (Button1 == Button2)
            yield break;
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
    }

    IEnumerator MoveAdjacent(Button Button1, Button Button2, float moveDuration)
    {
        Button newTargetButton;
        Vector2 location1 = Button1.GetLocation();
        Vector2 location2 = Button2.GetLocation();
        Vector2 temp = location2 - location1;
        newTargetButton = GetButtonScript(GetAdjacentLocation(location1, location2));
        yield return PieceMoveCor(Button1, newTargetButton, moveDuration);
    }

    Vector2 GetAdjacentLocation(Vector2 location1, Vector2 location2)
    {
        Vector2 temp = location2-location1;
        if (Math.Abs(temp.x) == Math.Abs(temp.y))
        {
            if (temp.x < 0) temp.x = -1;
            else if (temp.x > 0) temp.x = 1;

            if (temp.y < 0) temp.y = -1;
            else if (temp.y > 0) temp.y = 1;
        }
        else if (Math.Abs(temp.x) > Math.Abs(temp.y))
        {
            if (temp.x < 0) temp.x = -1;
            else if (temp.x > 0) temp.x = 1;

            temp.y = 0;
        }
        else if (Math.Abs(temp.x) < Math.Abs(temp.y))
        {
            temp.x = 0;

            if (temp.y < 0) temp.y = -1;
            else if (temp.y > 0) temp.y = 1;
        }
        return location2 - temp;
    }
    private IEnumerator ProcessQueue()
    {
        coroutineworking = true;
        Debug.Log(motionQueue.Count);
        while (motionQueue.Count > 0)
        {
            // 큐에서 다음 연출을 꺼냄
            IEnumerator nextAction = motionQueue.Dequeue();
            Debug.Log(nextAction);

            // 해당 연출이 끝날 때까지 대기
            yield return StartCoroutine(nextAction);
        }

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
