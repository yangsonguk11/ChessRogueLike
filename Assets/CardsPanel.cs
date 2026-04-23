using UnityEngine;

public class CardsPanel : MonoBehaviour
{
    [SerializeField] CardDatabase cardDatabase;

    private void Awake()
    {
        Debug.Log(DataManager.Instance.currentData.deckCardIDs.Count);
        foreach (string cardName in DataManager.Instance.currentData.deckCardIDs)
        {
            GameObject obj = cardDatabase.SpawnCard(GetComponent<RectTransform>(), cardName);
        }
    }
}
