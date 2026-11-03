using System.Collections.Generic;
using UnityEngine;

// 공격 이펙트(AttackVfxCue) 조회. CardEffect.vfx에 적힌 에셋 이름으로 찾는다 — Database.prefab에 붙어 있다.
public class VfxDatabase : MonoBehaviour, IVfxDatabase
{
    public static VfxDatabase instance;

    public List<AttackVfxCue> cues = new List<AttackVfxCue>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    // 이름이 비면 null(이펙트 없음). 등록되지 않은 이름이면 경고 후 null — 연출은 이펙트 없이 그대로 진행된다.
    public AttackVfxCue GetCue(string cueName)
    {
        if (string.IsNullOrEmpty(cueName)) return null;
        AttackVfxCue cue = cues.Find(c => c != null && c.name == cueName);
        if (cue == null) Debug.LogWarning($"[VfxDatabase] 이펙트 '{cueName}'이(가) 등록되어 있지 않습니다.");
        return cue;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (cues == null) return;
        var seen = new HashSet<string>();
        foreach (AttackVfxCue cue in cues)
        {
            if (cue == null) continue;
            if (!seen.Add(cue.name))
                Debug.LogWarning($"[VfxDatabase] 이펙트 이름 '{cue.name}'이(가) 중복됩니다.", cue);
        }
    }
#endif
}
