using UnityEngine;

public enum TurnPhase { OwnTurnStart, OwnTurnEnd }

// 기물에 걸리는 모든 상태(상태이상·버프·턴 효과)의 공통 기반. 턴 타이밍 처리는 Board.EnqueueTeamStatusEffects 하나가
// 모든 종류에 같은 규칙으로 한다 — 그 타이밍의 효과(TurnEffectAt)를 효과 큐에 넣고, TickPhase가 맞으면 지속시간을 줄인다.
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

    // 지속시간이 줄어드는 타이밍 — 걸린 기물의 자기 턴 시작 또는 종료.
    public virtual TurnPhase TickPhase => TurnPhase.OwnTurnEnd;

    // phase 타이밍에 걸린 기물을 시전자로 효과 큐에서 실행할 효과(없으면 null). 독·재생 틱, 카드가 정의한 턴 효과 등.
    public virtual CardEffect TurnEffectAt(TurnPhase phase) => null;

    // true면 같은 기물에 걸린 같은 종류끼리 TurnEffectAt의 dmg를 합산해 한 번만 실행한다(독·화상·재생).
    public virtual bool SumsTurnEffect => false;

    public virtual void OnApply(Piece piece) { }

    // TickPhase 타이밍에 한 번 호출. duration이 음수면 영구 지속(만료되지 않음). 만료되면 false.
    public bool TickDuration()
    {
        if (duration < 0) return true;
        duration--;
        return duration > 0;
    }

    // 만료·정화로 사라질 때 호출된다(Piece.RemoveStatusEffect — activeEffects에서 이미 빠진 뒤). 기본은 "… 해제" 텍스트.
    public virtual void OnRemove(Piece piece) => piece.ShowStatusText(DisplayName + " 해제", !IsBuff, EffectColor);

    // 이동공격을 받았을 때 호출. 반격 피해량을 반환.
    public virtual int OnReceiveMoveAttack(Piece self, Piece attacker) { return 0; }

    // attack/moveattack으로 피해를 받을 때 그 피해에 더할 값(취약 등). Piece.ModifyIncomingAttackDamage가 합산한다.
    public virtual int IncomingAttackDamageBonus => 0;
}
