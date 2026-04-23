using System.Collections.Generic;
using UnityEngine;

public class Enemy : Piece
{
    Card c;
    public List<Card> enemyCards; // 적이 보유한 스킬/효과 리스트

    int Movenum;

    Card nextMove;
    public override void Awake()
    {
        base.Awake();
        Movenum = 0;
        nextMove = enemyCards[0];
        ActionText();
        GameManager.instance.AddEnemy(gameObject);
    }

    public override List<Vector2> GetMoveableButton() {
        if (GetNextMove().effects[0].type == EffectType.Move)
            return base.GetMoveableButton();
        else
        {
            return GetNextMove().effects[0].effectRange.GetAbleRange();
        }
    }

    // 간단한 AI 로직: 타겟 선정 및 효과 반환
    public virtual Card GetNextMove()
    {
        if (enemyCards == null || enemyCards.Count == 0) return null;
        return nextMove;
    }

    public Card ChangeMove()
    {
        Movenum++;
        if (Movenum >= enemyCards.Count)
            Movenum = 0;
        nextMove = enemyCards[Movenum];
        return nextMove;
    }
    public override void ActionText()
    {
        pieceCanvas.ShowActionText(GetNextMove().Name);
    }

    private void OnDestroy()
    {
        GameManager.instance.RemoveEnemy(gameObject);
    }
}
