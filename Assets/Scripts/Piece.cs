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
    public int baseColDamage;
    public int ColDamageDelta => colDamage - baseColDamage;
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

    public List<StatusEffect> activeEffects = new List<StatusEffect>();
    public bool movedThisTurn;

    PieceEffect pieceEffect;

    public void AddStatusEffect(StatusEffect effect)
    {
        StatusEffect existing = activeEffects.Find(e => e.GetType() == effect.GetType());
        if (existing != null)
        {
            existing.duration = Mathf.Max(existing.duration, effect.duration);
            return;
        }
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
        colDamage = data.colDamage;
        baseColDamage = data.colDamage;
        teamID = data.teamID;
        shield = 0;
        moveableRange = RangeInfoSODatabase.instance.GetRangeInfoSO(data.rangeinfoname);
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
            rangeinfoname = moveableRange != null ? moveableRange.name : ""
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

    public void ShowStatusText(string text, bool isBuff)
    {
        if (pieceCanvas != null)
            pieceCanvas.InvokeStatusText(text, isBuff);
        if (!isBuff && pieceEffect != null)
            pieceEffect.PlayDebuffEffect();
    }

    public IEnumerator DeathCor()
    {
        if (isDeathScheduled) yield break;
        isDeathScheduled = true;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
