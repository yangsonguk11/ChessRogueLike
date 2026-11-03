using System.Collections;
using UnityEngine;

// 시전자 발밑에서 원이 퍼져 나간다. hitOnPass면 원이 대상 칸을 지나는 순간 그 대상이 피격한다("퍼지며 맞는" 연출).
// prefab이 있으면 지름 1 기준 프리팹을 반지름에 맞춰 키우고, 없으면 선으로 원을 그린다.
[CreateAssetMenu(fileName = "NewRingCue", menuName = "Vfx/Ring Cue")]
public class RingCue : AttackVfxCue
{
    [Tooltip("최종 반지름(월드). 0이면 이펙트 칸 중 가장 먼 칸까지")]
    public float radius = 0f;
    public bool hitOnPass = true;
    [Tooltip("prefab이 없을 때 그리는 원")]
    public float lineWidth = 0.08f;
    public Color color = new Color(1f, 0.85f, 0.3f);
    public Material material;
    public int segments = 48;
    [Tooltip("원 높이(바닥에서 띄우는 정도)")]
    public float height = 0.05f;

    public override bool FiresHits => hitOnPass;

    public override IEnumerator Play(ActionImpact impact)
    {
        if (impact.caster == null) yield break;
        Vector3 center = impact.caster.transform.position;
        float maxRadius = radius > 0f ? radius : FarthestCellDistance(impact, center);
        if (maxRadius <= 0f) maxRadius = 1f;

        GameObject ring = SpawnAt(center);
        LineRenderer line = ring == null ? VfxHelper.CreateLine("RingLine", lineWidth, color, material, Mathf.Max(8, segments), true) : null;

        float t = 0f;
        while (t < duration)
        {
            float r = maxRadius * Mathf.Clamp01(duration > 0f ? t / duration : 1f);
            if (ring != null) ring.transform.localScale = Vector3.one * (r * 2f);
            if (line != null) DrawCircle(line, center, r);
            if (hitOnPass)
            {
                foreach (HitReaction h in impact.hits)
                    if (!h.started && HorizontalDistance(center, VfxHelper.CellPosition(h.cell)) <= r)
                        impact.TriggerHit(h);
            }
            yield return null;
            t += Time.deltaTime;
        }

        if (line != null) Destroy(line.gameObject);
    }

    void DrawCircle(LineRenderer line, Vector3 center, float r)
    {
        int count = line.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = 2f * Mathf.PI * i / count;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * r, height, Mathf.Sin(angle) * r));
        }
    }

    static float FarthestCellDistance(ActionImpact impact, Vector3 center)
    {
        float max = 0f;
        foreach (Vector2Int cell in impact.EffectCells())
            max = Mathf.Max(max, HorizontalDistance(center, VfxHelper.CellPosition(cell)));
        return max;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b;
        d.y = 0f;
        return d.magnitude;
    }
}
