using System.Collections;
using UnityEngine;

// 공격 이펙트 정의(Assets/SO/Vfx/). 카드가 CardEffect.vfx에 이 에셋 이름을 적으면, 시전자 타격 시점(OnAnimationEvent)에
// Board가 Play를 시작한다. Play는 이펙트를 내면서 원하는 순간에 impact.TriggerHit/TriggerHitsAt으로 대상 피격을 발사한다 —
// 이펙트가 끝날 때까지 발사하지 않은 대상은 Board가 마지막에 한꺼번에 피격시킨다(안전장치).
// 새 연출 방식은 하위 클래스만 추가하면 된다. 시간은 모두 스케일 시간이라 게임 배속·히트스탑을 그대로 따른다.
public abstract class AttackVfxCue : ScriptableObject
{
    [Tooltip("생성할 이펙트 프리팹. 파티클이면 재생 시간+입자 수명 뒤, 아니면 duration 뒤 파괴된다.")]
    public GameObject prefab;
    [Tooltip("이펙트 재생 시간(초). 이 시간이 지나야 행동 연출이 다음으로 넘어간다.")]
    public float duration = 0.5f;
    [Tooltip("이펙트가 나가는 시전자 부위(VfxSocket.id). 비우거나 기물에 없으면 기물 중심.")]
    public string socketId;
    public bool useTint;
    public Color tint = Color.white;
    [Tooltip("생성 위치 보정(월드). 예: 번개를 칸 위쪽에서 생성.")]
    public Vector3 spawnOffset;
    public Vector3 spawnEuler;

    // false면 이펙트가 피격을 발사하지 않는다 — Board가 타격 시점에 모든 대상을 바로 피격시키고 이펙트는 장식으로만 재생한다.
    public virtual bool FiresHits => true;

    public abstract IEnumerator Play(ActionImpact impact);

    // 이펙트가 걸릴 최대 시간 — Board가 이 시간(+여유)이 지나면 이펙트가 멈췄어도 행동을 끝낸다.
    public virtual float EstimateDuration(ActionImpact impact) => duration;

    // prefab을 생성하고 수명이 끝나면 파괴한다(정리 코루틴은 Board에서 돈다).
    protected GameObject SpawnAt(Vector3 position)
    {
        GameObject instance = VfxHelper.Spawn(prefab, position + spawnOffset, Quaternion.Euler(spawnEuler), null, useTint ? tint : (Color?)null);
        if (instance != null && Board.instance != null)
            Board.instance.StartCoroutine(VfxHelper.DestroyAfterLifetime(instance, duration));
        return instance;
    }

    // 칸의 피격점: 그 칸에 피격 대상이 있으면 기물 중심, 없으면 칸 위치.
    protected static Vector3 HitPoint(ActionImpact impact, Vector2Int cell)
    {
        foreach (HitReaction h in impact.hits)
            if (h.cell == cell && h.piece != null) return VfxHelper.PieceCenter(h.piece);
        return VfxHelper.CellPosition(cell);
    }

    protected static IEnumerator TriggerHitsAtAfter(ActionImpact impact, Vector2Int cell, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        impact.TriggerHitsAt(cell);
    }
}
