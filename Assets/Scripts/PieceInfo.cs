using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPieceInfo", menuName = "PieceInfo")]
public class PieceInfo : ScriptableObject
{
    [SerializeField] string _pieceName;
    [SerializeField] int _teamID;

    [Header("기본")]
    [SerializeField] int _maxHp;
    [SerializeField] int _colDamage;
    [SerializeField] int _shieldBonus;
    [SerializeField] RangeInfoSO _rangeInfoSO;
    [SerializeField] RangeInfoSO _moveAttackRangeInfoSO;

    [Header("영입")]
    [Tooltip("이 기물이 세이브에 새로 추가될 때(DataManager.AddPiece) 시작 덱으로 쓸 카드 목록. CardDatabase에 등록된 카드 이름.")]
    [SerializeField] List<string> _defaultDeckCardIDs;

    public string PieceName => _pieceName;
    public int TeamID => _teamID;

    public int MaxHp => _maxHp;
    public int ColDamage => _colDamage;
    public int ShieldBonus => _shieldBonus;
    public RangeInfoSO RangeInfoSO => _rangeInfoSO;
    public RangeInfoSO MoveAttackRangeInfoSO => _moveAttackRangeInfoSO;
    public List<string> DefaultDeckCardIDs => _defaultDeckCardIDs;
}
