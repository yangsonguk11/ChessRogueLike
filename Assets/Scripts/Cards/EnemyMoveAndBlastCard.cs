using UnityEngine;

// 적 전용(수호자 Enemy-Guardian의 격노 패턴): 가장 가까운 적 쪽으로 이동한 뒤(이동 범위는 기물 PieceInfo의 RangeInfoSO),
// 이동한 자리를 중심으로 넓은 범위(effectRange[0])의 적 전원에게 damage + 힘 보너스만큼 피해를 준다.
// 두 번째 효과는 기록된 시전자의 현재 칸을 기준으로 펼쳐지므로 이동 뒤 위치에서 터진다. 동료가 모두 쓰러진 뒤 매 턴 반복된다.
public class EnemyMoveAndBlastCard : Card
{
    public int damage = 6;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyMoveAndBlastCard";
        Cost = 0;
        type = CardType.Attack;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Move,
            dmg = 0,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = null, // null이면 기물 이동 범위를 쓴다(EnemyMoveCard와 같음)
            animTrigger = "Move",
        });
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

    public override string EffectDescription => $"이동한 뒤 주변 범위의 적 모두에게 {EffectiveDmg(effects[1])} 피해를 줍니다.";
}
