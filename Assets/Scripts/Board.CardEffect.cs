using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard;
    Vector2 lockedCaster = new Vector2(-1, -1);
    bool IsLockedCasterActive() => lockedCaster.x >= 0;

    public void UseCard(Card card)
    {
        lockedCaster = new Vector2(-1, -1);
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
            ScheduleNextCardEffect();
        }
        else if (IsLockedCasterActive())
        {
            // 이전 효과에서 선택된 캐릭터를 자동으로 다시 선택
            selectedButton = lockedCaster;
            GetButtonScript(selectedButton).SelectedTrue();
            // selectedButton setter가 OnButtonSelected를 발생시켜 OnSelectBoard()를 호출하므로
            // 범위 표시가 자동으로 업데이트됨
        }
    }

    void ScheduleNextCardEffect()
    {
        if (queuecoroutineworking)
            StartCoroutine(WaitThenProcessNext());
        else
            ProcessNextCardEffect();
    }

    IEnumerator WaitThenProcessNext()
    {
        yield return new WaitUntil(() => !queuecoroutineworking);
        ProcessNextCardEffect();
    }

    void ProcessEnemyCardEffect(CardEffect nextEffect)
    {
        if (nextEffect.requiredMode == BoardMode.command)
        {
            Vector2 targetPos = ResolveEnemyTarget(nextEffect);
            Debug.Log(targetPos);
            ExecuteEffect(pendingEffects.Dequeue(), targetPos);
            ScheduleNextCardEffect();
        }
        else if (nextEffect.requiredMode == BoardMode.targeting)
        {
            Vector2 targetPos = ResolveEnemyTargetingTarget(nextEffect);
            if (targetPos.x >= 0)
            {
                ExecuteEffect(pendingEffects.Dequeue(), targetPos);
                ScheduleNextCardEffect();
            }
            else
            {
                pendingEffects.Dequeue();
                ScheduleNextCardEffect();
            }
        }
    }

    Vector2 ResolveEnemyTarget(CardEffect effect)
    {
        switch (effect.targetlogic)
        {
            case TargetLogic.NearestEnemy:
                return ResolveNearestEnemyTarget();
            case TargetLogic.LowestHP:
                return ResolveLowestHPTarget(effect);
            default:
                return selectedButton;
        }
    }

    Vector2 ResolveEnemyTargetingTarget(CardEffect effect)
    {
        switch (effect.targetlogic)
        {
            case TargetLogic.self:
                return selectedButton;
            case TargetLogic.LowestHP:
                return ResolveLowestHPTarget(effect);
            case TargetLogic.AllEnemiesInRange:
            case TargetLogic.AllAlliesInRange:
                return selectedButton;
            default:
                return new Vector2(-1, -1);
        }
    }

    Vector2 ResolveLowestHPTarget(CardEffect effect)
    {
        if (effect.effectRange == null) return new Vector2(-1, -1);

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        int targetTeam = caster != null ? (caster.teamID == 0 ? 1 : 0) : 1;

        AddMovableButtons(effect.effectRange.GetAbleRange());

        int lowestHP = int.MaxValue;
        Vector2 target = new Vector2(-1, -1);

        foreach (Vector2 pos in selectedButtonMovable)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p != null && p.teamID == targetTeam && p.hp < lowestHP)
            {
                lowestHP = p.hp;
                target = pos;
            }
        }

        return target;
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
        // lockCasterForNext가 true이고 다음 효과가 있을 때만 시전자를 고정
        // Move 효과는 기물이 targetPos로 이동하므로 목적지를 저장, 나머지는 현재 위치 유지
        if (cardEffect.lockCasterForNext && pendingEffects.Count > 0)
            lockedCaster = cardEffect.type == EffectType.Move ? targetPos : selectedButton;

        currentActiveCard.cardCanvas.GetComponent<CardCanvas>().isCardEffecting = true;

        if (cardEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
            cardEffect.targetlogic == TargetLogic.AllAlliesInRange)
        {
            ExecuteAreaEffect(cardEffect, targetPos);
            return;
        }

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
            case EffectType.SelfDamage:
                SelfDamagePiece(selectedButton, cardEffect.dmg);
                break;
            case EffectType.Draw:
                CardCanvas.instance.DrawCard();
                break;
            default:
                Debug.LogError("효과 타입을 찾지 못했습니다");
                break;
        }
    }

    void ExecuteAreaEffect(CardEffect cardEffect, Vector2 center)
    {
        if (cardEffect.effectRange == null) return;

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        int casterTeam = caster != null ? caster.teamID : 0;
        int targetTeam = cardEffect.targetlogic == TargetLogic.AllEnemiesInRange
            ? (casterTeam == 0 ? 1 : 0)
            : casterTeam;
        var targets = new List<Vector2>();

        List<Vector2> offsets = cardEffect.effectRange.GetAbleRange();
        Vector2 actualCenter = center;

        if (cardEffect.areaTargetMode == AreaTargetMode.Directional4 ||
            cardEffect.areaTargetMode == AreaTargetMode.Directional8)
        {
            actualCenter = selectedButton;
            offsets = RotateOffsets(offsets, currentHoverDirection);
        }

        foreach (Vector2 offset in offsets)
        {
            Vector2 pos = actualCenter + offset;
            if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;

            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null || p.teamID != targetTeam) continue;
            targets.Add(pos);
        }

        switch (cardEffect.type)
        {
            case EffectType.Damage: AreaAttackPiece(selectedButton, targets, cardEffect.dmg); break;
            case EffectType.Shield:
                foreach (var pos in targets) ShieldPiece(selectedButton, pos, cardEffect.dmg);
                break;
            case EffectType.Heal:
                foreach (var pos in targets) HealPiece(selectedButton, pos, cardEffect.dmg);
                break;
        }
    }

    void ResetBoardAfterCardUse()
    {
        boardmode = BoardMode.Inspect;
        ClearHoverRange();
        if (IsLockedCasterActive())
            GetButtonScript(lockedCaster).SelectedFalse();
        lockedCaster = new Vector2(-1, -1);
        ClearSelectedButton();
        Debug.Log("Finished Card Use");
    }

    void FinishCardUsage()
    {
        if (currentActiveCard != null && currentActiveCard.blocksMovementAfterUse)
            playerMovedThisTurn = true;
        CardCanvas.instance.FinishUseCard();
        ResetBoardAfterCardUse();
    }
}
