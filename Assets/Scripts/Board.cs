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
    [SerializeField] GameObject BoardUICanvas;
    event Action OnButtonSelected;
    event Action OnButtonUnSelected;
    [Header("보드 크기")]
    [Min(1)] public int N; //세로
    [Min(1)] public int M; //가로
    Vector2 _selectedButton;
    Vector2 selectedButton
    {
        get { return _selectedButton; }
        set { _selectedButton = value; if (isSelectedButtonActive()) OnButtonSelected?.Invoke(); else OnButtonUnSelected?.Invoke(); }
    }

    List<Vector2> selectedButtonMovable = new List<Vector2>();
    Queue<IEnumerator> motionQueue = new Queue<IEnumerator>();
    bool queuecoroutineworking;

    public enum BoardMode
    {
        Inspect,
        command,
        targeting
    }

    BoardMode boardmode;
    private void Start()
    {
        OnButtonSelected += OnSelectBoard;
        OnButtonUnSelected += OnUnSelectBoard;
        queuecoroutineworking = false;
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

        if (queuecoroutineworking)
            return;

        //Inspect Mode
        if (boardmode == BoardMode.Inspect)
        {
            if (selectedButton.x >= 0 && selectedButton.y >= 0)   //이미 선택된 칸이 있을 때 
            {
                GetButtonScript(selectedButton).SelectedFalse();
                ClearSelectedButton();
            }

            if (GetButtonScript(pos).IsSelectable())
            {
                selectedButton = pos;
                GetButtonScript(pos).SelectedTrue();
            }
        }
        else if(boardmode == BoardMode.command)
        {
            if (selectedButton.x < 0 || selectedButton.y < 0)   //이미 선택된 칸이 비어있을 때
            {
                if (GetButtonScript(pos).IsSelectable())
                {
                    selectedButton = pos;
                    GetButtonScript(pos).SelectedTrue();
                }
            }
            else if (selectedButton == pos || !selectedButtonMovable.Contains(pos))
            {
                GetButtonScript(selectedButton).SelectedFalse();
                ClearSelectedButton();
            }
            else
            {
                if (GetButtonScript(selectedButton).GetPiece().GetComponent<Piece>().teamID == 1) { }   //적이 선택되었을 때
                else if (selectedButtonMovable.Contains(pos))
                {
                    ExecuteEffect(pendingEffects.Dequeue(), pos);
                    ProcessNextCardEffect();
                }
            }
        }
        else if (boardmode == BoardMode.targeting)
        {

        }

    }
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard; // 현재 사용 중인 카드 참조

    public void UseCard(Card card)
    {
        currentActiveCard = card;
        pendingEffects.Clear();

        // 카드가 가진 효과들을 큐에 담음
        foreach (var effect in card.effects)
        {
            pendingEffects.Enqueue(effect);
        }

        ProcessNextCardEffect();
    }

    void ProcessNextCardEffect()
    {
        if (pendingEffects.Count == 0)
        {
            FinishCardUsage();
            return;
        }
        CardEffect nextEffect = pendingEffects.Peek(); // 다음에 실행할 효과 확인

        // 1. 모드 전환
        boardmode = nextEffect.requiredMode;

        // 2. 만약 조준이 필요한 모드라면 여기서 중단 (유저 입력을 기다림)
        if (boardmode == BoardMode.command)
        {
            
        }
        else if (boardmode == BoardMode.targeting)
        {

        }
        else
        {
            ExecuteEffect(pendingEffects.Dequeue());
            ProcessNextCardEffect(); // 다음 효과로 넘어감
        }
    }

    void ExecuteEffect(CardEffect cardEffect, Vector2 targetPos = default)
    {
        switch (cardEffect.type)
        {
            case EffectType.Move:
                // 기존 MovePiece 로직 연결
                MovePiece(selectedButton, targetPos);
                break;
        }
    }
    void FinishCardUsage()
    {
        currentActiveCard.cardCanvas.GetComponent<CardCanvas>().FinishUseCard();
        boardmode = BoardMode.Inspect;
        ClearSelectedButton();
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
        queuecoroutineworking = true;
        while (motionQueue.Count > 0)
        {
            // 큐에서 다음 연출을 꺼냄
            IEnumerator nextAction = motionQueue.Dequeue();

            // 해당 연출이 끝날 때까지 대기
            yield return StartCoroutine(nextAction);
        }

        queuecoroutineworking = false;
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

    void OnSelectBoard()
    {
        ShowMovableButtons(GetButtonScript(selectedButton).GetPiece());
        ShowButtonInfo();
    }
    void OnUnSelectBoard()
    {
        HideMovableButtons();
        HideButtonInfo();
    }
    void ShowMovableButtons(GameObject p)
    {
        if (p == null)
            return;

        Piece piece = p.GetComponent<Piece>();
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
    }

    void HideMovableButtons()
    {
        foreach (Vector2 v in selectedButtonMovable)
        {
            GetButtonScript(v).RangeOff();
        }

    }
    void ShowButtonInfo()
    {
        BoardUICanvas.SetActive(true);
    }

    void HideButtonInfo()
    {
        BoardUICanvas.SetActive(false);
    }
}
