using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // 사거리 — 표시(1순위 카드 사거리 / 2순위 AI 선택 범위)와 판정(드롭 검증, 클릭 대상, AI 대상 계산)에 함께 쓰인다.
    List<Vector2Int> selectedButtonMovable = new List<Vector2Int>();
    int selectedMovableTeam = 0;

    // 카드를 들고 있는 동안(usecard 진입~사용/취소) 카드 주인 칸에 초록 표시(1순위). 종류 상관없이 카드를
    // 든 시점부터 무조건 켜지고, ResetBoardAfterCardUse에서 끈다. 기물 대신 버튼을 기억하고, 기물이 이동하면
    // RelocateCasterIndicator가 같이 옮긴다.
    Button casterIndicatorButton;

    // 3순위 "기물 선택"(카드 사용 전): 정보창(ButtonInfo)에 뜬 기물의 범위와 그 칸 초록.
    bool inspectedActive;
    Vector2Int inspectedCell;
    int inspectedRangeTeam;
    readonly List<Vector2Int> inspectedRangeCells = new List<Vector2Int>();

    // ── 범위 표시 ──────────────────────────────────────────────
    // 범위는 각 기능이 Board의 자기 목록에 담아두고, 칸에 그리는 건 이 함수 하나가 한다 — 목록을 바꾼 기능은
    // RefreshRangeDisplay만 부르면 된다. 높은 순위에 켤 것이 있으면 낮은 순위는 모두 끄고, 같은 순위끼리는 함께 보인다.
    //  1. 조준(플레이어가 카드를 겨누는 중): 카드 사거리, 범위 효과 미리보기, 카드 사용 가능 기물, 기물 선택 요청 / 초록 = 카드 주인
    //  2. 행동(motionQueue 재생 중, 적·자동 아군의 선택): AI 선택 범위, 행동 연출 범위 / 초록 = 행동 중인 시전자.
    //     큐가 도는 동안은 내용이 없어도 켜진 것으로 본다 — 연출 사이(사망 등)에는 아무 범위도 보이지 않는다.
    //  3. 기물 선택(카드 사용 전): 정보창에 뜬 기물의 범위 / 초록 = 그 기물
    //  4. 평상시: 적 예고 범위
    // 적·자동 아군 턴에는 AI가 selectedButton을 거쳐 selectedButtonMovable을 채우므로, 플레이어가 행동할 수 없을 때의
    // 사거리는 1순위(조준)가 아니라 2순위(AI 선택)로 본다.
    void RefreshRangeDisplay()
    {
        if (Buttons == null) return;

        var ally = new HashSet<Vector2Int>();
        var enemy = new HashSet<Vector2Int>();
        var caster = new HashSet<Vector2Int>();
        void AddCell(Vector2Int c, int team) => (team == 0 ? ally : enemy).Add(c);
        void Add(IEnumerable<Vector2Int> cells, int team)
        {
            foreach (Vector2Int c in cells) AddCell(c, team);
        }

        bool actionable = TurnManager.instance == null || TurnManager.instance.IsPlayerActionable;
        bool hasMovable = !movableButtonsSilent && selectedButtonMovable.Count > 0;

        if (actionable && (hasMovable || hoverRangeButtons.Count > 0 || useEligibilityHighlights.Count > 0
                           || pieceSelectHighlights.Count > 0 || casterIndicatorButton != null))
        {
            if (hasMovable) Add(selectedButtonMovable, selectedMovableTeam);
            Add(hoverRangeButtons, 0);
            foreach (var (pos, team) in useEligibilityHighlights) AddCell(pos, team);
            foreach (var (pos, team) in pieceSelectHighlights) AddCell(pos, team);
            if (casterIndicatorButton != null) caster.Add(casterIndicatorButton.GetLocation());
        }
        else if (queuecoroutineworking || currentActionDisplay != null || (!actionable && hasMovable))
        {
            if (!actionable && hasMovable) Add(selectedButtonMovable, selectedMovableTeam);
            if (currentActionDisplay != null)
            {
                Add(currentActionDisplay.cells, currentActionDisplay.team);
                caster.Add(currentActionDisplay.casterCell);
            }
        }
        else if (inspectedActive)
        {
            Add(inspectedRangeCells, inspectedRangeTeam);
            caster.Add(inspectedCell);
        }
        else
        {
            Add(enemyAlwaysOnRange, 1);
        }

        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                var pos = new Vector2Int(x, y);
                GetButtonScript(pos).SetRangeVisual(ally.Contains(pos), enemy.Contains(pos), caster.Contains(pos));
            }
    }

    // origin + offsets 중 보드 안의 칸만.
    List<Vector2Int> CellsInBoard(Vector2Int origin, IEnumerable<Vector2Int> offsets)
    {
        var cells = new List<Vector2Int>();
        foreach (Vector2Int offset in offsets)
        {
            Vector2Int c = origin + offset;
            if (c.x >= 0 && c.x < N && c.y >= 0 && c.y < M) cells.Add(c);
        }
        return cells;
    }

    public void SetCasterIndicator(Piece piece, bool active)
    {
        casterIndicatorButton = active ? GetButtonForPiece(piece) : null;
        RefreshRangeDisplay();
    }

    // 시전자 표시가 켜진 버튼(button1)에서 다른 칸(button2)으로 피스가 실제로 이동했을 때 표시도 함께 옮긴다.
    void RelocateCasterIndicator(Button button1, Button button2)
    {
        if (casterIndicatorButton != button1) return;
        casterIndicatorButton = button2;
        RefreshRangeDisplay();
    }

    void OnSelectBoard()
    {
        ClearUseEligibilityPreview();
        casterPiece = GetButtonScript(selectedButton).GetPieceScript();
        CardCanvas.instance?.RefreshAllCardViews();
        // 자기 자신 대상 효과: 시전자 클릭 즉시 실행 (두 번째 클릭 불필요)
        if (boardmode == BoardMode.targeting && pendingEffects.Count > 0 &&
            pendingEffects.Peek().targetlogic == TargetLogic.self)
        {
            ExecuteEffect(pendingEffects.Dequeue(), selectedButton);
            ScheduleNextCardEffect();
            return;
        }

        // 플레이어가 카드 없이 기물을 고른 경우는 3순위 "기물 선택" — hover와 같은 경로(ShowButtonInfo)로 그 기물의
        // 범위만 보여주고, 판정용 사거리(selectedButtonMovable)는 카드를 쓸 때(또는 AI가 고를 때)만 채운다.
        if (pendingEffects.Count == 0 && (TurnManager.instance == null || TurnManager.instance.IsPlayerActionable))
        {
            ShowButtonInfo(selectedButton);
            return;
        }

        ShowCasterEffectRange(selectedButton, pendingEffects.Count > 0 ? pendingEffects.Peek() : null);
    }

    // 캐스터(casterPos)가 이번 카드 효과로 이동/공격/AoE 중심 지정 등에 쓸 수 있는 칸을 하이라이트한다.
    // selectedButton(캐스터 확정 + self 즉시실행까지 트리거하는 프로퍼티)에 의존하지 않는 순수 미리보기용
    // 함수라서, 카드를 픽업한 시점처럼 아직 캐스터를 "확정"하기 전에도 안전하게 호출할 수 있다.
    void ShowCasterEffectRange(Vector2Int casterPos, CardEffect currentEffect)
    {
        // noRangeLimit 효과(예: MagicAttackCard, Zone*Card)는 캐스터 위치/이동범위와 무관하게 보드
        // 전체가 유효 대상이어야 하므로, 캐스터 기준 사거리 계산을 전부 건너뛰고 무제한 폴백만 채운다.
        if (currentEffect != null && currentEffect.noRangeLimit)
        {
            FillAllMovableButtonsSilent(casterPos);
            ShowButtonInfo(casterPos);
            return;
        }

        if (currentEffect != null &&
            currentEffect.type != EffectType.Move &&
            currentEffect.effectRange != null &&
            currentEffect.areaTargetMode != AreaTargetMode.Fixed)
        {
            if (currentEffect.targetingUsesMovement)
            {
                // 캐릭터 이동 범위 내에서만 AoE 중심 선택 가능 (시각적 표시 O)
                ShowMovableButtons(casterPos, GetButtonScript(casterPos).GetPiece(), null);
            }
            else if (currentEffect.targetingRange != null)
            {
                // 지정된 사거리 내에서만 AoE 중심 선택 가능 (시각적 표시 O)
                AddMovableButtons(casterPos, currentEffect.targetingRange.GetAbleRange());
            }
            else
            {
                // 제한 없음: 전체 보드, 시각적 표시 없음
                FillAllMovableButtonsSilent(casterPos);
            }
            ShowButtonInfo(casterPos);
            return;
        }

        List<Vector2Int> effectRange = null;
        if (currentEffect != null && currentEffect.type != EffectType.Move && currentEffect.effectRange != null)
            effectRange = currentEffect.effectRange.GetAbleRange();
        ShowMovableButtons(casterPos, GetButtonScript(casterPos).GetPiece(), effectRange);

        if (currentEffect != null && currentEffect.type == EffectType.Move && currentEffect.noMoveAttack)
            FilterEnemyOccupiedFromMovable(casterPos);
        if (currentEffect != null && currentEffect.type == EffectType.Summon)
            FilterOccupiedFromMovable(casterPos);

        ShowButtonInfo(casterPos);
    }

    void FilterEnemyOccupiedFromMovable(Vector2Int casterPos)
    {
        Piece caster = GetButtonScript(casterPos).GetPieceScript();
        if (caster == null) return;
        selectedButtonMovable.RemoveAll(pos =>
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            return p != null && p.teamID != caster.teamID;
        });
        RefreshRangeDisplay();
    }

    void FilterOccupiedFromMovable(Vector2Int casterPos)
    {
        selectedButtonMovable.RemoveAll(pos => GetButtonScript(pos).GetPieceScript() != null);
        RefreshRangeDisplay();
    }

    void OnUnSelectBoard()
    {
        casterPiece = null;
        CardCanvas.instance?.RefreshAllCardViews();
        ClearHoverRange();
        HideMovableButtons();
        HideButtonInfo();
    }

    // 보드 전체가 대상인 효과(noRangeLimit 등)의 판정용 사거리 — 목록은 채우되 표시하지 않는다.
    bool movableButtonsSilent = false;

    void FillAllMovableButtonsSilent(Vector2Int casterPos)
    {
        selectedButtonMovable.Clear();
        selectedMovableTeam = GetButtonScript(casterPos).GetPieceScript()?.teamID ?? 0;
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
                selectedButtonMovable.Add(new Vector2Int(x, y));
        movableButtonsSilent = true;
        RefreshRangeDisplay();
    }

    void ShowMovableButtons(Vector2Int casterPos, GameObject p, List<Vector2Int> effectableButton = default)
    {
        if (p == null) return;

        List<Vector2Int> list = effectableButton ?? p.GetComponent<Piece>().GetMoveableButton();
        AddMovableButtons(casterPos, list);
    }

    void HideMovableButtons()
    {
        selectedButtonMovable.Clear();
        movableButtonsSilent = false;
        RefreshRangeDisplay();
    }

    void AddMovableButtons(Vector2Int casterPos, List<Vector2Int> list)
    {
        selectedMovableTeam = GetButtonScript(casterPos).GetPieceScript()?.teamID ?? 0;
        selectedButtonMovable.Clear();
        selectedButtonMovable.AddRange(CellsInBoard(casterPos, list));
        movableButtonsSilent = false;
        RefreshRangeDisplay();
    }

    // 정보창 표시 + 3순위 "기물 선택" 범위. hover와 카드 없는 클릭 선택이 함께 쓰고, 카드/AI 경로(ShowCasterEffectRange)도
    // 정보창을 띄우려고 부르지만 그때는 1·2순위가 켜져 있어 이 범위는 가려진다.
    // 범위: AutoPiece(적·자동 아군)는 다음 행동 범위(잠긴 칸 포함), 그 외 기물은 이동 범위. 모닥불·상점(teamID 2)은 표시 없음.
    void ShowButtonInfo(Vector2Int button)
    {
        ButtonInfo buttonInfo = BoardUICanvas.GetComponent<ButtonInfo>();
        buttonInfo.SetActive(true);
        buttonInfo.UpdateButtonInfo(GetButtonScript(button));

        inspectedRangeCells.Clear();
        Piece piece = GetPieceAt(button);
        inspectedActive = piece != null && (piece.teamID == 0 || piece.teamID == 1);
        if (inspectedActive)
        {
            inspectedCell = button;
            inspectedRangeTeam = piece.teamID;
            inspectedRangeCells.AddRange(piece is AutoPiece auto
                ? GetActionRangeCells(auto, button)
                : CellsInBoard(button, piece.GetMoveableButton()));
        }
        RefreshRangeDisplay();
    }

    void HideButtonInfo()
    {
        BoardUICanvas.GetComponent<ButtonInfo>().SetActive(false);
        inspectedActive = false;
        inspectedRangeCells.Clear();
        RefreshRangeDisplay();
    }

    // ShopCanvas.Show()에서 호출: 상점 캔버스가 대신 나타나므로 ButtonInfo(호버 정보창)는 숨긴다.
    public void HideButtonInfoForShop() => HideButtonInfo();

    // 기준 위치에서 가장 가까운 targetTeam 소속 기물 위치 반환
    Vector2Int GetNearestPlayerPos(Vector2Int enemyPos, int targetTeam)
    {
        Vector2Int nearest = enemyPos;
        float minDistance = float.MaxValue;

        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                Piece p = GetButtonScript(new Vector2Int(x, y)).GetPiece()?.GetComponent<Piece>();
                if (p != null && p.teamID == targetTeam)
                {
                    float dist = Vector2.Distance(enemyPos, new Vector2Int(x, y));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = new Vector2Int(x, y);
                    }
                }
            }
        }
        return nearest;
    }

    // 이동공격 도착 위치 결정: location1(공격자) → location2(피격자) 방향 기준.
    // - 상하좌우로 이미 붙어있으면 이동하지 않고 그 자리에서 공격 (location1 반환).
    // - 그 외에는 피격자를 둘러싼 8칸 중 공격 방향에 맞는 3칸(직선/대각선 칸 + 옆 2칸)을 우선순위대로 시도해서
    //   비어있는 첫 칸으로 이동. 대각선으로 딱 붙어있는 경우 첫 후보(직선 칸)가 공격자의 현재 위치와 같아져
    //   자동으로 건너뛰어지므로 상하좌우 옆 칸 중 하나로만 이동하게 됨.
    // - 후보가 모두 막혀있거나(다른 기물 점유) 보드 밖이면 (-1,-1)을 반환해 이동공격 자체가 불가능함을 알림
    //   (호출부는 이를 일반 이동 실패와 동일하게 처리해야 함).
    Vector2Int GetAdjacentLocation(Vector2Int location1, Vector2Int location2)
    {
        Vector2Int delta = location2 - location1;
        int adx = Math.Abs(delta.x);
        int ady = Math.Abs(delta.y);

        if ((adx == 1 && ady == 0) || (adx == 0 && ady == 1))
            return location1;

        Vector2Int dir;
        if (adx == ady)
        {
            dir = new Vector2Int(delta.x < 0 ? -1 : 1, delta.y < 0 ? -1 : 1);
        }
        else if (adx > ady)
        {
            dir = new Vector2Int(delta.x < 0 ? -1 : 1, 0);
        }
        else
        {
            dir = new Vector2Int(0, delta.y < 0 ? -1 : 1);
        }

        Vector2Int back = -dir;
        Vector2Int lineSquare = location2 + back;
        Vector2Int side1, side2;
        if (back.x != 0 && back.y != 0)
        {
            // 대각선 접근: 옆 후보는 피격자의 상하 칸 / 좌우 칸
            side1 = location2 + new Vector2Int(back.x, 0);
            side2 = location2 + new Vector2Int(0, back.y);
        }
        else
        {
            // 직선 접근: 옆 후보는 직선 칸과 맞붙은 대각선 코너 칸
            Vector2Int perp = back.x != 0 ? new Vector2Int(0, 1) : new Vector2Int(1, 0);
            side1 = lineSquare + perp;
            side2 = lineSquare - perp;
        }

        foreach (Vector2Int candidate in new[] { lineSquare, side1, side2 })
        {
            if (candidate == location1) continue; // 공격자가 이미 그 칸에 있으면 이동 후보로 치지 않음
            if (candidate.x < 0 || candidate.x >= N || candidate.y < 0 || candidate.y >= M) continue;
            if (GetPieceAt(candidate) == null) return candidate;
        }

        return new Vector2Int(-1, -1); // 후보 칸이 모두 막혀있음: 이동공격 불가
    }

    // teamID==1(적)이든 teamID==0 AutoPiece(자동행동 아군)든, 이동한 기물이 어느 위치 추적 리스트에
    // 들어있었는지 몰라도 실제로 값을 갖고 있던 리스트만 갱신한다.
    void UpdateAutoPiecePositionList(Vector2Int pos1, Vector2Int pos2)
    {
        int enemyIndex = enemyPositions.IndexOf(pos1);
        if (enemyIndex != -1)
        {
            enemyPositions[enemyIndex] = pos2;
            return;
        }
        int allyIndex = autoAllyPositions.IndexOf(pos1);
        if (allyIndex != -1)
            autoAllyPositions[allyIndex] = pos2;
    }
}
