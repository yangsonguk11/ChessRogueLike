using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2> selectedButtonMovable = new List<Vector2>();
    int selectedMovableTeam = 0;

    void OnSelectBoard()
    {
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

        if (pendingEffects.Count > 0)
        {
            CardEffect currentEffect = pendingEffects.Peek();
            if (currentEffect.type != EffectType.Move &&
                currentEffect.effectRange != null &&
                currentEffect.areaTargetMode != AreaTargetMode.Fixed)
            {
                if (currentEffect.targetingUsesMovement)
                {
                    // 캐릭터 이동 범위 내에서만 AoE 중심 선택 가능 (시각적 표시 O)
                    ShowMovableButtons(GetButtonScript(selectedButton).GetPiece(), null);
                }
                else if (currentEffect.targetingRange != null)
                {
                    // 지정된 사거리 내에서만 AoE 중심 선택 가능 (시각적 표시 O)
                    AddMovableButtons(currentEffect.targetingRange.GetAbleRange());
                }
                else
                {
                    // 제한 없음: 전체 보드, 시각적 표시 없음
                    FillAllMovableButtonsSilent();
                }
                ShowButtonInfo(selectedButton);
                return;
            }
        }

        List<Vector2> effectRange = null;
        if (pendingEffects.Count > 0)
        {
            CardEffect currentEffect = pendingEffects.Peek();
            if (currentEffect.type != EffectType.Move && currentEffect.effectRange != null)
                effectRange = currentEffect.effectRange.GetAbleRange();
        }
        ShowMovableButtons(GetButtonScript(selectedButton).GetPiece(), effectRange);

        if (pendingEffects.Count > 0 && pendingEffects.Peek().type == EffectType.Move && pendingEffects.Peek().noMoveAttack)
            FilterEnemyOccupiedFromMovable();

        ShowButtonInfo(selectedButton);
    }

    void FilterEnemyOccupiedFromMovable()
    {
        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        if (caster == null) return;
        for (int i = selectedButtonMovable.Count - 1; i >= 0; i--)
        {
            Piece p = GetButtonScript(selectedButtonMovable[i]).GetPieceScript();
            if (p != null && p.teamID != caster.teamID)
            {
                GetButtonScript(selectedButtonMovable[i]).RangeOff(selectedMovableTeam);
                selectedButtonMovable.RemoveAt(i);
            }
        }
    }

    void OnUnSelectBoard()
    {
        casterPiece = null;
        CardCanvas.instance?.RefreshAllCardViews();
        ClearHoverRange();
        ClearHoverPieceRange();
        HideMovableButtons();
        HideButtonInfo();
    }

    // FillAllMovableButtonsSilent로 채운 칸은 RangeOn을 호출하지 않았으므로,
    // HideMovableButtons에서도 RangeOff를 호출하면 안 됨 (다른 곳에서 켜둔 같은 칸의 표시를 잘못 꺼버리게 됨).
    bool movableButtonsSilent = false;

    void FillAllMovableButtonsSilent()
    {
        HideMovableButtons();
        selectedMovableTeam = GetButtonScript(selectedButton).GetPieceScript()?.teamID ?? 0;
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
                selectedButtonMovable.Add(new Vector2(x, y));
        movableButtonsSilent = true;
    }

    void ShowMovableButtons(GameObject p, List<Vector2> effectableButton = default)
    {
        if (p == null) return;

        List<Vector2> list = effectableButton ?? p.GetComponent<Piece>().GetMoveableButton();
        AddMovableButtons(list);
    }

    void HideMovableButtons()
    {
        if (!movableButtonsSilent)
            foreach (Vector2 v in selectedButtonMovable)
                GetButtonScript(v).RangeOff(selectedMovableTeam);
        selectedButtonMovable.Clear();
        movableButtonsSilent = false;
    }

    void AddMovableButtons(List<Vector2> list)
    {
        HideMovableButtons();
        selectedMovableTeam = GetButtonScript(selectedButton).GetPieceScript()?.teamID ?? 0;
        foreach (Vector2 v in list)
        {
            Vector2 m = selectedButton + v;
            if (m.x < 0 || m.x >= N || m.y < 0 || m.y >= M) continue;
            GetButtonScript(m).RangeOn(selectedMovableTeam);
            selectedButtonMovable.Add(m);
        }
    }

    void ShowButtonInfo(Vector2 button)
    {
        BoardUICanvas.SetActive(true);
        BoardUICanvas.GetComponent<BoardUICanvas>().UpdateButtonInfo(GetButtonScript(button));
    }

    void HideButtonInfo()
    {
        BoardUICanvas.SetActive(false);
    }

    // 적 기준 가장 가까운 플레이어 위치 반환
    Vector2 GetNearestPlayerPos(Vector2 enemyPos)
    {
        Vector2 nearest = enemyPos;
        float minDistance = float.MaxValue;

        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                Piece p = GetButtonScript(new Vector2(x, y)).GetPiece()?.GetComponent<Piece>();
                if (p != null && p.teamID == 0)
                {
                    float dist = Vector2.Distance(enemyPos, new Vector2(x, y));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = new Vector2(x, y);
                    }
                }
            }
        }
        return nearest;
    }

    // location1 → location2 방향으로 한 칸 이동한 인접 위치 반환
    Vector2 GetAdjacentLocation(Vector2 location1, Vector2 location2)
    {
        Vector2 delta = location2 - location1;
        if (Math.Abs(delta.x) == Math.Abs(delta.y))
        {
            delta.x = delta.x < 0 ? -1 : 1;
            delta.y = delta.y < 0 ? -1 : 1;
        }
        else if (Math.Abs(delta.x) > Math.Abs(delta.y))
        {
            delta.x = delta.x < 0 ? -1 : 1;
            delta.y = 0;
        }
        else
        {
            delta.x = 0;
            delta.y = delta.y < 0 ? -1 : 1;
        }
        return location2 - delta;
    }

    void UpdateEnemyPositionList(Vector2 pos1, Vector2 pos2)
    {
        int index = enemyPositions.IndexOf(pos1);
        if (index != -1)
        {
            enemyPositions[index] = pos2;
            Debug.Log($"[리스트 업데이트] 인덱스 {index}: {pos1} -> {pos2}");
        }
    }
}
