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
}
