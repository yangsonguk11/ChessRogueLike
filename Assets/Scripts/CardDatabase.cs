using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public List<GameObject> cardPrefabs;

    public GameObject SpawnCard(RectTransform handParent, string cardName)
    {
        GameObject c = cardPrefabs.Find(p => p.name == cardName);
        return Instantiate(c, handParent);
    }
}
