// 적 전용(스케일 강요 보스): 자신을 healAmount만큼 회복한다(최대 체력은 넘지 않음).
public class BossRegenerateCard : Card
{
    public int healAmount = 15;

    public override void Awake()
    {
        base.Awake();
        Name = "BossRegenerateCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;
        dragDropTarget = DragDropTarget.Self;

        // BoardMode.command + TargetLogic.self → ResolveEnemyTarget이 selectedButton(보스 자신)을 반환
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Heal,
            dmg = healAmount,
            targetlogic = TargetLogic.self,
            animTrigger = "Heal",
        });
    }

    public override string EffectDescription => $"자신을 {effects[0].dmg} 회복합니다.";
}
