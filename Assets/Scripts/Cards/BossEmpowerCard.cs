// 적 전용(스케일 강요 보스): 자신의 이동공격력을 amount만큼 올린다(이번 전투 동안 누적).
// 이동공격 피해와 공격 카드 피해(dmg + ColDamageDelta)가 함께 오른다.
public class BossEmpowerCard : Card
{
    public int amount = 3;

    public override void Awake()
    {
        base.Awake();
        Name = "BossEmpowerCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;
        dragDropTarget = DragDropTarget.Self;

        // BoardMode.command + TargetLogic.self → ResolveEnemyTarget이 selectedButton(보스 자신)을 반환
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.ColDamageUp,
            dmg = amount,
            targetlogic = TargetLogic.self,
            animTrigger = "Buff",
        });
    }

    public override string EffectDescription => $"이동공격력을 {effects[0].dmg} 올립니다.";
}
