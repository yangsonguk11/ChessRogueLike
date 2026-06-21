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
        ButtonInfo buttonInfo = BoardUICanvas.GetComponent<ButtonInfo>();
        buttonInfo.SetActive(true);
        buttonInfo.UpdateButtonInfo(GetButtonScript(button));
    }

    void HideButtonInfo()
    {
        BoardUICanvas.GetComponent<ButtonInfo>().SetActive(false);
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

    // 이동공격 도착 위치 결정: location1(공격자) → location2(피격자) 방향 기준.
    // - 상하좌우로 이미 붙어있으면 이동하지 않고 그 자리에서 공격 (location1 반환).
    // - 그 외에는 피격자를 둘러싼 8칸 중 공격 방향에 맞는 3칸(직선/대각선 칸 + 옆 2칸)을 우선순위대로 시도해서
    //   비어있는 첫 칸으로 이동. 대각선으로 딱 붙어있는 경우 첫 후보(직선 칸)가 공격자의 현재 위치와 같아져
    //   자동으로 건너뛰어지므로 상하좌우 옆 칸 중 하나로만 이동하게 됨.
    // - 후보가 모두 막혀있거나(다른 기물 점유) 보드 밖이면 (-1,-1)을 반환해 이동공격 자체가 불가능함을 알림
    //   (호출부는 이를 일반 이동 실패와 동일하게 처리해야 함).
    Vector2 GetAdjacentLocation(Vector2 location1, Vector2 location2)
    {
        Vector2 delta = location2 - location1;
        float adx = Math.Abs(delta.x);
        float ady = Math.Abs(delta.y);

        if ((adx == 1 && ady == 0) || (adx == 0 && ady == 1))
            return location1;

        Vector2 dir;
        if (adx == ady)
        {
            dir.x = delta.x < 0 ? -1 : 1;
            dir.y = delta.y < 0 ? -1 : 1;
        }
        else if (adx > ady)
        {
            dir.x = delta.x < 0 ? -1 : 1;
            dir.y = 0;
        }
        else
        {
            dir.x = 0;
            dir.y = delta.y < 0 ? -1 : 1;
        }

        Vector2 back = -dir;
        Vector2 lineSquare = location2 + back;
        Vector2 side1, side2;
        if (back.x != 0 && back.y != 0)
        {
            // 대각선 접근: 옆 후보는 피격자의 상하 칸 / 좌우 칸
            side1 = location2 + new Vector2(back.x, 0);
            side2 = location2 + new Vector2(0, back.y);
        }
        else
        {
            // 직선 접근: 옆 후보는 직선 칸과 맞붙은 대각선 코너 칸
            Vector2 perp = back.x != 0 ? new Vector2(0, 1) : new Vector2(1, 0);
            side1 = lineSquare + perp;
            side2 = lineSquare - perp;
        }

        foreach (Vector2 candidate in new[] { lineSquare, side1, side2 })
        {
            if (candidate == location1) continue; // 공격자가 이미 그 칸에 있으면 이동 후보로 치지 않음
            if (candidate.x < 0 || candidate.x >= N || candidate.y < 0 || candidate.y >= M) continue;
            if (GetPieceAt(candidate) == null) return candidate;
        }

        return new Vector2(-1, -1); // 후보 칸이 모두 막혀있음: 이동공격 불가
    }

    void UpdateEnemyPositionList(Vector2 pos1, Vector2 pos2)
    {
        int index = enemyPositions.IndexOf(pos1);
        if (index != -1)
            enemyPositions[index] = pos2;
    }
}
