using UnityEngine;

public partial class Board
{
    // 이동 애니메이션(PieceMoveCor)이 재생되기 전에, 점유(논리) 상태부터 즉시 확정한다.
    // 카드 예약(다음 카드가 이 칸을 바로 타겟/이동 대상으로 참조) 기능이 성립하려면
    // 시각적 이동이 끝나기 전에도 GetButtonScript(to).GetPieceScript()가 최신값을 반환해야 한다.
    // 반환값(옮겨진 GameObject)은 호출부가 애니메이션 코루틴에 파라미터로 넘길 때 쓴다.
    GameObject ApplyMoveOccupancy(Button from, Button to)
    {
        GameObject piece = from.GetPiece();
        if (piece == null || from == to) return piece;

        to.SetPieceLogical(piece);
        from.RemovePiece();
        RelocateCasterIndicator(from, to);
        RelocatePieceDeckIndicator(from, to);

        if (piece.GetComponent<Piece>() is AutoPiece)
            UpdateAutoPiecePositionList(from.GetLocation(), to.GetLocation());

        return piece;
    }

    // 사망 판정 즉시(사망 연출을 기다리지 않고) 그 칸을 논리적으로 비운다.
    // dead로 대상 일치를 확인해, 죽은 기물이 아직 파괴되지 않은 상태에서 그 사이 다른 기물이
    // 같은 칸으로 이동해왔더라도 방금 도착한 기물을 실수로 지우지 않게 한다.
    void ClearDeadPieceOccupancy(Vector2Int pos, Piece dead)
    {
        if (dead == null) return;
        Button button = GetButtonScript(pos);
        if (button.GetPieceScript() == dead)
            button.RemovePiece();

        if (dead.teamID == 1) enemyPositions.Remove(pos);
        else if (dead is AutoPiece) autoAllyPositions.Remove(pos);
    }
}
