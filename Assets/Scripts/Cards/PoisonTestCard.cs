// 검증 전용 카드: StatusEffectType.Poison을 실제로 부여하는 카드가 아직 없어서
// 독 틱 텍스트 합산(Piece.ProcessStatusEffects)을 플레이테스트하기 위해 추가했다.
// 정식 카드로 유지할지는 플레이테스트 후 결정.
public class PoisonTestCard : Card
{
    public override void Awake()
    {
        base.Awake();
        Name = "PoisonTestCard";
        Cost = 1;
        type = CardType.Skill;
        dragDropTarget = DragDropTarget.Enemy;
        effects.Add(new CardEffect(
            Board.BoardMode.targeting,
            EffectType.ApplyStatus,
            0,
            TargetLogic.NearestEnemy,
            _statusEffectType: StatusEffectType.Poison,
            _statusDuration: 2,
            _statusPower: 2
        )
        {
            hasCaster = false
        });
    }

    public override string EffectDescription =>
        $"적을 선택해 {effects[0].statusDuration}턴간 매 턴 {effects[0].statusPower}의 독 피해를 입힙니다.";
}
