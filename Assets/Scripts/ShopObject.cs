using System.Collections.Generic;
using UnityEngine;

// 상점 이벤트 레벨에서 보드에 놓이는 오브젝트. RestObject와 동일한 역할이지만
// 무엇을 파는지는 아직 정해지지 않아 스켈레톤만 있다.
public class ShopObject : Piece
{
    public override void Awake()
    {
        base.Awake();
        hp = maxhp = 1;
    }

    public override List<Vector2Int> GetMoveableButton() => new List<Vector2Int>();
}
