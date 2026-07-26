using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // filters를 전부(AND) 만족하는 기물을 count개 클릭해 모을 때까지만 담당하는 범용 선택 상태.
    // BoardMode.targeting을 그대로 빌려 쓰므로 카드 사용/드래그 등 다른 입력과 충돌하지 않는다.
    // 결과가 모이면 콜백에 그대로 넘기고, 그걸로 무엇을 할지는 호출부(Dialogue, CardEffect 등)가 알아서 처리한다.
    int pieceSelectCount;
    PieceSelectFilter[] pieceSelectFilters;
    readonly List<Vector2> pieceSelectedPositions = new List<Vector2>();
    Action<List<Piece>> pieceSelectCallback;

    public bool PieceSelectionActive => pieceSelectCallback != null;

    static bool MatchesFilters(Vector2 pos, Piece piece, PieceSelectFilter[] filters)
    {
        if (filters == null) return true;
        foreach (var f in filters)
            if (!f(pos, piece)) return false;
        return true;
    }

    // filters를 만족하는 기물이 count보다 적으면 있는 만큼으로 줄어든다.
    // 필터는 PieceSelectFilters의 팩토리들을 원하는 만큼 조합해서 넘기면 된다 (예: Team(0), WithinRange(pos, 2)).
    public void RequestPieceSelection(int count, Action<List<Piece>> onConfirm, params PieceSelectFilter[] filters)
    {
        int matchCount = 0;
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Piece p = GetPieceAt(pos);
                if (p != null && MatchesFilters(pos, p, filters)) matchCount++;
            }

        count = Mathf.Clamp(count, 0, matchCount);
        if (count == 0)
        {
            onConfirm?.Invoke(new List<Piece>());
            return;
        }

        pieceSelectCount = count;
        pieceSelectFilters = filters;
        pieceSelectedPositions.Clear();
        pieceSelectCallback = onConfirm;
        boardmode = BoardMode.targeting;

        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Piece p = GetPieceAt(pos);
                if (p != null && MatchesFilters(pos, p, filters))
                    GetButtonScript(pos).RangeOn(p.teamID);
            }
    }

    // 카드 사용이 취소/종료될 때 진행 중이던 기물 선택을 무효화한다. RequestPieceSelection에서
    // 필터에 매칭되는 칸들에 걸어둔 RangeOn 하이라이트를 FinishPieceSelection과 동일하게 직접
    // RangeOff로 꺼주고, 이미 클릭해둔 칸의 "선택됨" 표시(SelectedTrue)도 함께 정리한다.
    public void CancelPieceSelection()
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Piece p = GetPieceAt(pos);
                if (p == null || !MatchesFilters(pos, p, pieceSelectFilters)) continue;

                GetButtonScript(pos).RangeOff(p.teamID);
                if (pieceSelectedPositions.Contains(pos))
                    GetButtonScript(pos).SelectedFalse();
            }

        pieceSelectedPositions.Clear();
        pieceSelectFilters = null;
        pieceSelectCallback = null;
    }

    public void HandlePieceSelectionClick(Vector2 pos)
    {
        Piece clicked = GetPieceAt(pos);
        if (clicked == null || !MatchesFilters(pos, clicked, pieceSelectFilters)) return;

        if (pieceSelectedPositions.Contains(pos))
        {
            pieceSelectedPositions.Remove(pos);
            GetButtonScript(pos).SelectedFalse();
            return;
        }

        if (pieceSelectedPositions.Count >= pieceSelectCount) return;

        pieceSelectedPositions.Add(pos);
        GetButtonScript(pos).SelectedTrue();

        if (pieceSelectedPositions.Count == pieceSelectCount)
            FinishPieceSelection();
    }

    void FinishPieceSelection()
    {
        var result = new List<Piece>();
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Piece p = GetPieceAt(pos);
                if (p == null || !MatchesFilters(pos, p, pieceSelectFilters)) continue;

                GetButtonScript(pos).RangeOff(p.teamID);
                if (pieceSelectedPositions.Contains(pos))
                {
                    GetButtonScript(pos).SelectedFalse();
                    result.Add(p);
                }
            }

        pieceSelectedPositions.Clear();
        pieceSelectFilters = null;
        boardmode = BoardMode.Inspect;

        var callback = pieceSelectCallback;
        pieceSelectCallback = null;
        callback?.Invoke(result);
    }

    // targets에게 영구 스탯 버프를 적용한다. colDamage/shieldBonus와 baseColDamage/baseShieldBonus를 함께 올려
    // 이번 전투뿐 아니라 GetPieceData 저장을 거쳐 다음 전투로도 이어지게 한다 (EffectType.BaseColDamageUp과 동일한 방식).
    public void ApplyPermanentStatBuff(List<Piece> targets, int colDamageDelta, int shieldBonusDelta)
    {
        if (targets == null) return;

        foreach (var p in targets)
        {
            if (p == null) continue;
            if (colDamageDelta != 0) { p.colDamage += colDamageDelta; p.baseColDamage += colDamageDelta; }
            if (shieldBonusDelta != 0) { p.shieldBonus += shieldBonusDelta; p.baseShieldBonus += shieldBonusDelta; }
        }

        CardCanvas.instance?.RefreshAllCardViews();
    }
}
