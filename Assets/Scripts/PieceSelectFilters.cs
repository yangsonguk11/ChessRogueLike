using UnityEngine;

// Board.RequestPieceSelection에 넘기는 칸 선택 조건 하나. pos는 필터 대상 기물이 있는 보드 좌표.
public delegate bool PieceSelectFilter(Vector2 pos, Piece piece);

// RequestPieceSelection에 여러 개를 params로 넘기면 전부(AND)를 만족하는 칸만 선택 가능해진다.
// 필요한 조건이 늘어나면 이 클래스에 팩토리 메서드를 추가하기만 하면 됨.
public static class PieceSelectFilters
{
    // teamID가 일치하는 기물만 (아군은 0)
    public static PieceSelectFilter Team(int teamID) => (pos, piece) => piece.teamID == teamID;

    // center로부터 체스판 거리(대각선 포함, king-move 기준)로 range칸 이내인 기물만
    public static PieceSelectFilter WithinRange(Vector2 center, int range) => (pos, piece) =>
        Mathf.Max(Mathf.Abs(pos.x - center.x), Mathf.Abs(pos.y - center.y)) <= range;

    // 지정한 기물 하나를 선택 대상에서 제외 (예: 시전자 자신)
    public static PieceSelectFilter ExcludePiece(Piece excluded) => (pos, piece) => piece != excluded;
}
