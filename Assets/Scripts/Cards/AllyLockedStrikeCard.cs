using UnityEngine;

// 적 전용(이동 강요 보스): 각 아군 위치를 중심으로 effectRange[0] 패턴을 펼쳐 피해를 준다.
// 칸은 플레이어 턴 시작 시점에 잠기므로(CardEffect.lockOnAllyPositions), 예고를 보고 그 칸에서 벗어나면 피할 수 있다.
// 패턴(effectRange[0])과 피해(damage)만 다른 컴포넌트를 여러 개 붙여 서로 다른 패턴으로 쓴다.
public class AllyLockedStrikeCard : Card
{
    public int damage = 8;

    public override void Awake()
    {
        base.Awake();
        Name = "AllyLockedStrikeCard";
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
            lockOnAllyPositions = true,
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription => $"아군 위치를 표적으로 잠근 뒤, 그 범위에 {EffectiveDmg(effects[0])} 피해를 줍니다.";
}
