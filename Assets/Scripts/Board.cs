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

    public List<Vector2> enemyPositions = new List<Vector2>();
    [Header("보드 크기")]
    [Min(1)] public int N; //세로
    [Min(1)] public int M; //가로
    Vector2 _selectedButton;
    Vector2 selectedButton
    {
        get { return _selectedButton; }
        set { _selectedButton = value; if (isSelectedButtonActive()) { OnButtonSelected?.Invoke(); ShowButtonInfo(); } else { OnButtonUnSelected?.Invoke(); HideButtonInfo(); } }
    }

    List<Vector2> selectedButtonMovable = new List<Vector2>();
    Queue<IEnumerator> motionQueue = new Queue<IEnumerator>();
    public bool queuecoroutineworking;

    public enum BoardMode
    {
        Inspect,
        command,
        targeting,
        enemy
    }

    public BoardMode boardmode;
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
        GetButtonScript(new Vector2(2, 4)).SetPiece(Instantiate(Pieces[0]));
        GetButtonScript(new Vector2(1, 4)).SetPiece(Instantiate(Pieces[1]));
        GetButtonScript(new Vector2(1, 2)).SetPiece(Instantiate(Pieces[1]));
        enemyPositions.Add(new Vector2(1, 4));
        enemyPositions.Add(new Vector2(1, 2));
    }

    public void ButtonClicked(Vector2 pos)
    {

        if (TurnManager.instance.currentState != TurnState.Player) return;

        switch (boardmode)
        {
            case BoardMode.Inspect:
                if (selectedButton == pos)   //자신 선택 시
                {
                    ClearSelectedButton();
                    return;
                }
                if (selectedButton.x >= 0 && selectedButton.y >= 0)   //이미 선택된 칸이 있을 때 
                {
                    ClearSelectedButton();
                }

                if (GetButtonScript(pos).IsSelectable())
                {
                    selectedButton = pos;
                    GetButtonScript(pos).SelectedTrue();
                }
                break;
            case BoardMode.command:
                if (selectedButton.x < 0 || selectedButton.y < 0)   //이미 선택된 칸이 비어있을 때
                {
                    if (GetButtonScript(pos).IsSelectable())
                    {
                        selectedButton = pos;
                        GetButtonScript(pos).SelectedTrue();
                    }
                }
                else if (selectedButton == pos || !selectedButtonMovable.Contains(pos)) //자기자신 혹은 유효하지 않은 칸 눌러 취소
                {
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
                break;
            case BoardMode.targeting:
                if (selectedButton.x < 0 || selectedButton.y < 0)   //이미 선택된 칸이 비어있을 때
                {
                    if (GetButtonScript(pos).IsSelectable())
                    {
                        selectedButton = pos;
                        GetButtonScript(pos).SelectedTrue();
                    }
                }
                else if (selectedButton == pos || !selectedButtonMovable.Contains(pos)) //자기자신 혹은 유효하지 않은 칸 눌러 취소
                {
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
                break;
        }
    }
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard; // 현재 사용 중인 카드 참조

    public void UseCard(Card card, Vector2 TargetPos = default)
    {
        currentActiveCard = card;
        pendingEffects.Clear();

        // 카드가 가진 효과들을 큐에 담음
        foreach (var effect in card.effects)
        {
            pendingEffects.Enqueue(effect);
        }

        ProcessNextCardEffect(TargetPos);
    }

    void ProcessNextCardEffect(Vector2 TargetPos = default)
    {
        if (pendingEffects.Count == 0)
        {
            FinishCardUsage();
            return;
        }
        CardEffect nextEffect = pendingEffects.Peek(); // 다음에 실행할 효과 확인

        // 1. 모드 전환
        boardmode = nextEffect.requiredMode;

        if (TurnManager.instance.currentState == TurnState.Enemy)
        {
            if(boardmode == BoardMode.command)
            {
                ExecuteEffect(pendingEffects.Dequeue(), TargetPos);
                ProcessNextCardEffect();
            }
            return;
        }


        
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
        currentActiveCard.cardCanvas.GetComponent<CardCanvas>().isCardEffecting = true;
        switch (cardEffect.type)
        {
            case EffectType.Move:
                MovePiece(selectedButton, targetPos);
                break;
            case EffectType.Damage:
                AttackPiece(selectedButton, targetPos, cardEffect.dmg);
                break;
        }
    }
    void TurnEnd()
    {
        FinishCardUsage();
        ClearSelectedButton();
    }
    void ResetBoardAfterCardUse()
    {
        boardmode = BoardMode.Inspect;
        ClearSelectedButton();
        Debug.Log("Finished Card Use");
    }
    void FinishCardUsage()
    {
        CardCanvas.instance.FinishUseCard();
        ResetBoardAfterCardUse();
    }

    void PlayEnemyTurn()
    {
        Debug.Log(enemyPositions[0]);
        StartCoroutine(PlayEnemyTurnCoroutine());
        /*
        foreach (Vector2 pos in enemyPositions)
        {
            selectedButton = pos;

            var (card, target) = GetButtonScript(pos).GetPiece().GetComponent<Enemy>().DecideCardAndTarget(this, pos);
            if(card != null)
            {
                UseCard(card, target);
            }
        }
        */
    }
    public IEnumerator PlayEnemyTurnCoroutine()
    {
        // 1. 적 목록을 복사해서 사용 (행동 중 위치가 바뀌어도 안전하게)
        List<Vector2> currentEnemies = new List<Vector2>(enemyPositions);

        int i = 0;
        foreach (Vector2 pos in currentEnemies)
        {
            // 적 기물이 실제로 존재하는지 확인
            Piece p = GetButtonScript(pos).GetPiece()?.GetComponent<Piece>();
            if (p == null || p is not Enemy enemy) continue;

            // 현재 행동 중인 적을 강조
            selectedButton = pos;

            // 2. 적 AI로부터 카드와 타겟을 결정받음
            var (card, target) = enemy.DecideCardAndTarget(this, pos);

            if (card != null)
            {
                // 3. 카드 사용 시작
                UseCard(card, target);

                // 4. [중요] 카드의 모든 효과와 애니메이션(motionQueue)이 끝날 때까지 대기
                // pendingEffects가 비어있고, queuecoroutineworking이 false가 될 때까지 기다림
                yield return new WaitUntil(() => pendingEffects.Count == 0 && !queuecoroutineworking);
            }
            // 한 적의 행동이 끝난 후 잠시 대기 (연출상 자연스러움)
            yield return new WaitForSeconds(0.5f);
            ClearSelectedButton();
            i++;
        }

        // 5. 모든 적의 턴이 종료되면 플레이어 턴으로 전환
        TurnManager.instance.StartPlayerTurn();
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
                if(Piece1.GetComponent<Piece>().teamID != Piece2.GetComponent<Piece>().teamID)
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
        Debug.Log(hpLeft);
        motionQueue.Enqueue(MoveAdjacent(bScript1, bScript2, 1f));
        if (hpLeft <= 0)
        {
            motionQueue.Enqueue(pScript2.DeathCor());
            motionQueue.Enqueue(PieceMoveCor(GetButtonScript(GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation())), bScript2, 1f));
        }
        StartCoroutine(ProcessQueue());
    }

    void AttackPiece(Vector2 pos1, Vector2 pos2, int dmg)
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
                Piece pScript1 = Piece1.GetComponent<Piece>();
                Piece pScript2 = Piece2.GetComponent<Piece>();
                int hpLeft = pScript2.GetDamage(dmg, AttackType.NormalAttack);

                motionQueue.Enqueue(PieceAttackCor(button1script, button2script, 1f));
                if (hpLeft <= 0)
                    motionQueue.Enqueue(pScript2.DeathCor());
            }
        }
        StartCoroutine(ProcessQueue());
    }
    IEnumerator PieceAttackCor(Button Button1, Button Button2, float moveDuration)
    {
        float time = 0f;
        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = time / moveDuration;

            yield return null;
        }
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
        Piece pScript = piece.GetComponent<Piece>();
        if (pScript != null && pScript.teamID == 1)
        {
            UpdateEnemyPositionList(Button1.GetLocation(), Button2.GetLocation());
        }
    }

    private void UpdateEnemyPositionList(Vector2 pos1, Vector2 pos2)
    {
        int index = enemyPositions.IndexOf(pos1);

        if (index != -1)
        {
            // 순서는 유지한 채 좌표 값만 변경
            enemyPositions[index] = pos2;
            Debug.Log($"[리스트 업데이트] 인덱스 {index}: {pos1} -> {pos2}");
        }
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
        TurnManager.instance.TurnStateProcessing();
        while (motionQueue.Count > 0)
        {
            // 큐에서 다음 연출을 꺼냄
            IEnumerator nextAction = motionQueue.Dequeue();

            // 해당 연출이 끝날 때까지 대기
            yield return StartCoroutine(nextAction);
        }

        queuecoroutineworking = false;
        TurnManager.instance.RollbackStateProcessing();
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
        if(selectedButton.x != -1 && selectedButton.y != -1)
            GetButtonScript(selectedButton).SelectedFalse();
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
        List<Vector2> effectRange = null;
        if (pendingEffects.Count > 0)
        {
            CardEffect currentEffect = pendingEffects.Peek();

            // 2. 효과에 거리 정보(List<Vector2> 타입의 범위 데이터 등)가 있는지 확인
            // CardEffect 클래스에 범위 리스트(예: actionRange)가 있다고 가정합니다.
            if (currentEffect.effectRange != null)
            {
                effectRange = currentEffect.effectRange.GetAbleRange();
            }
        }
        ShowMovableButtons(GetButtonScript(selectedButton).GetPiece(), effectRange);
        ShowButtonInfo();
    }
    void OnUnSelectBoard()
    {
        HideMovableButtons();
        HideButtonInfo();
    }
    void ShowMovableButtons(GameObject p, List<Vector2> effectableButton = default)
    {
        if (p == null)
            return;

        Piece piece = p.GetComponent<Piece>();
        List<Vector2> list;
        if (effectableButton == null)
            list = piece.GetMoveableButton();
        else
            list = effectableButton;
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
