using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Piece : MonoBehaviour
{
    [SerializeField] PieceInfo pieceInfo;
    public PieceCanvas pieceCanvas;

    public new string name;
    public int hp;
    public int maxhp;
    public int colDamage;
    public int teamID;
    public int shield;
    public RangeInfoSO moveableRange;

    public List<StatusEffect> activeEffects = new List<StatusEffect>();

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
        /*
        name = pieceInfo.PieceName;
        hp = pieceInfo.MaxHp;
        maxhp = pieceInfo.MaxHp;
        colDamage = pieceInfo.ColDamage;
        teamID = pieceInfo.TeamID;
        */
    }
    public void SetPieceData(PieceData data)
    {
        name = data.pieceName;
        hp = data.hp;
        maxhp = data.maxHp;
        colDamage = data.colDamage;
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
            colDamage = colDamage,
            rangeinfoname = moveableRange != null ? moveableRange.name : ""
        };
    }
    public virtual List<Vector2> GetMoveableButton() { return pieceInfo.RangeInfoSO.GetAbleRange(); }
    public int GetDamage(int damage, AttackType type)
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
    public int GetHeal(int damage, AttackType type)
    {
        hp += damage;
        if (hp > maxhp)
            hp = maxhp;
        return hp;
    }
    public int GetShield(int damage, AttackType type)
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
        yield return null;
    }

    public IEnumerator ShieldText(int damage)
    {
        yield return new WaitForSeconds(1f);
        if (this != null && pieceCanvas != null)
            pieceCanvas.InvokeDamageText(damage);
    }

    public bool HasAnimator()
    {
        Animator anim = GetComponent<Animator>();
        return anim != null;
    }

    public void TriggerAnim(string triggerName)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger(triggerName);
    }

    public IEnumerator DeathCor()
    {
        if (isDeathScheduled) yield break;
        isDeathScheduled = true;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
