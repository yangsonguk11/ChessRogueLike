using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 범위 칸마다 무작위 순서로 번개를 한 번씩 떨어뜨린다. 번개가 떨어지는 순간(+hitDelay) 그 칸의 대상이 피격하고, 빈 칸은 이펙트만 난다.
// 순서 섞기는 연출 전용이라 UnityEngine.Random을 쓴다(게임 로직은 이미 확정돼 있어 결과와 무관).
[CreateAssetMenu(fileName = "NewLightningScatterCue", menuName = "Vfx/Lightning Scatter Cue")]
public class LightningScatterCue : AttackVfxCue
{
    [Tooltip("번개 사이 간격(초)")]
    public float interval = 0.12f;
    [Tooltip("번개 생성 후 피격까지(초)")]
    public float hitDelay = 0f;

    public override float EstimateDuration(ActionImpact impact)
    {
        return impact.EffectCells().Count * interval + Mathf.Max(duration, hitDelay);
    }

    public override IEnumerator Play(ActionImpact impact)
    {
        List<Vector2Int> cells = impact.EffectCells();
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            SpawnAt(VfxHelper.CellPosition(cells[i]));
            if (hitDelay > 0f) Board.instance.StartCoroutine(TriggerHitsAtAfter(impact, cells[i], hitDelay));
            else impact.TriggerHitsAt(cells[i]);
            if (i < cells.Count - 1 && interval > 0f) yield return new WaitForSeconds(interval);
        }

        // 마지막 번개의 피격·이펙트가 끝날 때까지.
        yield return new WaitForSeconds(Mathf.Max(duration, hitDelay));
    }
}
