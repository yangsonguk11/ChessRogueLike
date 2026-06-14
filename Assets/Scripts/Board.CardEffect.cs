using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard;
    Vector2 lockedCaster = new Vector2(-1, -1);
    Piece lockedCasterPiece = null;
    bool IsLockedCasterActive() => lockedCaster.x >= 0;
    bool effectApplied = false;
    public bool EffectApplied => effectApplied;

    public void UseCard(Card card)
    {
        if (card.user == User.Ally)
            ClearSelectedButton();
        lockedCaster = new Vector2(-1, -1);
        effectApplied = false;
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

        if (boardmode == BoardMode.cardSelecting)
        {
            CardEffect effect = pendingEffects.Dequeue();
            CardCanvas.instance.ShowCardSelectionPanel(
                effect.cardZone,
                effect.selectCount,
                effect,
                (selected) => ApplyCardSelectionEffect(effect, selected));
            return;
        }

        if (boardmode != BoardMode.command && boardmode != BoardMode.targeting)
        {
            ExecuteEffect(pendingEffects.Dequeue());
            ScheduleNextCardEffect();
        }
        else if (IsLockedCasterActive())
        {
            if (lockedCasterPiece != null && boardmode == BoardMode.targeting)
            {
                ExecuteEffect(pendingEffects.Dequeue(), lockedCasterPiece);
                ScheduleNextCardEffect();
            }
            else
            {
                selectedButton = lockedCaster;
                if (isSelectedButtonActive())
                    GetButtonScript(selectedButton).SelectedTrue();
            }
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
            Vector2 targetPos;
            if (nextEffect.areaTargetMode == AreaTargetMode.Directional4 ||
                nextEffect.areaTargetMode == AreaTargetMode.Directional8)
                targetPos = ResolveEnemyDirectionalTarget(nextEffect);
            else
                targetPos = ResolveEnemyTargetingTarget(nextEffect);

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

    Vector2 ResolveEnemyDirectionalTarget(CardEffect effect)
    {
        if (effect.effectRange == null) return new Vector2(-1, -1);

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        if (caster == null) return new Vector2(-1, -1);

        int targetTeam = effect.targetlogic == TargetLogic.AllEnemiesInRange
            ? (caster.teamID == 0 ? 1 : 0)
            : caster.teamID;

        bool eightDir = effect.areaTargetMode == AreaTargetMode.Directional8;
        Vector2[] directions = eightDir
            ? new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left,
                      new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) }
            : new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

        List<Vector2> offsets = effect.effectRange.GetAbleRange();
        Vector2 bestDir = new Vector2(-1, -1);
        int bestCount = 0;

        foreach (Vector2 dir in directions)
        {
            int count = 0;
            foreach (Vector2 offset in RotateOffsets(offsets, dir))
            {
                Vector2 pos = selectedButton + offset;
                if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p != null && p.teamID == targetTeam) count++;
            }
            if (count > bestCount) { bestCount = count; bestDir = dir; }
        }

        if (bestCount == 0) return new Vector2(-1, -1); // 어느 방향에도 대상 없음 → 스킵

        currentHoverDirection = bestDir;
        return selectedButton; // Directional 모드는 시전자 위치를 중심으로 사용
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
        List<Vector2> movableRange = GetButtonScript(selectedButton).GetPiece()?.GetComponent<Piece>().GetMoveableButton()
            ?? new List<Vector2>();
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
        {
            lockedCaster = cardEffect.type == EffectType.Move ? targetPos : selectedButton;
            lockedCasterPiece = GetButtonScript(lockedCaster).GetPieceScript();
        }

        effectApplied = true;
        CardCanvas.instance.isCardEffecting = true;

        if (cardEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
            cardEffect.targetlogic == TargetLogic.AllAlliesInRange)
        {
            ExecuteAreaEffect(cardEffect, targetPos);
            return;
        }

        int resolvedDmg = cardEffect.useColDamageAsDmg
            ? (GetButtonScript(selectedButton).GetPieceScript()?.colDamage ?? cardEffect.dmg)
            : cardEffect.dmg;

        switch (cardEffect.type)
        {
            case EffectType.Move:
                MovePiece(selectedButton, targetPos, cardEffect);
                break;
            case EffectType.Damage:
                AttackPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                ApplyStatusToTarget(targetPos, cardEffect);
                break;
            case EffectType.Heal:
                HealPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                ApplyStatusToTarget(targetPos, cardEffect);
                break;
            case EffectType.Shield:
                ShieldPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                ApplyStatusToTarget(targetPos, cardEffect);
                break;
            case EffectType.SelfDamage:
                SelfDamagePiece(selectedButton, cardEffect.dmg);
                break;
            case EffectType.Draw:
                CardCanvas.instance.DrawCard();
                break;
            case EffectType.ApplyStatus:
                ApplyStatusToTarget(targetPos, cardEffect);
                if (targetPos.x >= 0 && targetPos.y >= 0)
                    GetButtonScript(targetPos).GetPieceScript()?.TriggerAnim("ApplyStatus");
                break;
            case EffectType.ApplyTurnEffect:
                ApplyTurnEffectToTarget(targetPos, cardEffect);
                break;
            case EffectType.ColDamageUp:
            {
                Piece p = GetButtonScript(targetPos).GetPieceScript();
                if (p != null)
                {
                    p.colDamage += cardEffect.dmg;
                    p.TriggerAnim("Buff");
                }
                break;
            }
            case EffectType.DiscardHand:
                CardCanvas.instance.HandtoDiscardCount(cardEffect.dmg);
                break;
            case EffectType.ShuffleHandToDeck:
                CardCanvas.instance.HandtoDeckCount(cardEffect.dmg);
                break;
            case EffectType.ExileHand:
                CardCanvas.instance.HandtoExileCount(cardEffect.dmg);
                break;
            case EffectType.HandToDeckTop:
                CardCanvas.instance.HandtoDeckTop(cardEffect.dmg);
                break;
            default:
                Debug.LogError("효과 타입을 찾지 못했습니다");
                break;
        }
    }

    void ApplyTurnEffectToTarget(Vector2 targetPos, CardEffect cardEffect)
    {
        if (cardEffect.onTurnEndEffect == null) return;
        Piece target = GetButtonScript(targetPos).GetPieceScript();
        if (target == null) return;
        target.AddStatusEffect(new TurnEffect(cardEffect.turnPhase, cardEffect.onTurnEndEffect, cardEffect.turnDuration));
    }

    void ApplyStatusToTarget(Vector2 targetPos, CardEffect cardEffect)
    {
        if (cardEffect.statusEffectType == StatusEffectType.None) return;
        if (targetPos.x < 0 || targetPos.y < 0) return;
        Piece target = GetButtonScript(targetPos).GetPieceScript();
        if (target == null) return;
        StatusEffect effect = CreateStatusEffect(cardEffect.statusEffectType, cardEffect.statusDuration, cardEffect.statusPower,
            cardEffect.effectRange, cardEffect.targetlogic);
        if (effect != null)
            target.AddStatusEffect(effect);
    }

    void ApplyStatusToTargets(List<Vector2> targets, CardEffect cardEffect)
    {
        if (cardEffect.statusEffectType == StatusEffectType.None) return;
        foreach (Vector2 pos in targets)
        {
            Piece target = GetButtonScript(pos).GetPieceScript();
            if (target == null) continue;
            StatusEffect effect = CreateStatusEffect(cardEffect.statusEffectType, cardEffect.statusDuration, cardEffect.statusPower,
                cardEffect.effectRange, cardEffect.targetlogic);
            if (effect != null)
                target.AddStatusEffect(effect);
        }
    }

    StatusEffect CreateStatusEffect(StatusEffectType type, int duration, int power,
        RangeInfoSO range = null, TargetLogic targetLogic = TargetLogic.AllEnemiesInRange)
    {
        return type switch
        {
            StatusEffectType.Poison             => new PoisonEffect(duration, power),
            StatusEffectType.Burning            => new BurningEffect(duration, power),
            StatusEffectType.Regen              => new RegenEffect(duration, power),
            StatusEffectType.Stun               => new StunEffect(duration),
            StatusEffectType.Strengthen         => new StrengthenEffect(duration, power),
            StatusEffectType.Weaken             => new WeakenEffect(duration, power),
            StatusEffectType.TurnDamageStart    => new TurnEffect(TurnPhase.OwnTurnStart,
                new CardEffect(BoardMode.Inspect, EffectType.Damage, power, TargetLogic.self), duration),
            StatusEffectType.TurnDamageEnd      => new TurnEffect(TurnPhase.OwnTurnEnd,
                new CardEffect(BoardMode.Inspect, EffectType.Damage, power, TargetLogic.self), duration),
            StatusEffectType.TurnAoEDamageStart => new TurnEffect(TurnPhase.OwnTurnStart,
                new CardEffect(BoardMode.Inspect, EffectType.Damage, power, TargetLogic.AllEnemiesInRange, range), duration),
            StatusEffectType.TurnAoEDamageEnd   => new TurnEffect(TurnPhase.OwnTurnEnd,
                new CardEffect(BoardMode.Inspect, EffectType.Damage, power, TargetLogic.AllEnemiesInRange, range), duration),
            StatusEffectType.Thorn              => new ThornEffect(duration, power),
            _                                   => null,
        };
    }

    void ExecuteEffect(CardEffect cardEffect, Piece target)
    {
        if (cardEffect.lockCasterForNext && pendingEffects.Count > 0)
            lockedCasterPiece = target;

        effectApplied = true;
        CardCanvas.instance.isCardEffecting = true;

        switch (cardEffect.type)
        {
            case EffectType.Shield:
            {
                Vector2 pos = FindPiecePos(target);
                if (pos.x >= 0) ShieldPiece(pos, pos, cardEffect.dmg, cardEffect);
                break;
            }
            case EffectType.Heal:
            {
                Vector2 pos = FindPiecePos(target);
                if (pos.x >= 0) HealPiece(pos, pos, cardEffect.dmg, cardEffect);
                break;
            }
            case EffectType.ApplyTurnEffect:
                if (cardEffect.onTurnEndEffect != null)
                    target.AddStatusEffect(new TurnEffect(cardEffect.turnPhase, cardEffect.onTurnEndEffect, cardEffect.turnDuration));
                break;
            case EffectType.ApplyStatus:
            {
                StatusEffect effect = CreateStatusEffect(cardEffect.statusEffectType, cardEffect.statusDuration, cardEffect.statusPower,
                    cardEffect.effectRange, cardEffect.targetlogic);
                if (effect != null) target.AddStatusEffect(effect);
                target.TriggerAnim("ApplyStatus");
                break;
            }
            case EffectType.Draw:
                CardCanvas.instance.DrawCard();
                break;
            case EffectType.ColDamageUp:
                target.colDamage += cardEffect.dmg;
                target.TriggerAnim("Buff");
                break;
            default:
                Debug.LogWarning($"ExecuteEffect(Piece): 지원하지 않는 효과 타입 {cardEffect.type}");
                break;
        }
    }

    Vector2 FindPiecePos(Piece piece)
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Vector2 pos = new Vector2(x, y);
                if (GetButtonScript(pos).GetPieceScript() == piece)
                    return pos;
            }
        return new Vector2(-1, -1);
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

        if (cardEffect.areaTargetMode == AreaTargetMode.Fixed)
        {
            actualCenter = selectedButton; // 고정 범위는 항상 시전자 중심
        }
        else if (cardEffect.areaTargetMode == AreaTargetMode.Directional4 ||
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
            case EffectType.Damage:
                AreaAttackPiece(selectedButton, targets, cardEffect.dmg, cardEffect);
                ApplyStatusToTargets(targets, cardEffect);
                break;
            case EffectType.Shield:
                AreaShieldPiece(targets, cardEffect.dmg, cardEffect);
                ApplyStatusToTargets(targets, cardEffect);
                break;
            case EffectType.Heal:
                AreaHealPiece(targets, cardEffect.dmg, cardEffect);
                ApplyStatusToTargets(targets, cardEffect);
                break;
            case EffectType.ApplyStatus:
                ApplyStatusToTargets(targets, cardEffect);
                break;
        }
    }

    public void CancelCardUsage()
    {
        pendingEffects.Clear();
        currentActiveCard = null;
        ResetBoardAfterCardUse();
    }

    void ResetBoardAfterCardUse()
    {
        boardmode = BoardMode.Inspect;
        ClearHoverRange();
        if (IsLockedCasterActive())
            GetButtonScript(lockedCaster).SelectedFalse();
        lockedCaster = new Vector2(-1, -1);
        lockedCasterPiece = null;
        ClearSelectedButton();
    }

    void FinishCardUsage()
    {
        if (currentActiveCard != null && currentActiveCard.blocksMovementAfterUse)
            playerMovedThisTurn = true;
        CardCanvas.instance.FinishUseCard();
        ResetBoardAfterCardUse();
    }

    // 카드 선택 패널에서 플레이어가 선택을 확정한 후 호출됨
    void ApplyCardSelectionEffect(CardEffect effect, List<RectTransform> selected)
    {
        switch (effect.type)
        {
            case EffectType.SelectAndDiscard:
                foreach (var card in selected)
                    CardCanvas.instance.MoveCardToDiscard(card);
                break;
            case EffectType.SelectAndChangeCost:
                foreach (var card in selected)
                {
                    var c = card.GetComponent<Card>();
                    if (c == null) continue;
                    if (c.originalCost < 0) c.originalCost = c.Cost;
                    c.Cost = Mathf.Max(0, c.Cost + effect.costChange);
                    c.costDuration = effect.costDuration;
                    c.RefreshView();
                }
                break;
            case EffectType.SelectAndReturnToDeck:
                foreach (var card in selected)
                    CardCanvas.instance.MoveCardToDeck(card);
                break;
        }
        ScheduleNextCardEffect();
    }
}
