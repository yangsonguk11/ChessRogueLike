using UnityEngine;

[CreateAssetMenu(fileName = "NewPieceInfo", menuName = "PieceInfo")]
public class PieceInfo : ScriptableObject
{
    [SerializeField] string _pieceName;
    [SerializeField] int _teamID;

    [Header("½ºÅÈ")]
    [SerializeField] int _maxHp;
    [SerializeField] int _colDamage;
    [SerializeField] RangeInfoSO _rangeInfoSO;

    public string PieceName => _pieceName;
    public int TeamID => _teamID;

    public int MaxHp => _maxHp;
    public int ColDamage => _colDamage;
    public RangeInfoSO RangeInfoSO => _rangeInfoSO;
}
