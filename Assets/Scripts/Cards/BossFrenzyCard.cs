using UnityEngine;

// 적 전용(보스): 자기 주변 effectRange[0] 범위에 피해를 준다. 피해에 시전자의 ColDamageDelta(이번 전투에서 오른
// 힘)가 자동으로 더해지므로, 힘을 쌓는 보스(데미지 강요/스케일 강요)일수록 세진다.
public class BossFrenzyCard : Card
{
    public int damage = 3;

    public override void Awake()
    {
        base.Awake();
        Name = "BossFrenzyCard";
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
            animTrigger = "AreaAttack",
        });
    }

    public override string EffectDescription => $"주변 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다. 힘이 오른 만큼 강해집니다.";
}
