using UnityEngine;

public abstract class StatusEffect
{
    public abstract string DisplayName { get; }
    public abstract bool IsBuff { get; }
    public virtual Color EffectColor => new Color(1f, 0.27f, 0.27f); // 기본 디버프 색 (#FF4444)
    public int duration;

    const string RemainingTurnColor = "#AAAAAA"; // 남은 턴은 이름보다 약한 보조 정보라 회색

    // 상태 이름 + 남은 턴(TMP 리치 텍스트) — 예: "<color=분홍>취약 (+1)</color> <color=#AAAAAA>2턴</color>".
    // 지속이 음수(영구·이동공격할 때까지)면 이름만. 이름 색은 호출부가 정한다(버프 파랑 / 디버프 효과별 색).
    public string StatusLabel(string nameColor)
    {
        string name = $"<color={nameColor}>{DisplayName}</color>";
        return duration >= 0 ? $"{name} <color={RemainingTurnColor}>{duration}턴</color>" : name;
    }

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
