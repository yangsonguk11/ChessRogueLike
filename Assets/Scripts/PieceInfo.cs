using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPieceInfo", menuName = "PieceInfo")]
public class PieceInfo : ScriptableObject
{
    [Tooltip("기물 이름 겸 조회 키 — 레벨 배치(LevelData.placements.name)·소환·합류·세이브·보상 풀 조회와 화면 표시에 모두 쓰인다.\n등록된 기물끼리 겹치면 안 된다(PieceDatabase가 경고). 프리팹 파일 이름과는 무관.")]
    [SerializeField] string _pieceName;
    [SerializeField] int _teamID;
    [SerializeField] bool _isSummon;

    [Header("기본")]
    [SerializeField] int _maxHp;
    [SerializeField] int _colDamage;
    [SerializeField] int _shieldBonus;
    [SerializeField] RangeInfoSO _rangeInfoSO;
    [SerializeField] RangeInfoSO _moveAttackRangeInfoSO;

    [Header("영입")]
    [Tooltip("이 기물이 세이브에 새로 추가될 때(DataManager.AddPiece) 시작 덱으로 쓸 카드 목록. CardDatabase에 등록된 카드 이름.")]
    [SerializeField] List<string> _defaultDeckCardIDs;

    [Header("직업")]
    [Tooltip("이 기물이 속한 직업. 전투 보상 화면에서 카드 풀을 결정하는 데 쓰인다.")]
    [SerializeField] JobInfo _job;

    public string PieceName => _pieceName;
    public int TeamID => _teamID;
    public bool IsSummon => _isSummon;

    public int MaxHp => _maxHp;
    public int ColDamage => _colDamage;
    public int ShieldBonus => _shieldBonus;
    public RangeInfoSO RangeInfoSO => _rangeInfoSO;
    public RangeInfoSO MoveAttackRangeInfoSO => _moveAttackRangeInfoSO;
    public List<string> DefaultDeckCardIDs => _defaultDeckCardIDs;
    public JobInfo Job => _job;
}
