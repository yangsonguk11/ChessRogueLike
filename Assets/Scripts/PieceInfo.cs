using UnityEngine;

[CreateAssetMenu(fileName = "NewPieceInfo", menuName = "PieceInfo")]
public class PieceInfo : ScriptableObject
{
    [SerializeField] string _pieceName;
    [SerializeField] int _teamID;

    [Header("기본")]
    [SerializeField] int _maxHp;
    [SerializeField] int _colDamage;
    [SerializeField] RangeInfoSO _rangeInfoSO;
    [SerializeField] RangeInfoSO _moveAttackRangeInfoSO;

    public string PieceName => _pieceName;
    public int TeamID => _teamID;

    public int MaxHp => _maxHp;
    public int ColDamage => _colDamage;
    public RangeInfoSO RangeInfoSO => _rangeInfoSO;
    public RangeInfoSO MoveAttackRangeInfoSO => _moveAttackRangeInfoSO;
}
