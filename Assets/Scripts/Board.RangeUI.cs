using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    List<Vector2> selectedButtonMovable = new List<Vector2>();

    void OnSelectBoard()
    {
        if (pendingEffects.Count > 0)
        {
            CardEffect currentEffect = pendingEffects.Peek();
            if (currentEffect.type != EffectType.Move &&
                currentEffect.effectRange != null &&
                currentEffect.areaTargetMode != AreaTargetMode.Fixed)
            {
                // 마우스 기반 범위 효과: 모든 칸을 클릭 가능하게 하되 시각적 표시 없음
                FillAllMovableButtonsSilent();
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
        ShowButtonInfo(selectedButton);
    }

    void OnUnSelectBoard()
    {
        ClearHoverRange();
        HideMovableButtons();
        HideButtonInfo();
    }

    void FillAllMovableButtonsSilent()
    {
        selectedButtonMovable.Clear();
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
                selectedButtonMovable.Add(new Vector2(x, y));
    }

    void ShowMovableButtons(GameObject p, List<Vector2> effectableButton = default)
    {
        if (p == null) return;

        List<Vector2> list = effectableButton ?? p.GetComponent<Piece>().GetMoveableButton();
        AddMovableButtons(list);
    }

    void HideMovableButtons()
    {
        foreach (Vector2 v in selectedButtonMovable)
            GetButtonScript(v).RangeOff();
    }

    void AddMovableButtons(List<Vector2> list)
    {
        selectedButtonMovable.Clear();
        foreach (Vector2 v in list)
        {
            Vector2 m = selectedButton + v;
            if (m.x < 0 || m.x >= N || m.y < 0 || m.y >= M) continue;
            GetButtonScript(m).RangeOn();
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
