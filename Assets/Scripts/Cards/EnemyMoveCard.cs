// MoveCard와 효과가 완전히 동일한, 적 전용 카드. prefab이 이 타입을 직접 참조하므로
// 별도 클래스는 유지하되, Name만 다르게 설정.
public class EnemyMoveCard : MoveCard
{
    public override void Awake()
    {
        base.Awake();
        Name = "EnemyMoveCard";
    }
}
