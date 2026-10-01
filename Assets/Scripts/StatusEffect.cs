using UnityEngine;

public abstract class StatusEffect
{
    public abstract string DisplayName { get; }
    public abstract bool IsBuff { get; }
    public virtual Color EffectColor => new Color(1f, 0.27f, 0.27f); // 기본 디버프 색 (#FF4444)
    public int duration;

    public virtual void OnApply(Piece piece) { }

    // duration이 음수면 영구 지속(만료되지 않음). Returns false when the effect expires (duration ran out)
    public virtual bool OnTurnEnd(Piece piece)
    {
        if (duration < 0) return true;
        duration--;
        return duration > 0;
    }

    public virtual void OnRemove(Piece piece) { }

    // 이동공격을 받았을 때 호출. 반격 피해량을 반환.
    public virtual int OnReceiveMoveAttack(Piece self, Piece attacker) { return 0; }

    // attack/moveattack으로 피해를 받을 때 그 피해에 더할 값(취약 등). Piece.ModifyIncomingAttackDamage가 합산한다.
    public virtual int IncomingAttackDamageBonus => 0;
}
