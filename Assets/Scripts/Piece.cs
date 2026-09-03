using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(PieceEffect))]
public abstract class Piece : MonoBehaviour
{
    [Tooltip("필수 입력 — Awake()에서 teamID를 이 값의 TeamID로 덮어씀. Enemy/AutoPiece 계열은 RangeInfoSO도 여기서만 읽음.")]
    [SerializeField] PieceInfo pieceInfo;
    public PieceCanvas pieceCanvas;

    public new string name;
    [Tooltip("Ally(플레이어 로스터)로 스폰되면 SetPieceData가 덮어씀 — 여기 값은 무시됨.\nEnemy/AutoAlly처럼 레벨에 직접 배치·소환되는 기물은 SetPieceData를 안 거치므로 여기 값을 그대로 씀 — 0으로 두면 즉시 사망 판정된다.")]
    public int hp;
    [Tooltip("hp와 동일 조건 — Ally는 SetPieceData가 덮어씀, Enemy/AutoAlly는 여기 값이 그대로 쓰임.")]
    public int maxhp;
    [Tooltip("hp와 동일 조건 — Ally는 SetPieceData가 덮어씀, Enemy/AutoAlly는 여기 값이 그대로 쓰임.")]
    public int colDamage;
    [ReadOnlyInInspector] public int baseColDamage; // Awake()에서 항상 colDamage를 그대로 복사함 — 여기 채워도 덮어써짐
    public int colDamageBonus; // 영구 강화로 누적된 이동공격력 보너스. 전투 시작 시 colDamage에 합산된다.
    public int ColDamageDelta => colDamage - baseColDamage;
    [Tooltip("hp와 동일 조건 — Ally는 SetPieceData가 덮어씀, Enemy/AutoAlly는 여기 값이 그대로 쓰임.")]
    public int shieldBonus;
    [ReadOnlyInInspector] public int baseShieldBonus; // Awake()에서 항상 shieldBonus를 그대로 복사함 — 여기 채워도 덮어써짐
    public int shieldBonusBonus; // 영구 강화로 누적된 방어막 보너스. 전투 시작 시 shieldBonus에 합산된다.
    public int ShieldBonusDelta => shieldBonus - baseShieldBonus;
    [ReadOnlyInInspector] public int teamID; // Awake()에서 항상 pieceInfo.TeamID로 덮어씀 — teamID는 pieceInfo에서 바꿔야 함
    [ReadOnlyInInspector] public bool isSummon; // Awake()에서 항상 pieceInfo.IsSummon으로 덮어씀 — isSummon도 pieceInfo에서 바꿔야 함
    int _shield;
    public int shield
    {
        get => _shield;
        set
        {
            _shield = value;
            UpdateShieldVisual();
        }
    }
    [ReadOnlyInInspector] public RangeInfoSO moveableRange; // Ally는 SetPieceData가 덮어씀, Enemy/AutoPiece는 아예 안 읽음(카드의 effectRange만 사용)

    // teamID==0(아군)일 때만 SetPieceData에서 생성됨. 적/NPC는 null로 유지.
    public PieceDeck pieceDeck;

    // DataManager.currentData.pieceData에서 이 기물에 해당하는 인덱스. 덱은 더 이상 손패/버림/덱 더미를
    // 스캔해서 만들지 않고 이 인덱스로 저장된 deckCardIDs를 그대로 이어받는다(Board.SavePlayerPiecesToDataManager
    // 가 매번 다시 채워준다). 스폰 경로 밖에서 만들어진 경우 -1로 남아 저장에 반영되지 않는다.
    [ReadOnlyInInspector] public int pieceDataIndex = -1; // Awake()에서 손대지 않음 — 스폰 시점에 외부(Board)가 채워줌

    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    public bool movedThisTurn;

    PieceEffect pieceEffect;

    public void AddStatusEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
        effect.OnApply(this);
    }

    public bool IsStunned() => activeEffects.Exists(e => e is StunEffect);

    public int TriggerReceiveMoveAttack(Piece attacker)
    {
        int total = 0;
        foreach (var effect in activeEffects)
            total += effect.OnReceiveMoveAttack(this, attacker);
        return total;
    }

    void ProcessStatusEffects()
    {
        // 같은 종류의 상태이상(독/화상/재생)이 여러 개 걸려 있어도 OnTurnEnd에서 곧바로 적용하지 않고
        // 종류별로 수치를 누적해뒀다가, 순회가 끝난 뒤 한 번에 적용 + 텍스트도 한 번만 띄운다.
        int poisonTotal = 0, burningTotal = 0, regenTotal = 0;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];
            if (effect is TurnEffect) continue; // Board가 처리

            if (effect is PoisonEffect poison) poisonTotal += poison.damagePerTurn;
            else if (effect is BurningEffect burning) burningTotal += burning.damagePerTurn;
            else if (effect is RegenEffect regen) regenTotal += regen.healPerTurn;

            bool stillActive = effect.OnTurnEnd(this); // 지속시간 감소만 수행(피해/회복은 아래에서 합산 적용)
            if (!stillActive)
            {
                activeEffects.RemoveAt(i);
                effect.OnRemove(this);
            }
        }

        if (poisonTotal > 0)
        {
            GetDamage(poisonTotal, isAttack: false);
            if (Board.instance != null) Board.instance.StartCoroutine(DamageText(poisonTotal));
        }
        if (burningTotal > 0)
        {
            GetDamage(burningTotal, isAttack: false);
            if (Board.instance != null) Board.instance.StartCoroutine(DamageText(burningTotal));
        }
        if (regenTotal > 0)
        {
            int healed = GetHeal(regenTotal);
            if (healed > 0 && Board.instance != null) Board.instance.StartCoroutine(HealText(healed));
        }
    }
    public virtual void Awake()
    {
        baseColDamage = colDamage;
        baseShieldBonus = shieldBonus;
        teamID = pieceInfo.TeamID;
        isSummon = pieceInfo.IsSummon;
        pieceEffect = GetComponent<PieceEffect>();
        UpdateShieldVisual();
        if (!isSummon)
        {
            if (teamID == 0) GameManager.instance?.AddAlly(gameObject);
            else if (teamID == 1) GameManager.instance?.AddEnemy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (!isSummon)
        {
            if (teamID == 0) GameManager.instance?.RemoveAlly(gameObject);
            else if (teamID == 1) GameManager.instance?.RemoveEnemy(gameObject);
        }
    }

    void UpdateShieldVisual()
    {
        if (pieceEffect != null) pieceEffect.SetVisible(shield > 0);
    }
    public void SetPieceData(PieceData data)
    {
        name = data.pieceName;
        hp = data.hp;
        maxhp = data.maxHp;
        baseColDamage = data.colDamage;
        colDamageBonus = data.colDamageBonus;
        colDamage = baseColDamage + colDamageBonus;
        baseShieldBonus = data.shieldBonus;
        shieldBonusBonus = data.shieldBonusBonus;
        shieldBonus = baseShieldBonus + shieldBonusBonus;
        teamID = data.teamID;
        shield = 0;
        IRangeInfoSODatabase rangeInfoDb = RangeInfoSODatabase.instance;
        moveableRange = rangeInfoDb.GetRangeInfoSO(data.rangeinfoname);

        if (teamID == 0)
        {
            pieceDeck = new PieceDeck();
            CardCanvas.instance?.SpawnDeckForPiece(this, data.deckCardIDs ?? new List<string>());
        }
    }

    // deckCardIDs는 손패/버림/덱 더미를 스캔해 만드는 대신, 저장돼 있던 값을 그대로 이어받는다.
    // 전투 중 카드를 얻는 효과(FetchAttackCard 등)로 손패에 잠깐 들어온 카드는 이번 전투에서만 쓰이고
    // 저장에는 반영되지 않는다 — 영구 획득은 항상 DataManager.AddCardToPieceDeck을 통해서만 이뤄진다.
    List<string> SavedDeckCardIDs()
    {
        var pieces = DataManager.Instance?.Pieces;
        if (pieces == null || pieceDataIndex < 0 || pieceDataIndex >= pieces.Count) return new List<string>();
        List<string> saved = pieces[pieceDataIndex].deckCardIDs;
        return saved != null ? new List<string>(saved) : new List<string>();
    }

    public PieceData GetPieceData()
    {
        return new PieceData
        {
            pieceName = name,
            teamID = teamID,
            hp = hp,
            maxHp = maxhp,
            colDamage = baseColDamage,
            colDamageBonus = colDamageBonus,
            shieldBonus = baseShieldBonus,
            shieldBonusBonus = shieldBonusBonus,
            rangeinfoname = moveableRange != null ? moveableRange.name : "",
            deckCardIDs = SavedDeckCardIDs()
        };
    }
    public virtual List<Vector2Int> GetMoveableButton() { return pieceInfo.RangeInfoSO.GetAbleRange(); }
    // 설정 안 돼 있으면 중앙 1칸(부딫힌 대상만 공격)으로 폴백 — 기존 단일 타겟 이동공격 동작을 그대로 유지.
    public virtual List<Vector2Int> GetMoveAttackRange()
    {
        return pieceInfo.MoveAttackRangeInfoSO != null
            ? pieceInfo.MoveAttackRangeInfoSO.GetAbleRange()
            : new List<Vector2Int> { Vector2Int.zero };
    }
    public RangeInfoSO MoveAttackRangeInfoSO => pieceInfo.MoveAttackRangeInfoSO;
    // isAttack: false면 독/화상 같은 상태이상 틱 데미지 — OnHit 유물은 '공격'을 받았을 때만 발동해야 하므로 제외한다.
    public int GetDamage(int damage, bool isAttack = true)
    {
        if(shield < damage)
        {
            hp -= damage - shield;
            shield = 0;
        }
        else
        {
            shield -= damage;
        }

        if (isAttack && teamID == 0) Board.instance?.TriggerRelicsOnHit(this);
        return hp;
    }
    // 실제로 회복된 양(최대 체력 클램프 적용)을 반환한다. 호출부는 이 반환값을 회복 텍스트 표시에 그대로 써야 한다.
    public int GetHeal(int amount)
    {
        int healed = Mathf.Clamp(amount, 0, Mathf.Max(0, maxhp - hp));
        hp += healed;
        return healed;
    }
    public int GetShield(int damage)
    {
        shield += damage;
        return shield;
    }
    // colDamage를 변경하고 버프/디버프 텍스트+파티클을 함께 재생한다. permanent가 true면 colDamageBonus도 같이 올려
    // GetPieceData 저장을 거쳐 다음 전투로도 이어지게 한다(영구 강화용). baseColDamage는 건드리지 않는다.
    public int AddColDamage(int delta, bool permanent = false)
    {
        colDamage += delta;
        if (permanent) colDamageBonus += delta;
        if (delta != 0)
            ShowStatusText(delta > 0 ? $"이동공격력 +{delta}" : $"이동공격력 {delta}", delta > 0, new Color(1f, 0.27f, 0.27f));
        return colDamage;
    }
    // shieldBonus를 변경하고 버프/디버프 텍스트+파티클을 함께 재생한다. permanent 의미는 AddColDamage와 동일.
    public int AddShieldBonus(int delta, bool permanent = false)
    {
        shieldBonus += delta;
        if (permanent) shieldBonusBonus += delta;
        if (delta != 0)
            ShowStatusText(delta > 0 ? $"방어막 보너스 +{delta}" : $"방어막 보너스 {delta}", delta > 0, new Color(1f, 0.27f, 0.27f));
        return shieldBonus;
    }
    public virtual void OnTurnEnd()
    {
        ProcessStatusEffects();
    }
    public virtual void OnTurnEndOther()
    {

    }
    // Enemy가 다음 행동 예고에 쓰는 것과 동일한 ShowActionText/ClearActionText를 그대로 재사용해서,
    // 아군도 기절 중이면 같은 자리에 같은 방식으로 스턴 아이콘을 띄운다. 기절이 아니면(또는 풀리면) 지운다.
    public virtual void ActionText()
    {
        if (IsStunned())
            pieceCanvas?.ShowActionText("<sprite name=\"Stun\">");
        else
            pieceCanvas?.ClearActionText();
    }
    bool isDeathScheduled;

    public IEnumerator DamageText(int damage)
    {
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
        yield return null;
    }

    public IEnumerator HealText(int damage)
    {
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
        if (this != null && pieceEffect != null)
            pieceEffect.PlayHealEffect();
        yield return null;
    }

    public IEnumerator ShieldText(int damage)
    {
        yield return new WaitForSeconds(1f);
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
    }

    public void TriggerAnim(string triggerName)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger(triggerName);
    }

    public void ShowStatusText(string text, bool isBuff, Color effectColor)
    {
        if (pieceCanvas != null)
            pieceCanvas.InvokeStatusText(text, isBuff, effectColor);
        if (!isBuff && pieceEffect != null)
            pieceEffect.PlayDebuffEffect(effectColor);
        else if (isBuff && pieceEffect != null)
            pieceEffect.PlayBuffEffect();
    }


    public IEnumerator DeathCor()
    {
        if (isDeathScheduled) yield break;
        isDeathScheduled = true;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
