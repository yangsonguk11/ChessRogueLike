using UnityEngine;

// 보스 패턴 2: 선풍기(바람개비) 형태로 회전하며 공격
// RangeInfoSO 패턴이 90도 회전 대칭 — 4개 날개 각 3칸
public class BossAttack2Card : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "보스 패턴 2";
        Cost = 0;
        type = CardType.Attack;
        CardEffect cf = new CardEffect(
            Board.BoardMode.targeting,
            EffectType.Damage,
            5,
            TargetLogic.AllEnemiesInRange,
            effectRange.Count > 0 ? effectRange[0] : null,
            false,
            AreaTargetMode.Fixed
        )
        { animTrigger = "AreaAttack" };
        effects.Add(cf);
    }

    public override string EffectDescription => $"선풍기 모양 범위의 적에게 {effects[0].dmg} 피해를 줍니다.";
}
