using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RangeInfo
{
    // 한 행에서 포함되는 칸들 (true = 범위에 포함)
    public bool[] columns = new bool[7];
}

[CreateAssetMenu(fileName = "NewRangeInfo", menuName = "RangeInfo")]
public class RangeInfoSO : ScriptableObject
{

    // 행(RangeInfo)을 배열로 묶어 2차원 범위를 구성
    public RangeInfo[] rows = new RangeInfo[7];

    public List<Vector2> GetAbleRange()
    {
        List<Vector2> temp = new List<Vector2>();
        int size = rows.Length;
        int center = size / 2;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < rows[i].columns.Length; j++)
            {
                if (rows[i].columns[j])
                {
                    temp.Add(new Vector2(i - center, j - center));
                }
            }
        }
        return temp;
    }
}
