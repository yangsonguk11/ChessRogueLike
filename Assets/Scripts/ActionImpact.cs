using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 대상 하나의 피격 반응(Hit/Die 트리거, 사망음, 데미지·상태이상 텍스트 — 다중 타격이면 MultiHitReactionCor 하나).
public class HitReaction
{
    public Piece piece;
    public Vector2Int cell;
    public List<IEnumerator> coroutines = new List<IEnumerator>();
    public bool started;
}

// 시전자→대상 행동 연출 하나(Board.PlayCasterAndTargetReaction)의 연출 컨텍스트. 게임 상태는 이미 동기로 확정됐고, 이 객체는
// 효과 처리(enqueue) 시점에 만들어 둔 연출 데이터만 담는다. 재생 시점에 시전자 타격 시점(OnAnimationEvent)이 오면 Board가
// 이펙트(cue)를 시작하고, 대상별 피격 반응은 이펙트가 원하는 순간(번개가 떨어진 칸, 빔이 닿은 순간 등)에 TriggerHit으로 시작한다.
// 이펙트가 없으면 타격 시점에 TriggerAll — 모든 대상이 한꺼번에 피격하는 기존 동작.
public class ActionImpact
{
    public Piece caster;
    public Vector2Int? primaryCell;                          // 단일 대상 공격의 대상 칸(범위 공격은 범위 기준 칸)
    public List<Vector2Int> areaCells = new List<Vector2Int>(); // 범위 표시와 같은 칸(Board.ResolveAnimRangeCells)
    public List<HitReaction> hits = new List<HitReaction>();
    public List<IEnumerator> common = new List<IEnumerator>(); // 시전자 자신 반응(흡혈 회복·이동공격 실드)·대상 구분 없는 반응
    public AttackVfxCue cue;
    public int activeCues;                                   // 재생 중인 이펙트 수 — 0이 될 때까지 Board가 행동을 끝내지 않는다
    public bool finished;                                    // Board.RunImpactCor가 끝남(이펙트가 멈춰 시간 제한으로 끝난 경우 포함)

    readonly MonoBehaviour runner;
    readonly List<Coroutine> running = new List<Coroutine>();
    bool commonStarted;

    // runner: 반응 코루틴을 돌릴 오브젝트(Board) — 시전자가 아니라 Board에서 돌려야 도중에 기물이 파괴돼도 끊기지 않는다.
    public ActionImpact(MonoBehaviour runner)
    {
        this.runner = runner;
    }

    public HitReaction AddHit(Piece piece, Vector2Int cell, IEnumerable<IEnumerator> coroutines)
    {
        var hit = new HitReaction { piece = piece, cell = cell };
        hit.coroutines.AddRange(coroutines);
        hits.Add(hit);
        return hit;
    }

    public bool AllTriggered
    {
        get
        {
            if (!commonStarted) return false;
            foreach (HitReaction h in hits)
                if (!h.started) return false;
            return true;
        }
    }

    // 이펙트가 쓰는 칸: 범위 칸 + 피격 대상 칸(+주 대상 칸). 범위 표시가 비는 효과(보드 전체 대상 등)나 범위 밖 대상도 빠지지 않게 합친다.
    public List<Vector2Int> EffectCells()
    {
        var cells = new List<Vector2Int>(areaCells);
        foreach (HitReaction h in hits)
            if (!cells.Contains(h.cell)) cells.Add(h.cell);
        if (primaryCell.HasValue && !cells.Contains(primaryCell.Value)) cells.Add(primaryCell.Value);
        return cells;
    }

    // 아직 시작하지 않았으면 그 대상의 반응을 시작한다. 첫 피격 때 시전자 반응(common)도 함께 시작한다.
    public void TriggerHit(HitReaction hit)
    {
        if (hit == null || hit.started) return;
        hit.started = true;
        StartCommon();
        foreach (IEnumerator c in hit.coroutines)
            running.Add(runner.StartCoroutine(c));
    }

    public void TriggerHitsAt(Vector2Int cell)
    {
        foreach (HitReaction h in hits)
            if (h.cell == cell) TriggerHit(h);
    }

    // 남은 반응을 전부 시작한다 — 이펙트 없는 행동의 타격 시점, 또는 이펙트가 끝난 뒤의 안전장치.
    public void TriggerAll()
    {
        StartCommon();
        foreach (HitReaction h in hits)
            TriggerHit(h);
    }

    void StartCommon()
    {
        if (commonStarted) return;
        commonStarted = true;
        foreach (IEnumerator c in common)
            running.Add(runner.StartCoroutine(c));
    }

    // 시작된 반응이 전부 끝날 때까지 대기.
    public IEnumerator WaitStartedReactions()
    {
        for (int i = 0; i < running.Count; i++)
            yield return running[i];
    }
}
