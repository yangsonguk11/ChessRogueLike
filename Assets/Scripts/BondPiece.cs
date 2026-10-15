using UnityEngine;

// 결속 기물(L2-Bond): 같은 팀 기물이 쓰러질 때마다(Board.ClearDeadPieceOccupancy → OnTeammateDied) 체력을 모두 회복하고
// 힘이 colDamageGain만큼 오른다. 표시·디스펠 대상으로 영구 버프 BondEffect("결속")를 지니며, 디스펠로 지워지면 더는 강화되지 않는다.
// 강화는 사망 판정 즉시 적용되고(같은 턴의 다음 행동·예고에 바로 반영), 텍스트·회복 연출은 보드 연출 순서에 맞춰 재생된다.
public class BondPiece : AutoPiece
{
    public int colDamageGain = 2;

    public override void Awake()
    {
        base.Awake();
        AddStatusEffect(new BondEffect());
    }

    public override void OnTeammateDied(int livingTeammates)
    {
        if (hp <= 0 || !activeEffects.Exists(e => e is BondEffect)) return;

        int healed = GetHeal(maxhp - hp);
        AddColDamage(colDamageGain, showReaction: false);
        ActionText(); // 공격 예고 숫자에 오른 힘 반영

        Board board = Board.instance;
        if (board == null) return;
        board.EnqueueBoardAnimation(StatusTextReaction("결속", true, new Color(1f, 0.55f, 0.2f)));
        if (healed > 0) board.EnqueueBoardAnimation(HealText(healed));
        board.EnqueueBoardAnimation(ColDamageUpReaction(colDamageGain));
    }
}
