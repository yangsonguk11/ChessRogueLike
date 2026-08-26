using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard;
    Vector2Int lockedCaster = new Vector2Int(-1, -1);
    Piece lockedCasterPiece = null;
    bool IsLockedCasterActive() => lockedCaster.x >= 0;
    bool effectApplied = false;
    public bool EffectApplied => effectApplied;

    public void UseCard(Card card)
    {
        Debug.Log($"UseCard: {card.name} (user: {card.user}, effects: {card.effects.Count})");
        if (card.user == User.Ally)
        {
            ClearSelectedButton();
            ShowUseEligibilityPreview(card);
        }
        lockedCaster = new Vector2Int(-1, -1);
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
            if (nextEffect.pieceSelectCount > 0)
            {
                CardEffect effect = pendingEffects.Dequeue();
                RequestPieceSelection(
                    effect.pieceSelectCount,
                    (selected) => ApplyPieceSelectionEffect(effect, selected),
                    PieceSelectFilters.Team(0));
                return;
            }

            ExecuteEffect(pendingEffects.Dequeue());
            ScheduleNextCardEffect();
        }
        else if (IsLockedCasterActive())
        {
            if (lockedCasterPiece != null && boardmode == BoardMode.targeting)
            {
                ExecuteEffect(pendingEffects.Dequeue(), lockedCaster);
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

    // 카드를 실제로 드롭(놓음)한 시점에 CardCanvas.OnDragCardReleased가 호출: 캐스터 선택이 필요한
    // 카드라면(원래 여기서 플레이어가 자기 기물을 클릭해야 했던 것을) 카드를 낸 기물로 바로 확정한다.
    // self 타겟 효과는 이 호출 안에서 OnSelectBoard를 통해 곧바로 실행될 수 있다 — 호출부는 그 후
    // nowusingCard가 비었는지 확인해서 카드 사용이 이미 끝났는지 판단해야 한다.
    public void ConfirmCasterOnDrop()
    {
        if (pendingEffects.Count == 0 || currentActiveCard == null || currentActiveCard.user != User.Ally) return;
        if (isSelectedButtonActive() || IsLockedCasterActive()) return;

        CardEffect nextEffect = pendingEffects.Peek();
        if (boardmode == BoardMode.command || (boardmode == BoardMode.targeting && nextEffect.hasCaster))
            AutoSelectCardOwnerAsCaster();
    }

    // CardCanvas에 표시 중인(카드를 낸) 기물을 캐스터로 즉시 선택 처리한다.
    // Board.InputHandler.cs의 "아군 기물을 클릭해 캐스터로 선택" 로직과 동일한 결과를 만든다.
    void AutoSelectCardOwnerAsCaster()
    {
        Piece owner = CardCanvas.instance != null ? CardCanvas.instance.ActivePiece : null;
        Button ownerButton = GetButtonForPiece(owner);
        if (ownerButton == null) return;

        selectedButton = ownerButton.GetLocation();
        // 위 대입이 self 타겟 효과의 즉시 실행(OnSelectBoard)까지 그 자리에서 끝내버렸을 수 있어
        // selectedButton이 이미 초기화(-1,-1)됐을 수 있다 — 그 상태로 GetButtonScript를 호출하면 안 된다.
        if (isSelectedButtonActive())
            GetButtonScript(selectedButton).SelectedTrue();
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
        // 이전 효과(예: Move)가 lockCasterForNext를 설정해뒀다면, 시전자가 이동한 새 위치를
        // 기준으로 다음 효과(예: 자기 자신 버프)를 풀이해야 함
        if (IsLockedCasterActive())
            _selectedButton = lockedCaster;

        if (nextEffect.requiredMode == BoardMode.command)
        {
            Vector2Int targetPos = ResolveEnemyTarget(nextEffect);
            ExecuteEffect(pendingEffects.Dequeue(), targetPos);
            ScheduleNextCardEffect();
        }
        else if (nextEffect.requiredMode == BoardMode.targeting)
        {
            Vector2Int targetPos;
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

    Vector2Int ResolveEnemyTarget(CardEffect effect)
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

    Vector2Int ResolveEnemyTargetingTarget(CardEffect effect)
    {
        switch (effect.targetlogic)
        {
            case TargetLogic.self:
                return selectedButton;
            case TargetLogic.LowestHP:
                return ResolveLowestHPTarget(effect);
            case TargetLogic.AllEnemiesInRange:
            case TargetLogic.AllAlliesInRange:
            case TargetLogic.AllPiecesInRange:
                return selectedButton;
            default:
                return new Vector2Int(-1, -1);
        }
    }

    Vector2Int ResolveEnemyDirectionalTarget(CardEffect effect)
    {
        if (effect.effectRange == null) return new Vector2Int(-1, -1);

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        if (caster == null) return new Vector2Int(-1, -1);

        int targetTeam = effect.targetlogic == TargetLogic.AllEnemiesInRange
            ? (caster.teamID == 0 ? 1 : 0)
            : caster.teamID;

        bool eightDir = effect.areaTargetMode == AreaTargetMode.Directional8;
        Vector2Int[] directions = eightDir
            ? new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
                      new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1) }
            : new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        List<Vector2Int> offsets = effect.effectRange.GetAbleRange();
        Vector2Int bestDir = new Vector2Int(-1, -1);
        int bestCount = 0;

        foreach (Vector2Int dir in directions)
        {
            int count = 0;
            foreach (Vector2Int offset in RotateOffsets(offsets, dir))
            {
                Vector2Int pos = selectedButton + offset;
                if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p != null && p.teamID == targetTeam) count++;
            }
            if (count > bestCount) { bestCount = count; bestDir = dir; }
        }

        if (bestCount == 0) return new Vector2Int(-1, -1); // 어느 방향에도 대상 없음 → 스킵

        currentHoverDirection = bestDir;
        return selectedButton; // Directional 모드는 시전자 위치를 중심으로 사용
    }

    Vector2Int ResolveLowestHPTarget(CardEffect effect)
    {
        if (effect.effectRange == null) return new Vector2Int(-1, -1);

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        int targetTeam = caster != null ? (caster.teamID == 0 ? 1 : 0) : 1;

        AddMovableButtons(selectedButton, effect.effectRange.GetAbleRange());

        int lowestHP = int.MaxValue;
        Vector2Int target = new Vector2Int(-1, -1);

        foreach (Vector2Int pos in selectedButtonMovable)
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

    Vector2Int ResolveNearestEnemyTarget()
    {
        List<Vector2Int> movableRange = GetButtonScript(selectedButton).GetPiece()?.GetComponent<Piece>().GetMoveableButton()
            ?? new List<Vector2Int>();
        AddMovableButtons(selectedButton, movableRange);

        float minDistance = float.MaxValue;
        Vector2Int bestTargetPos = new Vector2Int(-1, -1);

        foreach (Vector2Int movablePos in selectedButtonMovable)
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

        if (bestTargetPos != new Vector2Int(-1, -1))
            return bestTargetPos;

        // 범위 내 플레이어 없음: 가장 가까운 플레이어 방향으로 이동
        Vector2Int globalNearestPlayer = GetNearestPlayerPos(selectedButton);
        float minMoveDist = float.MaxValue;
        Vector2Int bestMovePos = selectedButton;

        foreach (Vector2Int movablePos in selectedButtonMovable)
        {
            if (GetPieceAt(movablePos) != null) continue; // 다른 기물이 있는 칸은 이동 후보에서 제외

            float dist = Vector2.Distance(movablePos, globalNearestPlayer);
            if (dist < minMoveDist)
            {
                minMoveDist = dist;
                bestMovePos = movablePos;
            }
        }

        return bestMovePos;
    }

    // useColDamageAsDmg면 이동공격력 전체 수치를 그대로 사용(이동공격 충돌 데미지와 동일 기준),
    // Damage 타입이면 dmg에 콜대미지 Delta(영구 강화분 + 이번 전투 임시 버프분)를 가산
    int ResolveDamageWithColDamage(CardEffect cardEffect, Piece caster)
    {
        if (cardEffect.useColDamageAsDmg)
            return Mathf.Max(0, caster?.colDamage ?? 0);

        int casterColDmg = caster?.ColDamageDelta ?? 0;
        int result = cardEffect.type == EffectType.Damage ? cardEffect.dmg + casterColDmg : cardEffect.dmg;
        return Mathf.Max(0, result);
    }

    // Shield 타입이면 dmg에 시전자의 shieldBonus를 가산 (colDamage와 같은 구조의 별개 스탯)
    int ResolveShieldWithBonus(CardEffect cardEffect, Piece caster)
    {
        int casterShieldBonus = caster?.ShieldBonusDelta ?? 0;
        int result = cardEffect.type == EffectType.Shield ? cardEffect.dmg + casterShieldBonus : cardEffect.dmg;
        return Mathf.Max(0, result);
    }

    void ExecuteEffect(CardEffect cardEffect, Vector2Int targetPos = default)
    {
        // lockCasterForNext가 true이고 다음 효과가 있을 때만 시전자를 고정
        // Move 효과는 기물이 targetPos로 이동하므로 목적지를 저장, 나머지는 현재 위치 유지
        if (cardEffect.lockCasterForNext && pendingEffects.Count > 0)
        {
            lockedCaster = cardEffect.type == EffectType.Move ? targetPos : selectedButton;
            lockedCasterPiece = GetButtonScript(lockedCaster).GetPieceScript();
        }

        // effectApplied가 false→true로 바뀌는 지금 이 순간이 이 카드의 첫 효과가 실제로 처리되기 시작하는
        // 시점이다(그 전까지는 CardCanvas.CancelCardUsage/RevertNowUsingCardToHeld로 언제든 취소 가능하고,
        // 이 지점부턴 취소가 막힌다 — EffectApplied 프로퍼티 참고). 카드 하나당 딱 한 번만 발동해야 하므로
        // 아직 false일 때만(=이 카드의 첫 효과일 때만) 호출한다. relicsOnCardUsed를 전부 큐에 모아
        // 동기적으로 순차 처리하고 나서(TriggerRelicsOnCardUsed 내부), 곧바로 아래에서 카드 자신의
        // 첫 효과(cardEffect)로 자연스럽게 이어진다 — 유물 효과 전부가 카드 효과보다 반드시 먼저 끝난다.
        if (!effectApplied && currentActiveCard != null && currentActiveCard.user == User.Ally)
            TriggerRelicsOnCardUsed(selectedButton, targetPos);

        effectApplied = true;
        CardCanvas.instance.isCardEffecting = true;

        ApplyCardEffectNow(cardEffect, targetPos);
    }

    // 카드/예약효과(TurnEffect·유물 등) 공용: CardEffect 하나를 targetPos 기준으로 실제로 적용한다.
    // currentActiveCard/pendingEffects/effectApplied 같은 "지금 실제 카드를 쓰는 중" 상태는 전혀 건드리지
    // 않으므로, 다른 카드가 한창 처리되는 도중에 끼어들어도(진행 중인 카드의 pendingEffects를 훼손하지 않고)
    // 안전하게 호출할 수 있다. 캐스터는 selectedButton으로 넘겨받는다(호출부가 미리 세팅).
    void ApplyCardEffectNow(CardEffect cardEffect, Vector2Int targetPos)
    {
        if (cardEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
            cardEffect.targetlogic == TargetLogic.AllAlliesInRange ||
            cardEffect.targetlogic == TargetLogic.AllPiecesInRange)
        {
            ExecuteAreaEffect(cardEffect, targetPos);
            return;
        }

        switch (cardEffect.type)
        {
            case EffectType.Move:
            {
                Piece moveCaster = GetButtonScript(selectedButton).GetPieceScript();
                if (moveCaster != null && moveCaster.activeEffects.Exists(e => e is MovementDisabledEffect))
                {
                    pendingEffects.Clear();
                    AnnouncementUI.instance?.Show("이동 불가 상태입니다");
                    break;
                }
                MovePiece(selectedButton, targetPos, cardEffect);
                break;
            }
            case EffectType.Damage:
            {
                // hasCaster가 false인 카드(예: MagicAttackCard)는 캐스터 없이 즉시발동하는 경우라 보너스 없이 그대로 적용
                Piece caster = cardEffect.hasCaster ? GetButtonScript(selectedButton).GetPieceScript() : null;
                int resolvedDmg = ResolveDamageWithColDamage(cardEffect, caster);
                AttackPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                ApplyStatusToTarget(targetPos, cardEffect);
                break;
            }
            case EffectType.Heal:
            {
                int resolvedDmg = ResolveDamageWithColDamage(cardEffect, GetButtonScript(selectedButton).GetPieceScript());
                HealPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                ApplyStatusToTarget(targetPos, cardEffect);
                break;
            }
            case EffectType.Shield:
            {
                int resolvedDmg = ResolveShieldWithBonus(cardEffect, GetButtonScript(selectedButton).GetPieceScript());
                ShieldPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                ApplyStatusToTarget(targetPos, cardEffect);
                break;
            }
            case EffectType.SelfDamage:
                SelfDamagePiece(selectedButton, cardEffect.dmg, cardEffect);
                break;
            case EffectType.Draw:
                CardCanvas.instance.DrawCard();
                break;
            case EffectType.DeBuff:
                ApplyStatusToTarget(targetPos, cardEffect);
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
                    p.AddColDamage(cardEffect.dmg);
                    motionQueue.Enqueue(PieceBuffCor(p, cardEffect));
                    StartMotionQueue();
                    CardCanvas.instance?.RefreshAllCardViews();
                }
                break;
            }
            case EffectType.ShieldBonusUp:
            {
                Piece p = GetButtonScript(targetPos).GetPieceScript();
                if (p != null)
                {
                    p.AddShieldBonus(cardEffect.dmg);
                    motionQueue.Enqueue(PieceBuffCor(p, cardEffect));
                    StartMotionQueue();
                    CardCanvas.instance?.RefreshAllCardViews();
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
            case EffectType.AddCard:
                CardCanvas.instance.AddCardDuringCombat(cardEffect.addCardID, cardEffect.addCardZone);
                break;
            case EffectType.Cleanse:
                CleanseTarget(targetPos, cardEffect);
                break;
            case EffectType.Charge:
                // 의도적인 무효과: 텔레그래프형 적이 공격 전에 한 턴을 예고만 하며 흘려보내는 용도.
                break;
            case EffectType.Stun:
                // 의도적인 무효과: 기절한 적이 이번 턴을 스턴으로 소모했다는 표시일 뿐.
                break;
            default:
                Debug.LogError("효과 타입을 찾지 못했습니다");
                break;
        }
    }

    void ApplyTurnEffectToTarget(Vector2Int targetPos, CardEffect cardEffect)
    {
        if (cardEffect.onTurnEndEffect == null) return;
        Piece target = GetButtonScript(targetPos).GetPieceScript();
        if (target == null) return;
        TurnEffect turnEffect = new TurnEffect(cardEffect.turnPhase, cardEffect.onTurnEndEffect, cardEffect.turnDuration);
        target.AddStatusEffect(turnEffect);
        target.ShowStatusText(turnEffect.DisplayName, turnEffect.IsBuff, turnEffect.EffectColor);
        motionQueue.Enqueue(PieceBuffCor(target, cardEffect));
        StartMotionQueue();
    }

    void ApplyStatusToTarget(Vector2Int targetPos, CardEffect cardEffect)
    {
        if (targetPos.x < 0 || targetPos.y < 0) return;
        ApplyStatusToTarget(new List<Vector2Int> { targetPos }, cardEffect);
    }

    void ApplyStatusToTarget(List<Vector2Int> targets, CardEffect cardEffect)
    {
        if (cardEffect.statusEffectType == StatusEffectType.None) return;
        foreach (Vector2Int pos in targets)
        {
            Piece target = GetButtonScript(pos).GetPieceScript();
            if (target == null) continue;
            StatusEffect effect = CreateStatusEffect(cardEffect.statusEffectType, cardEffect.statusDuration, cardEffect.statusPower,
                cardEffect.effectRange, cardEffect.targetlogic);
            if (effect != null)
            {
                target.AddStatusEffect(effect);
                target.TriggerAnim(effect.IsBuff ? "Buff" : "DeBuff");
                target.ShowStatusText(effect.DisplayName, effect.IsBuff, effect.EffectColor);
            }
        }
    }

    void CleanseTarget(Vector2Int targetPos, CardEffect cardEffect)
    {
        Piece target = GetButtonScript(targetPos)?.GetPieceScript();
        if (target == null) return;
        bool removedAny = false;
        for (int i = target.activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = target.activeEffects[i];
            if (effect.IsBuff != cardEffect.cleanseBuffs) continue;
            target.activeEffects.RemoveAt(i);
            effect.OnRemove(target);
            removedAny = true;
        }
        if (removedAny)
        {
            target.TriggerAnim(cardEffect.cleanseBuffs ? "DeBuff" : "Buff");
            target.ShowStatusText(cardEffect.cleanseBuffs ? "무효화" : "정화", cardEffect.cleanseBuffs, new Color(0.6f, 0.85f, 1f));
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
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.self }, duration),
            StatusEffectType.TurnDamageEnd      => new TurnEffect(TurnPhase.OwnTurnEnd,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.self }, duration),
            StatusEffectType.TurnAoEDamageStart => new TurnEffect(TurnPhase.OwnTurnStart,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.AllEnemiesInRange, effectRange = range }, duration),
            StatusEffectType.TurnAoEDamageEnd   => new TurnEffect(TurnPhase.OwnTurnEnd,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.AllEnemiesInRange, effectRange = range }, duration),
            StatusEffectType.Thorn              => new ThornEffect(duration, power),
            StatusEffectType.MovementDisabled   => new MovementDisabledEffect(duration),
            _                                   => null,
        };
    }

    Vector2Int FindPiecePos(Piece piece)
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (GetButtonScript(pos).GetPieceScript() == piece)
                    return pos;
            }
        return new Vector2Int(-1, -1);
    }

    void ExecuteAreaEffect(CardEffect cardEffect, Vector2Int center)
    {
        if (cardEffect.effectRange == null) return;

        // hasCaster가 false인 카드(예: MouseCentered AoE)는 캐스터 개념이 없음 — selectedButton이 우연히
        // 어떤 기물의 칸과 겹치더라도 그 기물을 캐스터로 오인하면 안 되므로 caster를 아예 null로 둔다.
        Piece caster = cardEffect.hasCaster ? GetButtonScript(selectedButton).GetPieceScript() : null;

        // 아군/적 판정은 caster 기물의 teamID가 아니라 카드 자체의 user(Ally/Enemy)를 기준으로 한다.
        // caster 기물이 없는 MouseCentered 카드도 이 카드를 누가 쓰는 카드인지로 정확히 판정할 수 있다.
        // currentActiveCard가 없는 경우(TurnEffect/유물 같은 예약 효과)는 caster의 teamID로 대신 판정한다.
        int userTeam = currentActiveCard != null
            ? (currentActiveCard.user == User.Ally ? 0 : 1)
            : (caster != null ? caster.teamID : 0);
        int targetTeam = cardEffect.targetlogic == TargetLogic.AllEnemiesInRange
            ? (userTeam == 0 ? 1 : 0)
            : userTeam;
        var targets = new List<Vector2Int>();

        List<Vector2Int> offsets = cardEffect.effectRange.GetAbleRange();
        Vector2Int actualCenter = center;

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

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int pos = actualCenter + offset;
            if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;

            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            if (cardEffect.targetlogic != TargetLogic.AllPiecesInRange && p.teamID != targetTeam) continue;
            targets.Add(pos);
        }

        switch (cardEffect.type)
        {
            case EffectType.Damage:
                if (cardEffect.hasCaster)
                    AreaAttackPiece(selectedButton, targets, ResolveDamageWithColDamage(cardEffect, caster), cardEffect);
                else
                    AreaAttackPiece(targets, ResolveDamageWithColDamage(cardEffect, caster), cardEffect);
                ApplyStatusToTarget(targets, cardEffect);
                break;
            case EffectType.Shield:
                AreaShieldPiece(targets, ResolveShieldWithBonus(cardEffect, caster), cardEffect);
                ApplyStatusToTarget(targets, cardEffect);
                break;
            case EffectType.Heal:
                AreaHealPiece(targets, cardEffect.dmg, cardEffect);
                ApplyStatusToTarget(targets, cardEffect);
                break;
            case EffectType.ApplyStatus:
                ApplyStatusToTarget(targets, cardEffect);
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
        lockedCaster = new Vector2Int(-1, -1);
        lockedCasterPiece = null;
        ClearSelectedButton();
        CancelPieceSelection();
        ClearUseEligibilityPreview();
        SetCasterIndicator(CardCanvas.instance?.ActivePiece, false);
    }

    void FinishCardUsage()
    {
        if (currentActiveCard != null && currentActiveCard.blocksMovementAfterUse && casterPiece != null)
            casterPiece.movedThisTurn = true;
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

    // RequestPieceSelection으로 보드에서 직접 고른 기물들 각각에게 effect를 적용한다.
    void ApplyPieceSelectionEffect(CardEffect effect, List<Piece> selected)
    {
        foreach (var piece in selected)
            ExecuteCardEffectOnPiece(FindPiecePos(piece), piece, effect);
        ScheduleNextCardEffect();
    }
}
