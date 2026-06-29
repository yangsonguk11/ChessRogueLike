using UnityEngine;

public class PieceEffectDatabase : MonoBehaviour
{
    public static PieceEffectDatabase instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public GameObject healEffectPrefab;
    public GameObject statusEffectPrefab;
}
