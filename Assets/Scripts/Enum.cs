public enum AttackType
{
    MoveAttack,
    NormalAttack,
}

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
}
