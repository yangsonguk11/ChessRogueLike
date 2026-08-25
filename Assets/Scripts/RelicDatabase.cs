using System;
using System.Collections.Generic;
using UnityEngine;

// 유물 이름(문자열)으로 (1) 실제 효과 데이터(Relic)를 만들거나 (2) 화면에 보여줄 아이콘 프리팹을
// 스폰한다. 이 둘은 서로 완전히 독립적이다 — 아이콘 프리팹(iconPrefabs)에는 스크립트가 없는
// 순수 시각 오브젝트(Image)만 있으면 되고, 유물 기능(Relic 데이터)은 Assets/Scripts/Relics/ 아래의
// Relic 서브클래스가 담당한다. 카드가 새로 추가돼도 CardDatabase 코드를 안 고치듯, 새 유물도
// Relics/ 폴더에 파일만 추가하면 된다 — relicName(클래스 이름)을 리플렉션으로 찾아 생성한다.
public class RelicDatabase : MonoBehaviour, IRelicDatabase
{
    public static RelicDatabase instance;

    [Tooltip("유물 아이콘 프리팹 목록. 프리팹 GameObject 이름이 곧 유물 식별자(Relic 서브클래스 이름)와 같아야 한다.")]
    public List<GameObject> iconPrefabs;

    // iconPrefabs를 이름으로 즉시 찾기 위한 캐시. CardDatabase.cardsByName과 동일한 이유.
    Dictionary<string, GameObject> iconsByName;

    void Awake()
    {
        if (instance == null) instance = this;

        iconsByName = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in iconPrefabs)
            if (prefab != null) iconsByName[prefab.name] = prefab;
    }

    // relicName과 이름이 같은 Relic 서브클래스를 찾아 인스턴스를 만든다.
    public Relic CreateRelic(string relicName)
    {
        Type type = Type.GetType(relicName);
        if (type == null || !typeof(Relic).IsAssignableFrom(type) || type.IsAbstract)
        {
            Debug.LogError($"[RelicDatabase] 유물을 찾을 수 없습니다: \"{relicName}\"");
            return null;
        }
        return (Relic)Activator.CreateInstance(type);
    }

    // relicName에 해당하는 아이콘 프리팹을 parent 아래에 스폰한다. 효과 로직은 전혀 모르고,
    // RelicIcon.relicName만 채워서(CardDatabase.SpawnCard가 card.cardID를 채우는 것과 동일한 역할)
    // 호버 시 이름/설명을 스스로 조회해 보여줄 수 있게 한다.
    public GameObject SpawnIcon(Transform parent, string relicName)
    {
        if (!iconsByName.TryGetValue(relicName, out GameObject prefab))
        {
            string available = string.Join(", ", iconPrefabs.ConvertAll(p => p != null ? p.name : "null"));
            Debug.LogError($"[RelicDatabase] 유물 아이콘을 찾을 수 없습니다: \"{relicName}\"\n등록된 아이콘: {available}");
            return null;
        }
        GameObject obj = Instantiate(prefab, parent);
        RelicIcon icon = obj.GetComponent<RelicIcon>();
        if (icon != null) icon.relicName = relicName;
        return obj;
    }

    // ShopCanvas.PickShopCards(CardDatabase)와 동일한 패턴 — 유물 아이콘 풀에서 중복 없이 무작위 count개.
    public List<string> PickRandomDistinct(int count, IEnumerable<string> exclude = null)
    {
        var excludeSet = exclude != null ? new HashSet<string>(exclude) : null;
        var pool = new List<string>();
        foreach (GameObject prefab in iconPrefabs)
            if (prefab != null && (excludeSet == null || !excludeSet.Contains(prefab.name)))
                pool.Add(prefab.name);

        var result = new List<string>();
        int n = Mathf.Min(count, pool.Count);
        for (int i = 0; i < n; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }
}
