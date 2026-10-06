using System.Collections.Generic;
using UnityEngine;

// 상점 이벤트 레벨에서 보드에 놓이는 오브젝트. Inspect 모드에서 클릭하면 ShopCanvas가 열린다
// (Board.InputHandler). 진열·구매·카드 제거 로직은 전부 ShopCanvas에 있고, 이 기물은 클릭 대상 역할만 한다.
public class ShopObject : Piece
{
    public override void Awake()
    {
        base.Awake();
        hp = maxhp = 1;
    }

    public override List<Vector2Int> GetMoveableButton() => new List<Vector2Int>();
}
