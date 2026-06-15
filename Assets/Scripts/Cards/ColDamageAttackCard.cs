using UnityEngine;

public class ColDamageAttackCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "ColDamageAttackCard";
        Cost = 1;
        type = CardType.Attack;

        effects.Add(new CardEffect(
            Board.BoardMode.command,
            EffectType.Damage,
            0,
            TargetLogic.NearestEnemy,
            null
        )
        { useColDamageAsDmg = true, animTrigger = "Attack" });
    }

    public override string EffectDescription => "이동 범위 내 적에게 이동공격력만큼 피해를 줍니다.";
}
