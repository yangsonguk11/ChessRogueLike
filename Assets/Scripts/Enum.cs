public enum StatusEffectType
{
    None,
    Poison,
    Burning,
    Regen,
    Stun,
    Strengthen,
    Weaken,
    TurnDamageStart,    // 자기 턴 시작 시 자신에게 피해
    TurnDamageEnd,      // 자기 턴 종료 시 자신에게 피해
    TurnAoEDamageStart, // 자기 턴 시작 시 주변 적에게 광역 피해
    TurnAoEDamageEnd,   // 자기 턴 종료 시 주변 적에게 광역 피해
    Thorn,              // 이동공격을 받으면 공격자에게 반격 피해
    MovementDisabled,   // 이동 불가 (현재 게임플레이 미적용)
    Vulnerable,         // 취약: attack/moveattack으로 받는 피해 +N
    Taunt,              // 도발: 상대 AI/카드가 사거리 안의 이 기물을 우선 대상으로 삼음
    NextMoveAttackDamage,     // 다음 이동공격 피해 +statusPower
    NextMoveAttackMultiplier, // 다음 이동공격 피해 ×statusPower
    NextMoveAttackStatus,     // 다음 이동공격 적중 대상에게 CardEffect.onMoveAttackHitEffect의 상태이상 부여
}
