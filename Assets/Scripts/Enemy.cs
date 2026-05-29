using System.Collections.Generic;
using UnityEngine;

public class Enemy : Piece
{
    Card c;
    public List<Card> enemyCards; // ���� ������ ��ų/ȿ�� ����Ʈ

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
        Card move = GetNextMove();
        if (move == null || move.effects.Count == 0) return base.GetMoveableButton();
        if (move.effects[0].type == EffectType.Move)
            return base.GetMoveableButton();
        return move.effects[0].effectRange?.GetAbleRange() ?? base.GetMoveableButton();
    }

    // ������ AI ����: Ÿ�� ���� �� ȿ�� ��ȯ
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
