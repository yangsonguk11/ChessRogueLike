using System.Collections.Generic;
using UnityEngine;

// 휴식 이벤트 레벨에서 보드에 놓이는 오브젝트. 전투 대상이 되지 않도록 pieceInfo의 teamID를 0/1과 다르게(예: 2) 설정해둔다.
public class RestObject : Piece
{
    // 휴식을 한 번 사용했는지 여부. true가 되면 휴식 버튼이 더 이상 뜨지 않는다.
    public bool used;

    public override void Awake()
    {
        base.Awake();
        hp = maxhp = 1;
    }

    public override List<Vector2> GetMoveableButton() => new List<Vector2>();
}
