using UnityEngine;

// 적 전용(수호자 GuardianPiece의 평상시 패턴): 시전자 중심 넓은 범위(effectRange[0])의 동료 전원에게
// shieldAmount만큼 방어도를 부여한다. 범위에 시전자 칸이 포함돼 있으면 자신도 받는다.
// 범위는 다른 적 행동과 똑같이 보드 예고(적 색)·칸 정보창·행동 연출로 표시된다.
public class EnemyAreaShieldCard : Card
{
    public int shieldAmount = 2;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAreaShieldCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Shield,
            dmg = shieldAmount,
            targetlogic = TargetLogic.AllAlliesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "Shield",
        });
    }

    public override string EffectDescription => $"모든 아군(자신 포함)에게 방어도 {EffectiveShield(effects[0])}를 부여합니다.";
}
