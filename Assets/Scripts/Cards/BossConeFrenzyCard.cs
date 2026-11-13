// Boss-Berserker 전용: BossFrenzyCard를 방향성(TowardTarget) 삼각형 광역으로 바꾼 카드.
// effectRange(BossTriangleRangeInfo)는 위쪽 기준 — 시전자 칸에서 맨해튼 거리 2 이내의 앞쪽 절반(시전자 줄 양옆 2칸씩,
// 앞 1줄 3칸, 맨 앞 1칸). 이 카드가 다음 행동으로 정해지는 순간(AutoPiece.PlanNextMove → Board.PlanFacing) 보스가 가장 가까운
// 아군을 바라보고, 범위는 그 방향(Piece.facing)대로 펼쳐지며 예고도 같은 방향으로 보인다. LocksFacing이라 행동할 때까지
// 피격으로 방향이 바뀌지 않는다. 피해·힘 비례 규칙은 BossFrenzyCard와 같고, Boss-Colossus는 기존 BossFrenzyCard(고정 범위)를 계속 쓴다.
public class BossConeFrenzyCard : BossFrenzyCard
{
    public override bool LocksFacing => true;

    public override void Awake()
    {
        base.Awake();
        Name = "BossConeFrenzyCard";
        effects[0] = effects[0] with { facingMode = FacingMode.TowardTarget };
    }

    public override string EffectDescription =>
        $"한 방향의 삼각형 범위 안 모든 적에게 {EffectiveDmg(effects[0])} 피해를 줍니다. 힘이 오른 만큼 강해집니다.";
}
