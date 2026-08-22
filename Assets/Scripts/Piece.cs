using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(PieceEffect))]
public abstract class Piece : MonoBehaviour
{
    [SerializeField] PieceInfo pieceInfo;
    public PieceCanvas pieceCanvas;

    public new string name;
    public int hp;
    public int maxhp;
    public int colDamage;
    public int baseColDamage; // 기물의 강화 전(영입 시점) 이동공격력. 영구 강화로는 변하지 않는다.
    public int colDamageBonus; // 영구 강화로 누적된 이동공격력 보너스. 전투 시작 시 colDamage에 합산된다.
    public int ColDamageDelta => colDamage - baseColDamage;
    public int shieldBonus;
    public int baseShieldBonus; // 기물의 강화 전(영입 시점) 방어막 보너스. 영구 강화로는 변하지 않는다.
    public int shieldBonusBonus; // 영구 강화로 누적된 방어막 보너스. 전투 시작 시 shieldBonus에 합산된다.
    public int ShieldBonusDelta => shieldBonus - baseShieldBonus;
    public int teamID;
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
    public RangeInfoSO moveableRange;

    // teamID==0(아군)일 때만 SetPieceData에서 생성됨. 적/NPC는 null로 유지.
    public PieceDeck pieceDeck;

    // DataManager.currentData.pieceData에서 이 기물에 해당하는 인덱스. 덱은 더 이상 손패/버림/덱 더미를
    // 스캔해서 만들지 않고 이 인덱스로 저장된 deckCardIDs를 그대로 이어받는다(Board.SavePlayerPiecesToDataManager
    // 가 매번 다시 채워준다). 스폰 경로 밖에서 만들어진 경우 -1로 남아 저장에 반영되지 않는다.
    public int pieceDataIndex = -1;

    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    public bool movedThisTurn;

    PieceEffect pieceEffect;

    public void AddStatusEffect(StatusEffect effect)
    {
        effect.OnApply(this);
        activeEffects.Add(effect);
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
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i] is TurnEffect) continue; // Board가 처리
            bool stillActive = activeEffects[i].OnTurnEnd(this);
            if (!stillActive)
            {
                activeEffects[i].OnRemove(this);
                activeEffects.RemoveAt(i);
            }
        }
    }
    public virtual void Awake()
    {
        baseColDamage = colDamage;
        baseShieldBonus = shieldBonus;
        teamID = pieceInfo.TeamID;
        pieceEffect = GetComponent<PieceEffect>();
        UpdateShieldVisual();
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
        moveableRange = RangeInfoSODatabase.instance.GetRangeInfoSO(data.rangeinfoname);

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
        var pieces = DataManager.Instance?.currentData.pieceData;
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
    public virtual List<Vector2> GetMoveableButton() { return pieceInfo.RangeInfoSO.GetAbleRange(); }
    // 설정 안 돼 있으면 중앙 1칸(부딫힌 대상만 공격)으로 폴백 — 기존 단일 타겟 이동공격 동작을 그대로 유지.
    public virtual List<Vector2> GetMoveAttackRange()
    {
        return pieceInfo.MoveAttackRangeInfoSO != null
            ? pieceInfo.MoveAttackRangeInfoSO.GetAbleRange()
            : new List<Vector2> { Vector2.zero };
    }
    public RangeInfoSO MoveAttackRangeInfoSO => pieceInfo.MoveAttackRangeInfoSO;
    public int GetDamage(int damage)
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
    public virtual void ActionText()
    {

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
