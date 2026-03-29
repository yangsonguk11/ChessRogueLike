using System.Collections.Generic;
using UnityEngine;

public class Enemy : Piece
{
    Card c;
    public List<Card> enemyCards; // 적이 보유한 스킬/효과 리스트

    private void Start()
    {

    }
    // 간단한 AI 로직: 타겟 선정 및 효과 반환
    public virtual Card GetNextMove()
    {
        /*
        if (enemyCards == null || enemyCards.Count == 0) return (null, Vector2.zero);

        // 예시: 첫 번째 효과를 선택하고, 사거리 내 플레이어가 있다면 그 위치를 타겟으로 함
        Card c = enemyCards[0];
        c.user = User.Enemy;
        // 여기서 타겟(Vector2)을 찾는 로직을 구현합니다. 
        // 지금은 일단 현재 위치(제자리 사용) 혹은 특정 위치를 반환한다고 가정합니다.
        Vector2 targetPos = myPos + new Vector2(1, 0);
        */
        if (enemyCards == null || enemyCards.Count == 0) return null;

        // 단순하게 첫 번째 카드를 반환하거나, 랜덤/패턴에 따라 반환
        return enemyCards[0];
    }
}
