// MagicAttackCard의 작은 버전 — 사거리 안 기물 하나에게 이동공격력과 무관한 고정 피해를 주고 소멸한다.
// LoadMagicMissileCard가 전투 중 손에 넣어주는 카드이기도 하다.
public class MagicMissileCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "MagicMissileCard";
        Cost = 2;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.AnyPiece;
        exileOnUse = true;
        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.targeting,
            type = EffectType.Damage,
            dmg = 6,
            targetlogic = TargetLogic.NearestEnemy,
            effectRange = effectRange[0],
            animTrigger = "Attack",
            ignoreCasterColDamageBonus = true,
        });
    }

    public override string EffectDescription => $"기물에 {EffectiveDmg(effects[0])}의 고정 데미지를 줍니다. (소멸)";
}
