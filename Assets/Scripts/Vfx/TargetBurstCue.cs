using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 대상 위치에서 터지는 이펙트. hitOnSpawn이면 생성 순간(+hitDelay) 그 칸의 대상이 피격한다.
[CreateAssetMenu(fileName = "NewTargetBurstCue", menuName = "Vfx/Target Burst Cue")]
public class TargetBurstCue : AttackVfxCue
{
    public enum BurstTargets
    {
        Primary,  // 주 대상 칸 하나(범위 공격은 범위 기준 칸)
        AllHits,  // 피격 대상 칸마다(헛스윙이면 주 대상 칸)
        AllCells, // 범위 칸 전부(빈 칸 포함)
    }

    public BurstTargets targets = BurstTargets.AllHits;
    public bool hitOnSpawn = true;
    public float hitDelay = 0f;

    public override bool FiresHits => hitOnSpawn;

    public override float EstimateDuration(ActionImpact impact) => Mathf.Max(duration, hitDelay);

    public override IEnumerator Play(ActionImpact impact)
    {
        List<Vector2Int> cells = ResolveCells(impact);
        foreach (Vector2Int cell in cells)
            SpawnAt(HitPoint(impact, cell));

        float waited = 0f;
        if (hitOnSpawn)
        {
            if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);
            waited = hitDelay;
            foreach (Vector2Int cell in cells)
                impact.TriggerHitsAt(cell);
        }
        if (duration > waited) yield return new WaitForSeconds(duration - waited);
    }

    List<Vector2Int> ResolveCells(ActionImpact impact)
    {
        var cells = new List<Vector2Int>();
        switch (targets)
        {
            case BurstTargets.AllCells:
                return impact.EffectCells();
            case BurstTargets.AllHits:
                foreach (HitReaction h in impact.hits)
                    if (!cells.Contains(h.cell)) cells.Add(h.cell);
                break;
        }
        if (cells.Count == 0)
        {
            if (impact.primaryCell.HasValue) cells.Add(impact.primaryCell.Value);
            else if (impact.hits.Count > 0) cells.Add(impact.hits[0].cell);
        }
        return cells;
    }
}
