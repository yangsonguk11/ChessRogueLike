using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RangeInfo
{
    // 한 행에 들어갈 데이터 (예: bool, int, float 등)
    public bool[] columns = new bool[7];
}

[CreateAssetMenu(fileName = "NewRangeInfo", menuName = "RangeInfo")]
public class RangeInfoSO : ScriptableObject
{
    // GridRow를 배열로 선언하여 2차원 구조 형성
    public RangeInfo[] rows = new RangeInfo[7];

    public List<Vector2> GetAbleRange() 
    {
        List<Vector2> temp = new List<Vector2>();
        for(int i = 0; i < 7; i++)
        {
            for(int j = 0; j < 7; j++)
            {
                if (rows[i].columns[j])
                {
                    temp.Add(new Vector2(i - 3, j - 3));
                }
            }
        }
        return temp;
    }
}