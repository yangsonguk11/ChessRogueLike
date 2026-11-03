using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시전자 부위(socketId)에서 대상마다 레이저를 한 줄씩 잇는다. 선은 매 프레임 다시 그려서 소켓이 애니메이션으로 움직여도 따라간다.
// 레이저를 쏜 뒤 hitDelay가 지나면(빔이 닿는 순간) 모든 대상이 피격한다. prefab은 소켓 위치에서 터지는 발사 이펙트(선택).
[CreateAssetMenu(fileName = "NewBeamCue", menuName = "Vfx/Beam Cue")]
public class BeamCue : AttackVfxCue
{
    [Tooltip("레이저 발사 후 피격까지(초)")]
    public float hitDelay = 0.1f;
    public float width = 0.08f;
    public Color color = new Color(0.4f, 0.9f, 1f);
    [Tooltip("비우면 정점 색을 쓰는 기본 머티리얼")]
    public Material material;
    [Tooltip("피격 순간 대상 위치에서 터지는 이펙트(선택)")]
    public GameObject hitPrefab;

    public override float EstimateDuration(ActionImpact impact) => Mathf.Max(duration, hitDelay);

    public override IEnumerator Play(ActionImpact impact)
    {
        Piece caster = impact.caster;
        Transform socket = VfxHelper.FindSocket(caster, socketId);

        // 대상: 피격 대상마다, 없으면(헛스윙) 주 대상 칸.
        var targets = new List<(Piece piece, Vector2Int cell)>();
        foreach (HitReaction h in impact.hits)
            targets.Add((h.piece, h.cell));
        if (targets.Count == 0 && impact.primaryCell.HasValue)
            targets.Add((null, impact.primaryCell.Value));

        var lines = new List<LineRenderer>();
        foreach (var _ in targets)
            lines.Add(VfxHelper.CreateLine("BeamLine", width, color, material, 2, false));
        if (caster != null) SpawnAt(VfxHelper.SocketOrCenter(caster, socket));

        bool fired = false;
        float t = 0f;
        while (t < duration || !fired)
        {
            Vector3 start = caster != null ? VfxHelper.SocketOrCenter(caster, socket) : lines.Count > 0 ? lines[0].GetPosition(0) : Vector3.zero;
            for (int i = 0; i < targets.Count; i++)
            {
                if (lines[i] == null) continue;
                lines[i].SetPosition(0, start);
                lines[i].SetPosition(1, EndPoint(targets[i].piece, targets[i].cell));
            }

            if (!fired && t >= hitDelay)
            {
                fired = true;
                foreach (var target in targets)
                {
                    impact.TriggerHitsAt(target.cell);
                    if (hitPrefab != null)
                    {
                        GameObject burst = VfxHelper.Spawn(hitPrefab, EndPoint(target.piece, target.cell), Quaternion.identity);
                        Board.instance.StartCoroutine(VfxHelper.DestroyAfterLifetime(burst));
                    }
                }
            }

            yield return null;
            t += Time.deltaTime;
        }

        foreach (LineRenderer line in lines)
            if (line != null) Destroy(line.gameObject);
    }

    static Vector3 EndPoint(Piece piece, Vector2Int cell)
    {
        return piece != null ? VfxHelper.PieceCenter(piece) : VfxHelper.CellPosition(cell);
    }
}
