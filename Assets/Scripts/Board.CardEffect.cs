using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard;

    public void UseCard(Card card)
    {
        currentActiveCard = card;
        pendingEffects.Clear();
        foreach (var effect in card.effects)
            pendingEffects.Enqueue(effect);
        ProcessNextCardEffect();
    }

    void ProcessNextCardEffect()
    {
        if (pendingEffects.Count == 0)
        {
            FinishCardUsage();
            return;
        }

        CardEffect nextEffect = pendingEffects.Peek();

        if (currentActiveCard.user == User.Enemy)
        {
            ProcessEnemyCardEffect(nextEffect);
            return;
        }

        boardmode = nextEffect.requiredMode;

        if (boardmode != BoardMode.command && boardmode != BoardMode.targeting)
        {
            ExecuteEffect(pendingEffects.Dequeue());
            ProcessNextCardEffect();
        }
    }

    void ProcessEnemyCardEffect(CardEffect nextEffect)
    {
        if (nextEffect.requiredMode == BoardMode.command)
        {
            Vector2 targetPos = ResolveEnemyTarget(nextEffect);
            Debug.Log(targetPos);
            ExecuteEffect(pendingEffects.Dequeue(), targetPos);
            ProcessNextCardEffect();
        }
        // targeting mode for enemies not yet implemented
    }

    Vector2 ResolveEnemyTarget(CardEffect effect)
    {
        switch (effect.targetlogic)
        {
            case TargetLogic.NearestEnemy:
                return ResolveNearestEnemyTarget();
            default:
                return selectedButton;
        }
    }

    Vector2 ResolveNearestEnemyTarget()
    {
        List<Vector2> movableRange = GetButtonScript(selectedButton).GetPiece()?.GetComponent<Piece>().GetMoveableButton();
        AddMovableButtons(movableRange);

        float minDistance = float.MaxValue;
        Vector2 bestTargetPos = new Vector2(-1, -1);

        foreach (Vector2 movablePos in selectedButtonMovable)
        {
            Piece p = GetButtonScript(movablePos).GetPiece()?.GetComponent<Piece>();
            if (p != null && p.teamID == 0)
            {
                float dist = Vector2.Distance(selectedButton, movablePos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestTargetPos = movablePos;
                }
            }
        }

        if (bestTargetPos != new Vector2(-1, -1))
            return bestTargetPos;

        // 범위 내 플레이어 없음: 가장 가까운 플레이어 방향으로 이동
        Vector2 globalNearestPlayer = GetNearestPlayerPos(selectedButton);
        float minMoveDist = float.MaxValue;
        Vector2 bestMovePos = selectedButton;

        foreach (Vector2 movablePos in selectedButtonMovable)
        {
            float dist = Vector2.Distance(movablePos, globalNearestPlayer);
            if (dist < minMoveDist)
            {
                minMoveDist = dist;
                bestMovePos = movablePos;
            }
        }

        return bestMovePos;
    }

    void ExecuteEffect(CardEffect cardEffect, Vector2 targetPos = default)
    {
        currentActiveCard.cardCanvas.GetComponent<CardCanvas>().isCardEffecting = true;
        switch (cardEffect.type)
        {
            case EffectType.Move:
                MovePiece(selectedButton, targetPos);
                break;
            case EffectType.Damage:
                AttackPiece(selectedButton, targetPos, cardEffect.dmg);
                break;
            case EffectType.Heal:
                HealPiece(selectedButton, targetPos, cardEffect.dmg);
                break;
            case EffectType.Shield:
                ShieldPiece(selectedButton, targetPos, cardEffect.dmg);
                break;
            default:
                Debug.LogError("효과 타입을 찾지 못했습니다");
                break;
        }
    }

    void ResetBoardAfterCardUse()
    {
        boardmode = BoardMode.Inspect;
        ClearSelectedButton();
        Debug.Log("Finished Card Use");
    }

    void FinishCardUsage()
    {
        CardCanvas.instance.FinishUseCard();
        ResetBoardAfterCardUse();
    }
}
