using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
            if (Board.instance != null) Board.instance.StartCoroutine(DamageText(poisonTotal, isAttack: false));
        }
        if (burningTotal > 0)
        {
            GetDamage(burningTotal, isAttack: false);
            if (Board.instance != null) Board.instance.StartCoroutine(DamageText(burningTotal, isAttack: false));
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

    // 실드가 꺼지는 경우(0 이하로 떨어짐)만 자동으로 처리한다. 켜지는 시점(GetShield)은 시전자의 실드
    // 애니메이션에 맞춰 대상이 하는 유일한 반응이므로, ShieldVisualOn()을 통해 명시적으로 호출해야 한다.
    void UpdateShieldVisual()
    {
        if (pieceEffect != null && shield <= 0) pieceEffect.SetVisible(false);
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
    // colDamage를 변경하고(즉시) 버프/디버프 텍스트+파티클+사운드를 재생한다. permanent가 true면
    // colDamageBonus도 같이 올려 GetPieceData 저장을 거쳐 다음 전투로도 이어지게 한다(영구 강화용).
    // baseColDamage는 건드리지 않는다. showReaction: false면 텍스트/파티클/사운드를 재생하지 않는다 —
    // 캐스터 애니메이션의 OnAnimationEvent 시점에 맞춰 ColDamageUpReaction으로 따로 재생하고 싶을 때 사용.
    public int AddColDamage(int delta, bool permanent = false, bool showReaction = true)
    {
        colDamage += delta;
        if (permanent) colDamageBonus += delta;
        if (showReaction) ShowColDamageReaction(delta);
        return colDamage;
    }
    void ShowColDamageReaction(int delta)
    {
        if (delta != 0)
            ShowStatusText(delta > 0 ? $"이동공격력 +{delta}" : $"이동공격력 {delta}", delta > 0, new Color(1f, 0.27f, 0.27f));
    }
    // AddColDamage(showReaction: false)와 짝을 이루는, 텍스트/파티클/사운드만 따로 재생하는 버전 —
    // PlayCasterAndTargetReaction의 targetReaction으로 넘겨서 캐스터 애니메이션과 동기화할 때 사용.
    public IEnumerator ColDamageUpReaction(int delta)
    {
        ShowColDamageReaction(delta);
        yield return null;
    }

    // shieldBonus를 변경하고(즉시) 버프/디버프 텍스트+파티클+사운드를 재생한다. permanent/showReaction
    // 의미는 AddColDamage와 동일.
    public int AddShieldBonus(int delta, bool permanent = false, bool showReaction = true)
    {
        shieldBonus += delta;
        if (permanent) shieldBonusBonus += delta;
        if (showReaction) ShowShieldBonusReaction(delta);
        return shieldBonus;
    }
    void ShowShieldBonusReaction(int delta)
    {
        if (delta != 0)
            ShowStatusText(delta > 0 ? $"방어막 보너스 +{delta}" : $"방어막 보너스 {delta}", delta > 0, new Color(1f, 0.27f, 0.27f));
    }
    public IEnumerator ShieldBonusUpReaction(int delta)
    {
        ShowShieldBonusReaction(delta);
        yield return null;
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

    // isAttack: false면 독/화상 같은 상태이상 틱 데미지 — GetDamage의 isAttack과 동일한 의미로,
    // 그 경우엔 attackImpact 타격음을 재생하지 않는다(틱마다 무기 타격음이 울리면 어색함).
    // isCounter: true면 가시/반격류로 되돌려받는 피해 — attackImpact 대신 counterAttack을 재생한다.
    public IEnumerator DamageText(int damage, bool isAttack = true, bool isCounter = false)
    {
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
        if (isCounter) AudioManager.instance?.PlayCounterAttack();
        else if (isAttack) AudioManager.instance?.PlayAttackImpact();
        yield return null;
    }

    public IEnumerator HealText(int damage)
    {
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
        if (this != null && pieceEffect != null)
            pieceEffect.PlayHealEffect();
        AudioManager.instance?.PlayHeal();
        yield return null;
    }

    public IEnumerator ShieldText(int damage)
    {
        yield return new WaitForSeconds(1f);
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
    }

    // 실드가 걸리는 시점의 유일한 대상 반응 — 실드 애니메이션은 시전자만 재생하고(대상은 자신의
    // 애니메이터 트리거를 갖지 않음), 대상은 pieceEffect 오빗을 켜는 것만 한다.
    // resultingShield(부여 후 실드량)가 0 이하면(예: dmg가 0으로 계산된 경우) 표시할 실드 자체가
    // 없으므로 켜지 않는다.
    public IEnumerator ShieldVisualOn(int resultingShield)
    {
        if (this != null && pieceEffect != null && resultingShield > 0)
        {
            pieceEffect.SetVisible(true);
            AudioManager.instance?.PlayShieldApply();
        }
        yield return null;
    }

    // 사망이 확정된 대상의 반응 목록(targetCoroutines)에 합류시켜서, "Die" 트리거 애니메이션과 같은
    // 시점(캐스터의 OnAnimationEvent)에 사망 사운드가 재생되게 한다. 실제 오브젝트 파괴(Destroy)는
    // DeathCor가 별도로 더 나중에(사망 연출용 대기 후) 처리 — 이건 사운드 타이밍만 담당한다.
    public IEnumerator PieceDeathSound()
    {
        AudioManager.instance?.PlayPieceDeath();
        yield return null;
    }

    // 소환 시 파티클 연출 + 등장 스케일 팝인. targetScale은 호출부(SummonPieceAt)가 Instantiate 직후
    // localScale을 0으로 숨겨두기 전의 프리팹 원래 스케일 — 이 시점에 그 크기로 다시 키워서 "짠" 하고
    // 나타나게 한다. 텍스트 없이 파티클만 재생 — 지금은 임시로 버프 파티클을 재사용한다.
    public IEnumerator SummonVisualEffect(Vector3 targetScale)
    {
        if (this == null) yield break;
        if (pieceEffect != null) pieceEffect.PlayBuffEffect();
        AudioManager.instance?.PlayPieceSummon();
        yield return transform.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack).WaitForCompletion();
    }

    public void TriggerAnim(string triggerName)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger(triggerName);
    }

    // Charge/Stun처럼 "켜지면 다른 애니메이션을 무시하고 유지되다가, 명시적으로 꺼야 사라지는" 상태를
    // Bool 파라미터로 켜고 끈다(트리거와 달리 값이 유지됨 — Any State 전환의 켜짐/꺼짐 조건으로 쓰임).
    public void SetAnimBool(string paramName, bool value)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetBool(paramName, value);
    }

    public bool animationEventFired;
    // Animation Event가 호출할 콜백. 공격/힐/실드/버프 등 어떤 애니메이션 클립이든, 시전자가
    // "지금 대상에게 효과가 전달되는" 순간에 이 함수를 호출하는 이벤트를 심어두면 된다
    // (Board.Animation.cs의 PlayCasterAndTargetReaction/WaitAnimationEventThenRun 참고).
    public void OnAnimationEvent() => animationEventFired = true;

    public void ShowStatusText(string text, bool isBuff, Color effectColor)
    {
        if (pieceCanvas != null)
            pieceCanvas.InvokeStatusText(text, isBuff, effectColor);
        if (!isBuff && pieceEffect != null)
            pieceEffect.PlayDebuffEffect(effectColor);
        else if (isBuff && pieceEffect != null)
            pieceEffect.PlayBuffEffect();
        if (isBuff) AudioManager.instance?.PlayBuffApply();
        else AudioManager.instance?.PlayDebuffApply();
    }

    // ShowStatusText를 DamageText/HealText와 같은 모양(IEnumerator)으로 감싼 버전 — 상태이상 적용
    // 자체(AddStatusEffect 등)는 호출부가 GetHeal/GetShield처럼 미리 즉시 실행해두고, 텍스트/파티클/
    // 사운드만 이 코루틴으로 extra/targetCoroutines에 끼워 넣어 캐스터의 OnAnimationEvent에 동기화한다.
    public IEnumerator StatusTextReaction(string text, bool isBuff, Color effectColor)
    {
        ShowStatusText(text, isBuff, effectColor);
        yield return null;
    }


    public IEnumerator DeathCor()
    {
        if (isDeathScheduled) yield break;
        isDeathScheduled = true;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
