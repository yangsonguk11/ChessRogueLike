using System.Collections.Generic;
using UnityEngine;

public class RangeInfoSODatabase : MonoBehaviour
{

    public static RangeInfoSODatabase instance;

    private void Awake()
    {
        if (instance == null) instance = this; 
    }
    public List<RangeInfoSO> RangeInfos;

    public RangeInfoSO GetRangeInfoSO(string RangeInfoSOName)
    {
        RangeInfoSO c = RangeInfos.Find(p => p.name == RangeInfoSOName);
        return c;
    }
}
