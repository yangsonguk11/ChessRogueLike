using UnityEngine;

// 적 전용(Boss-Colossus): BossFrenzyCard처럼 자기 주변 effectRange[0] 범위에 피해를 주고, 맞은 적에게 duration턴간
// 소멸 저주(ExileOnUseEffect)를 건다 — 피해 효과에 붙은 상태이상은 AreaAttackPiece가 피격자에게 건다.
// 기본값 1이면 바로 다음 플레이어 턴에 그 기물로 사용하는 카드가 모두 소멸한다. 피해에는 시전자의 ColDamageDelta가 더해진다.
public class BossExileStrikeCard : Card
{
    public int damage = 8;
    public int duration = 1;

    public override void Awake()
    {
        base.Awake();
        Name = "BossExileStrikeCard";
        Cost = 0;
        type = CardType.Attack;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = damage,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            statusEffectType = StatusEffectType.ExileOnUse,
            statusDuration = duration,
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription =>
        $"주변 적에게 {EffectiveDmg(effects[0])} 피해를 주고 {effects[0].statusDuration}턴간 소멸 저주를 겁니다. 힘이 오른 만큼 강해집니다.";
}
