using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour, ICardDatabase
{
    public static CardDatabase instance;

    public List<GameObject> cardPrefabs;
    public List<GameObject> spritesPrefabs;

    // cardPrefabs를 이름으로 즉시 찾기 위한 캐시. SpawnCard가 카드 뽑을 때마다(전투 중 드로우 포함)
    // 호출되므로 List.Find로 매번 선형 탐색하는 대신 Awake에서 한 번만 만들어둔다.
    Dictionary<string, GameObject> cardsByName;

    private void Awake()
    {
        if (instance == null) instance = this;

        cardsByName = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in cardPrefabs)
            if (prefab != null) cardsByName[prefab.name] = prefab;
    }

    public GameObject SpawnCard(RectTransform handParent, string cardName)
    {
        if (!cardsByName.TryGetValue(cardName, out GameObject c))
        {
            string available = string.Join(", ", cardPrefabs.ConvertAll(p => p != null ? p.name : "null"));
            Debug.LogError($"[CardDatabase] 카드를 찾을 수 없습니다: \"{cardName}\"\n등록된 카드: {available}");
            return null;
        }
        GameObject obj = Instantiate(c, handParent);
        Card card = obj.GetComponent<Card>();
        if (card != null) card.cardID = cardName;
        return obj;
    }
    public GameObject SpawnSprite(RectTransform handParent, string cardName)
    {
        GameObject c = spritesPrefabs.Find(p => p.name == cardName);
        return Instantiate(c, handParent);
    }

    public List<string> PickRandomDistinct(int count, IEnumerable<string> exclude = null)
    {
        var excludeSet = exclude != null ? new HashSet<string>(exclude) : null;
        var pool = new List<string>();
        foreach (GameObject prefab in cardPrefabs)
            if (prefab != null && (excludeSet == null || !excludeSet.Contains(prefab.name)))
                pool.Add(prefab.name);

        var result = new List<string>();
        int n = Mathf.Min(count, pool.Count);
        for (int i = 0; i < n; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    public IEnumerable<string> GetAllCardNames() => cardsByName.Keys;
}
