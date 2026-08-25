using UnityEngine;

public class PieceEffectDatabase : MonoBehaviour, IPieceEffectDatabase
{
    public static PieceEffectDatabase instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public GameObject healEffectPrefab;
    public GameObject statusEffectPrefab;

    public GameObject HealEffectPrefab => healEffectPrefab;
    public GameObject StatusEffectPrefab => statusEffectPrefab;
}
