using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase instance;

    public List<GameObject> cardPrefabs;
    public List<GameObject> spritesPrefabs;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public GameObject SpawnCard(RectTransform handParent, string cardName)
    {
        GameObject c = cardPrefabs.Find(p => p.name == cardName);
        if (c == null)
        {
            string available = string.Join(", ", cardPrefabs.ConvertAll(p => p != null ? p.name : "null"));
            Debug.LogError($"[CardDatabase] 카드를 찾을 수 없습니다: \"{cardName}\"\n등록된 카드: {available}");
            return null;
        }
        return Instantiate(c, handParent);
    }
    public GameObject SpawnSprite(RectTransform handParent, string cardName)
    {
        GameObject c = spritesPrefabs.Find(p => p.name == cardName);
        return Instantiate(c, handParent);
    }
}
