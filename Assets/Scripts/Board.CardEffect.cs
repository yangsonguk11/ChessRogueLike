using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard;
    bool effectApplied = false;
    public bool EffectApplied => effectApplied;
    // 무덤 비용(CardEffect.graveCost)을 내는 기물 — 아군 카드는 카드를 낸 기물(ActivePiece), 적/자동행동
    // 카드는 UseCard 시점의 selectedButton 기물(TurnControl이 미리 세팅). 카드 사용 동안 고정된다.
    Piece currentCardCaster;
    // CardEffect.useLastTarget이 재사용하는 직전 효과의 targetPos
    Vector2Int lastEffectTargetPos = new Vector2Int(-1, -1);
    // 이번 카드 사용 중 카드 효과(DiscardHand/SelectAndDiscard)로 실제로 버려진 카드 수 — CardEffect.hitsPerDiscarded가 읽는다.
    // 직전 효과 하나가 아니라 카드 전체 누적이라, 사이에 다른 효과가 끼어도 유지된다.
    int discardedThisCard;

    // 이름과 달리 카드를 "실제로 사용"하는 함수가 아니라, 이제부터 처리할 카드를 보드에 등록(무장)하는 함수다.
    // currentActiveCard/시전자를 세팅하고 effects를 pendingEffects에 채운 뒤, 첫 효과가 요구하는 모드
    // (boardmode)로 전환해 타겟 입력을 기다린다. 효과 적용과 에너지 차감은 모두 ExecuteEffect(effectApplied가
    // true가 되는 순간)에서 일어난다.
    // 호출 시점은 카드 종류마다 다르다:
    //  - 타겟팅(NeedsTargeting)/pieceSelectCount/Self 카드: CardCanvas.CommitNowUsingCard가 카드를 "집는"
    //    시점(드롭 전)에 호출 — 첫 효과가 타겟을 기다리므로 아직 아무것도 적용되지 않았고 취소도 가능하다.
    //  - 그 외(DrawCard처럼 효과가 전부 Inspect인 카드): CardCanvas.OnDragCardReleased가 "드롭" 시점에 호출.
    // 큐 맨 앞 효과가 위치가 필요 없는 Inspect면 ProcessNextCardEffect가 이 호출 안에서 곧바로 ExecuteEffect까지
    // 실행하므로, 그 경우엔 UseCard 호출 중에 효과 적용·에너지 차감이 함께 일어난다(DrawCard는 드롭 시점).
    public void UseCard(Card card)
    {
        Debug.Log($"UseCard: {card.name} (user: {card.user}, effects: {card.effects.Count})");
        if (card.user == User.Ally)
        {
            ClearSelectedButton();
            ShowUseEligibilityPreview(card);
        }
        currentCardCaster = card.user == User.Ally
            ? CardCanvas.instance?.ActivePiece
            : (isSelectedButtonActive() ? GetButtonScript(selectedButton).GetPieceScript() : null);
        lastEffectTargetPos = new Vector2Int(-1, -1);
        discardedThisCard = 0;
        effectApplied = false;
        currentActiveCard = card;
        pendingEffects.Clear();
        foreach (var effect in card.effects)
            pendingEffects.Enqueue(effect with { caster = currentCardCaster });
        ProcessNextCardEffect();
    }

    // 드레인 가드 — 애니메이션 대기가 사라지면서 ExecuteEffect(자기타겟 즉시실행 등)가 같은 호출
    // 스택 안에서 ScheduleNextCardEffect를 통해 ProcessNextCardEffect를 재진입시킬 수 있다(예:
    // selectedButton 대입 → OnSelectBoard → ExecuteEffect → ScheduleNextCardEffect). 재귀 대신
    // "지금 처리 중이면 한 번 더 돌 것만 표시하고 리턴"으로 반복 처리해 스택이 깊어지지 않게 하고,
    // 바깥쪽 호출이 selectedButton 등을 다 쓰기 전에 안쪽 호출이 상태를 먼저 리셋해버리는 걸 막는다.
    bool cardEffectDraining = false;
    bool cardEffectNeedsAnotherPass = false;

    void ProcessNextCardEffect()
    {
        if (cardEffectDraining)
        {
            cardEffectNeedsAnotherPass = true;
            return;
        }

        cardEffectDraining = true;
        try
        {
            ProcessNextCardEffectStep();
            while (cardEffectNeedsAnotherPass)
            {
                cardEffectNeedsAnotherPass = false;
                ProcessNextCardEffectStep();
            }
        }
        finally
        {
            cardEffectDraining = false;
        }
    }

    void ProcessNextCardEffectStep()
    {
        if (pendingEffects.Count == 0)
        {
            FinishCardUsage();
            return;
        }

        CardEffect nextEffect = pendingEffects.Peek();

        // 시전자(카드를 쓴 기물, 소환 시 효과면 소환된 기물)가 앞 효과로 죽어 보드에 없으면 이 효과는 건너뛴다.
        if (nextEffect.caster != null && FindPiecePos(nextEffect.caster).x < 0)
        {
            pendingEffects.Dequeue();
            ScheduleNextCardEffect();
            return;
        }

        // 카드 주인이 아닌 기물이 시전하는 효과(소환 시 효과): 적 카드 규칙대로 자동 실행한다(ProcessEnemyCardEffect가
        // 그 기물 위치를 시전자로 잡음). 실행하는 동안 바뀐 선택 상태는 되돌려 카드의 이후 효과로 새지 않게 한다 —
        // 자동 실행은 이 호출 안에서 끝나고 ScheduleNextCardEffect는 드레인 중이라 다음 패스만 예약한다.
        if (nextEffect.caster != null && nextEffect.caster != currentCardCaster)
        {
            if (nextEffect.graveCost > 0 && !nextEffect.caster.HasGrave(nextEffect.graveCost))
            {
                pendingEffects.Dequeue(); // 무덤 부족: 이 효과만 스킵
                ScheduleNextCardEffect();
                return;
            }
            var saved = (_selectedButton, casterPiece);
            ProcessEnemyCardEffect(nextEffect);
            (_selectedButton, casterPiece) = saved;
            return;
        }

        // 무덤 부족: 타겟팅에 들어가기 전에 판정해서, 실행되지 않을 효과 때문에 타겟을 고르게 하지 않는다.
        // 첫 효과면 카드 전체 무효(아군은 CardCanvas.UseCard에서 이미 막히므로 사실상 적/자동행동 경로),
        // 2번째 이후면 이 효과만 건너뛴다. 로직이 동기라 앞 효과가 만든 무덤 변화가 여기서 바로 반영된다.
        if (nextEffect.graveCost > 0 && (currentCardCaster == null || !currentCardCaster.HasGrave(nextEffect.graveCost)))
        {
            if (pendingEffects.Count == currentActiveCard.effects.Count) pendingEffects.Clear();
            else pendingEffects.Dequeue();
            ScheduleNextCardEffect();
            return;
        }

        if (nextEffect.useLastTarget && lastEffectTargetPos.x >= 0)
        {
            // 일반 공격은 selectedButton을 유지하지만, 이동(MovePiece)이 지웠다면 시전자의 현재 위치로 복원한다.
            // setter는 OnSelectBoard 부작용이 있으므로 백킹 필드에 직접 대입(ProcessEnemyCardEffect와 동일).
            if (!isSelectedButtonActive() && nextEffect.caster != null) _selectedButton = FindPiecePos(nextEffect.caster);
            ExecuteEffect(pendingEffects.Dequeue(), lastEffectTargetPos);
            ScheduleNextCardEffect();
            return;
        }

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
                var filters = new List<PieceSelectFilter> { PieceSelectFilters.Team(0) };
                if (effect.excludeCasterFromPieceSelection && CardCanvas.instance?.ActivePiece != null)
                    filters.Add(PieceSelectFilters.ExcludePiece(CardCanvas.instance.ActivePiece));
                RequestPieceSelection(
                    effect.pieceSelectCount,
                    (selected) => ApplyPieceSelectionEffect(effect, selected),
                    filters.ToArray());
                return;
            }

            ExecuteEffect(pendingEffects.Dequeue());
            ScheduleNextCardEffect();
        }
        else if (nextEffect.caster != null && pendingEffects.Count < currentActiveCard.effects.Count)
        {
            // 후속 효과(첫 효과가 아님 — 위 무덤 판정과 같은 기준, 앞에 끼워 넣은 소환 시 효과는 이미 소진됨):
            // 카드를 쓴 기물을 현재 위치에서 다시 선택한 것처럼 처리한다 — self 타겟이면 OnSelectBoard가 바로
            // 실행하고, 아니면 이 효과의 사거리를 띄워 클릭을 기다린다(이동했으면 새 위치 기준).
            // 첫 효과는 드롭(ConfirmCasterOnDrop)이 시전자를 확정하므로 건드리지 않는다.
            selectedButton = FindPiecePos(nextEffect.caster);
            if (isSelectedButtonActive())
                GetButtonScript(selectedButton).SelectedTrue();
        }
    }

    // 카드를 실제로 드롭(놓음)한 시점에 CardCanvas.OnDragCardReleased가 호출: 캐스터 선택이 필요한
    // 카드라면(원래 여기서 플레이어가 자기 기물을 클릭해야 했던 것을) 카드를 낸 기물로 바로 확정한다.
    // self 타겟 효과는 이 호출 안에서 OnSelectBoard를 통해 곧바로 실행될 수 있다 — 호출부는 그 후
    // nowusingCard가 비었는지 확인해서 카드 사용이 이미 끝났는지 판단해야 한다.
    public void ConfirmCasterOnDrop()
    {
        if (pendingEffects.Count == 0 || currentActiveCard == null || currentActiveCard.user != User.Ally) return;
        if (isSelectedButtonActive()) return;

        if (boardmode == BoardMode.command || boardmode == BoardMode.targeting)
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

    // 애니메이션(motionQueue) 완료를 기다리지 않는다 — ApplyCardEffectNow는 모든 EffectType에서
    // 상태 변화(데미지/이동/상태이상 등)를 이미 동기로 적용하고 연출만 motionQueue에 넣으므로,
    // 다음 효과의 로직은 이번 효과의 애니메이션이 끝나길 기다릴 이유가 없다. 이 즉시성이 카드 예약
    // 기능의 전제조건이다 — 자세한 근거는 계획 문서(cardeffect-misty-ullman.md) Stage 2 참고.
    void ScheduleNextCardEffect()
    {
        ProcessNextCardEffect();
    }

    void ProcessEnemyCardEffect(CardEffect nextEffect)
    {
        // 효과에 기록된 시전자의 현재 위치를 기준으로 풀이한다 — 앞 효과(예: Move)로 이동했으면 새 위치.
        if (nextEffect.caster != null)
            _selectedButton = FindPiecePos(nextEffect.caster);

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
        else
        {
            // Inspect처럼 위치가 필요 없는 효과: 시전자 칸 기준으로 즉시 실행(분기가 없으면 큐가 멈춘다)
            ExecuteEffect(pendingEffects.Dequeue(), selectedButton);
            ScheduleNextCardEffect();
        }
    }

    // 도발 판정용: effect가 casterPos에서 pos의 기물에 실제로 닿는지. 이동은 이동공격 도착 칸이 있어야 닿는다
    // (MoveAttack이 실패하는 조건과 같은 GetAdjacentLocation 기준, noMoveAttack이면 닿지 않음). 그 외 효과는 사거리 안이면 닿는다.
    bool CanEffectReach(CardEffect effect, Vector2Int casterPos, Vector2Int pos) =>
        effect == null || effect.type != EffectType.Move
        || (!effect.noMoveAttack && GetAdjacentLocation(casterPos, pos).x >= 0);

    // 도발: candidates 중 targetTeam 소속이고 도발을 가졌으며 effect가 실제로 닿는 기물의 칸이 하나라도 있으면 그 칸들만,
    // 없으면 candidates를 그대로 돌려준다 — "사거리 안에 도발 기물이 있으면 그 기물들 우선, 없으면 원래 로직".
    // 도발 기물이 여럿이면 전부 남기고, 그중 무엇을 고를지는 호출부의 원래 기준(가장 가까움/최저 체력 등)이 정한다.
    // 적/자동행동 아군 AI의 대상 선택이 쓰고, 플레이어 카드의 대상 제한(적 도발, 예정)도 같은 함수를 쓴다.
    List<Vector2Int> PrioritizeTauntTargets(List<Vector2Int> candidates, int targetTeam, CardEffect effect, Vector2Int casterPos)
    {
        List<Vector2Int> taunting = candidates.FindAll(pos =>
        {
            Piece p = GetPieceAt(pos);
            return p != null && p.teamID == targetTeam && p.HasTaunt() && CanEffectReach(effect, casterPos, pos);
        });
        return taunting.Count > 0 ? taunting : candidates;
    }

    Vector2Int ResolveEnemyTarget(CardEffect effect)
    {
        switch (effect.targetlogic)
        {
            case TargetLogic.NearestEnemy:
                return ResolveNearestEnemyTarget(effect);
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
        int bestTauntCount = 0;
        bool targetsFoe = targetTeam != caster.teamID; // 도발은 상대 진영을 노릴 때만 본다

        foreach (Vector2Int dir in directions)
        {
            int count = 0;
            int tauntCount = 0;
            foreach (Vector2Int offset in RotateOffsets(offsets, dir))
            {
                Vector2Int pos = selectedButton + offset;
                if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p == null || p.teamID != targetTeam) continue;
                count++;
                if (targetsFoe && p.HasTaunt()) tauntCount++;
            }
            if (count == 0) continue;
            // 도발 기물을 더 많이 맞히는 방향 우선 — 같으면 원래대로 대상 수가 많은 쪽
            if (tauntCount > bestTauntCount || (tauntCount == bestTauntCount && count > bestCount))
            {
                bestTauntCount = tauntCount;
                bestCount = count;
                bestDir = dir;
            }
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

        foreach (Vector2Int pos in PrioritizeTauntTargets(selectedButtonMovable, targetTeam, effect, selectedButton))
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

    Vector2Int ResolveNearestEnemyTarget(CardEffect effect)
    {
        // 캐스터 자신의 teamID 기준으로 상대팀을 동적으로 계산 — teamID==1(적)뿐 아니라
        // teamID==0(자동행동 아군)이 이 로직을 써도 올바르게 반대팀을 노리게 하기 위함.
        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        int targetTeam = caster != null && caster.teamID == 0 ? 1 : 0;

        List<Vector2Int> movableRange = GetButtonScript(selectedButton).GetPiece()?.GetComponent<Piece>().GetMoveableButton()
            ?? new List<Vector2Int>();
        AddMovableButtons(selectedButton, movableRange);

        float minDistance = float.MaxValue;
        Vector2Int bestTargetPos = new Vector2Int(-1, -1);

        // 사거리 안에 (닿는) 도발 기물이 있으면 그 기물들 중에서만 가장 가까운 기물을 고른다.
        foreach (Vector2Int movablePos in PrioritizeTauntTargets(selectedButtonMovable, targetTeam, effect, selectedButton))
        {
            Piece p = GetButtonScript(movablePos).GetPiece()?.GetComponent<Piece>();
            if (p != null && p.teamID == targetTeam)
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
        Vector2Int globalNearestPlayer = GetNearestPlayerPos(selectedButton, targetTeam);
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

        int casterColDmg = cardEffect.ignoreCasterColDamageBonus ? 0 : (caster?.ColDamageDelta ?? 0);
        int result = cardEffect.type == EffectType.Damage ? cardEffect.dmg + casterColDmg : cardEffect.dmg;
        return Mathf.Max(0, result);
    }

    // Shield 타입이면 dmg에 시전자의 shieldBonus를 가산 (colDamage와 같은 구조의 별개 스탯)
    int ResolveShieldWithBonus(CardEffect cardEffect, Piece caster)
    {
        int casterShieldBonus = cardEffect.ignoreCasterShieldBonus ? 0 : (caster?.ShieldBonusDelta ?? 0);
        int result = cardEffect.type == EffectType.Shield ? cardEffect.dmg + casterShieldBonus : cardEffect.dmg;
        return Mathf.Max(0, result);
    }

    // 무덤 비용을 시전자(e.caster — 카드 효과면 UseCard가 기록한 카드 시전자, 소환 시 효과면 소환된 기물)에게서
    // 즉시 차감하고, dmgPerGrave가 있으면 소모량만큼 dmg를 올린 사본을 돌려준다.
    // 효과가 실제로 실행되는 시점에만 호출한다 — 타겟팅 대기 중 취소하면 차감되지 않아야 하므로.
    CardEffect PayGrave(CardEffect e)
    {
        if (e.caster == null || (e.graveCost <= 0 && !e.consumeAllGrave)) return e;
        int used = e.caster.ConsumeGrave(e.graveCost, e.consumeAllGrave);
        CardCanvas.instance?.UpdateCardInteractability();
        return e.dmgPerGrave != 0 ? e with { dmg = e.dmg + used * e.dmgPerGrave } : e;
    }

    // hitsPerDiscarded가 있으면 이번 카드의 앞 효과로 실제로 버려진 카드 수(discardedThisCard)만큼 타격 수를 정한
    // 사본을 돌려준다. 로직이 동기라 앞 효과(DiscardHand 등)가 끝난 시점에 이미 확정된 값을 읽는다.
    CardEffect ApplyDiscardedHits(CardEffect e) =>
        e.hitsPerDiscarded > 0 ? e with { hitCount = discardedThisCard * e.hitsPerDiscarded } : e;

    void ExecuteEffect(CardEffect cardEffect, Vector2Int targetPos = default)
    {
        cardEffect = PayGrave(cardEffect);
        cardEffect = ApplyDiscardedHits(cardEffect);
        lastEffectTargetPos = targetPos;

        // effectApplied가 false→true로 바뀌는 지금 이 순간이 이 카드의 첫 효과가 실제로 처리되기 시작하는
        // 시점이다(그 전까지는 CardCanvas.CancelCardUsage/RevertNowUsingCardToHeld로 언제든 취소 가능하고,
        // 이 지점부턴 취소가 막힌다 — EffectApplied 프로퍼티 참고). 카드 하나당 딱 한 번만 발동해야 하므로
        // 아직 false일 때만(=이 카드의 첫 효과일 때만) 호출한다. relicsOnCardUsed를 전부 큐에 모아
        // 동기적으로 순차 처리하고 나서(TriggerRelicsOnCardUsed 내부), 곧바로 아래에서 카드 자신의
        // 첫 효과(cardEffect)로 자연스럽게 이어진다 — 유물 효과 전부가 카드 효과보다 반드시 먼저 끝난다.
        // 에너지 차감도 여기서 먼저 끝내둔다 — 카드 자신의 효과(예: ReduceCost)가 이후 Cost를 바꾸더라도
        // 이번 사용분 차감엔 영향이 없도록(예전엔 모든 효과가 끝난 뒤 FinishUseCard에서 차감해서, 카드가
        // 자기 코스트를 스스로 깎으면 이번 판 차감액까지 할인돼버리는 문제가 있었다).
        if (!effectApplied && currentActiveCard != null && currentActiveCard.user == User.Ally)
        {
            TriggerRelicsOnCardUsed(selectedButton, targetPos);
            CardCanvas.instance.DeductEnergyForCard(currentActiveCard);
        }

        effectApplied = true;
        CardCanvas.instance.isCardEffecting = true;

        // 버려진 카드가 없어 타격 수가 0이면 스킵 — AttackPiece 타격 루프의 Mathf.Max(1, hitCount)가 0타를 1타로 바꾸지 않게.
        // 카드 사용(에너지 차감 등)은 위에서 이미 처리됐고, 호출부가 이어서 ScheduleNextCardEffect를 부르므로 큐도 멈추지 않는다.
        if (cardEffect.hitsPerDiscarded > 0 && cardEffect.hitCount <= 0) return;

        ApplyCardEffectNow(cardEffect, targetPos);
    }

    // CardCanvas의 손패↔덱/버림/소멸 로직 메서드(HandtoDiscardCount 등)가 돌려준 "이동해야 할 카드"
    // 목록을 받아, 각 카드의 비주얼 이동(MoveCardVisualCor)을 motionQueue에 그대로 enqueue한다.
    // ShieldPiece/AttackPiece가 자기 애니메이션을 enqueue하는 것과 완전히 같은 패턴이라, 같은 카드효과가
    // 먼저 큐에 넣어둔 보드 애니메이션(예: 실드 연출)이 다 끝난 뒤에야 카드 이동 연출이 재생된다.
    // 마지막에 AlignCardsCor()를 하나 더 enqueue해서, 남은 손패의 재정렬(AlignCards)도 카드가 실제로
    // 날아가는 연출이 끝난 시점에 맞춰 재생되게 한다(그 전엔 이미 즉시 갱신된 handNumber 기준으로
    // 손패 조작은 정상 동작함 — CardCanvas.RefreshHandIndices 참고).
    void EnqueueCardMoves(List<(RectTransform card, Vector3 pos, Quaternion rot)> moves, float duration)
    {
        if (moves.Count == 0) return;
        foreach (var (card, pos, rot) in moves)
            motionQueue.Enqueue(CardCanvas.instance.MoveCardVisualCor(card, pos, rot, duration));
        motionQueue.Enqueue(CardCanvas.instance.AlignCardsCor());
        StartMotionQueue();
    }

    // 카드/예약효과(TurnEffect·유물 등) 공용: CardEffect 하나를 targetPos 기준으로 실제로 적용한다.
    // currentActiveCard/pendingEffects/effectApplied 같은 "지금 실제 카드를 쓰는 중" 상태는 전혀 건드리지
    // 않으므로, 다른 카드가 한창 처리되는 도중에 끼어들어도(진행 중인 카드의 pendingEffects를 훼손하지 않고)
    // 안전하게 호출할 수 있다. 캐스터는 selectedButton으로 넘겨받는다(호출부가 미리 세팅).
    void ApplyCardEffectNow(CardEffect cardEffect, Vector2Int targetPos)
    {
        // Charge는 지속시간을 따로 추적하는 상태이상이 아니라 "다음 카드 효과가 나올 때까지"만 유효한
        // 예고 연출이라, 매번 여기서 일단 꺼두고 cardEffect.type이 진짜 Charge일 때만(아래 case) 다시 켠다.
        // 그래야 "같은 기물이 다음으로 어떤 CardEffect든 적용받는 순간" 자동으로 사라진다.
        // DrawCard/FetchAttackCard처럼 모든 효과가 Inspect 모드인 카드는 캐스터를 선택할 일이 없어
        // selectedButton이 (-1,-1)로 남아있을 수 있으므로, 유효할 때만 조회한다.
        if (isSelectedButtonActive())
            GetButtonScript(selectedButton).GetPieceScript()?.SetAnimBool("Charge", false);

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
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                int resolvedDmg = ResolveDamageWithColDamage(cardEffect, caster);
                // 다중 타격(hitCount/hitsPerDiscarded)도 한 번 호출 — 피해는 타격마다 따로 들어가고, 시전자 애니메이션은 1회만 재생된다.
                AttackPiece(selectedButton, targetPos, resolvedDmg, cardEffect, cardEffect.hitCount);
                break;
            }
            case EffectType.Heal:
            {
                int resolvedDmg = ResolveDamageWithColDamage(cardEffect, GetButtonScript(selectedButton).GetPieceScript());
                HealPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                break;
            }
            case EffectType.Shield:
            {
                int resolvedDmg = ResolveShieldWithBonus(cardEffect, GetButtonScript(selectedButton).GetPieceScript());
                ShieldPiece(selectedButton, targetPos, resolvedDmg, cardEffect);
                break;
            }
            case EffectType.SelfDamage:
                SelfDamagePiece(selectedButton, cardEffect.dmg);
                break;
            case EffectType.Draw:
                CardCanvas.instance.DrawCard();
                break;
            case EffectType.ApplyStatus:
            {
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                ApplyStatusToTarget(caster, targetPos, cardEffect);
                break;
            }
            case EffectType.ApplyTurnEffect:
                ApplyTurnEffectToTarget(targetPos, cardEffect);
                break;
            case EffectType.ColDamageUp:
            {
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                Piece p = GetButtonScript(targetPos).GetPieceScript();
                if (p != null)
                {
                    p.AddColDamage(cardEffect.dmg, showReaction: false);
                    motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, p.ColDamageUpReaction(cardEffect.dmg), cardEffect));
                    StartMotionQueue();
                    CardCanvas.instance?.RefreshAllCardViews();
                    p.ActionText(); // 적의 행동 예고 숫자(dmg + ColDamageDelta)가 턴 종료 패시브 등으로 오른 값을 따라가게
                }
                break;
            }
            case EffectType.ShieldBonusUp:
            {
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                Piece p = GetButtonScript(targetPos).GetPieceScript();
                if (p != null)
                {
                    p.AddShieldBonus(cardEffect.dmg, showReaction: false);
                    motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, p.ShieldBonusUpReaction(cardEffect.dmg), cardEffect));
                    StartMotionQueue();
                    CardCanvas.instance?.RefreshAllCardViews();
                }
                break;
            }
            case EffectType.DiscardHand:
            {
                // 손패가 요청 수보다 적으면 있는 만큼만 버려지므로 moves.Count가 곧 실제로 버려진 수
                var discardMoves = CardCanvas.instance.HandtoDiscardCount(cardEffect.dmg);
                discardedThisCard += discardMoves.Count;
                EnqueueCardMoves(discardMoves, 0.2f);
                break;
            }
            case EffectType.ShuffleHandToDeck:
                EnqueueCardMoves(CardCanvas.instance.HandtoDeckCount(cardEffect.dmg), 0.2f);
                break;
            case EffectType.ExileHand:
                EnqueueCardMoves(CardCanvas.instance.HandtoExileCount(cardEffect.dmg), 0.25f);
                break;
            case EffectType.HandToDeckTop:
                EnqueueCardMoves(CardCanvas.instance.HandtoDeckTop(cardEffect.dmg), 0.2f);
                break;
            case EffectType.AddCard:
                CardCanvas.instance.AddCardDuringCombat(cardEffect.addCardID, cardEffect.addCardZone);
                break;
            case EffectType.Cleanse:
            {
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                Piece cleanseTarget = GetButtonScript(targetPos)?.GetPieceScript();
                var cleanseResult = CleanseTarget(cleanseTarget, cardEffect); // 즉시 적용
                IEnumerator cleanseReaction = cleanseResult.HasValue
                    ? cleanseTarget.StatusTextReaction(cleanseResult.Value.text, cleanseResult.Value.isBuff, cleanseResult.Value.color)
                    : Parallel(); // 지울 게 없었으면 아무 것도 안 하는 반응
                motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, cleanseReaction, cardEffect));
                StartMotionQueue();
                break;
            }
            case EffectType.Charge:
            {
                // 텔레그래프 포즈: 다른 애니메이션을 무시하고 즉시 켜지며, 함수 맨 위에서 항상 꺼둔 걸
                // 여기서만 다시 켠다 — 이 기물에게 다음 CardEffect가 뭐가 됐든 적용되는 순간 자동으로 꺼짐.
                GetButtonScript(selectedButton).GetPieceScript()?.SetAnimBool("Charge", true);
                break;
            }
            case EffectType.Stun:
            {
                // 상태 자체는 의도적인 무효과(기절 소모 표시일 뿐) — 캐스터 애니메이션만 재생.
                // 실제 기절 포즈(Stun 토글)는 StunEffect.OnApply/OnRemove가 지속시간 기준으로 관리한다.
                // stunSkip 사운드도 캐스터 애니메이션의 OnAnimationEvent 시점에 맞춰 재생한다.
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, StunSkipReaction(), cardEffect));
                StartMotionQueue();
                break;
            }
            case EffectType.Summon:
                SummonPieceAt(targetPos, cardEffect);
                break;
            case EffectType.ReduceCost:
                currentActiveCard.Cost = Mathf.Max(0, currentActiveCard.Cost - cardEffect.dmg);
                currentActiveCard.RefreshView();
                break;
            case EffectType.GrantChainMoveAttack:
            {
                Piece caster = GetButtonScript(targetPos).GetPieceScript();
                if (caster != null)
                {
                    if (caster.GetComponent<ChainMoveAttackBuff>() == null)
                        caster.gameObject.AddComponent<ChainMoveAttackBuff>();
                    // 다른 버프 카드(ColDamageUp 등)와 동일하게 animTrigger 재생 + 상태 텍스트/파티클/사운드를
                    // 캐스터의 OnAnimationEvent 시점에 맞춰 재생 — 이게 빠져 있어서 카드를 써도 아무 연출이 없었음.
                    motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger,
                        caster.StatusTextReaction("연쇄 공격", true, new Color(1f, 0.27f, 0.27f)), cardEffect));
                    StartMotionQueue();
                }
                break;
            }
            case EffectType.GrantSummonColDamage:
            {
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                if (caster != null)
                {
                    caster.summonColDamagePending += cardEffect.dmg;
                    // 다른 버프 카드와 동일하게 animTrigger 재생 + 상태 텍스트/파티클/사운드 표시.
                    motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger,
                        caster.StatusTextReaction($"다음 소환 강화 +{cardEffect.dmg}", true, new Color(1f, 0.27f, 0.27f)), cardEffect));
                    StartMotionQueue();
                }
                break;
            }
            case EffectType.GrantSummonMaxHp:
            {
                // GrantSummonColDamage와 동일하게 자체 버프 애니메이션/텍스트를 재생한다 — 두 효과가
                // 체인으로 이어져도(SummonMasteryCard/SummonGrowthCard) 각자 수치를 분명히 보여주기 위함.
                Piece caster = GetButtonScript(selectedButton).GetPieceScript();
                if (caster != null)
                {
                    caster.summonMaxHpPending += cardEffect.dmg;
                    motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger,
                        caster.StatusTextReaction($"다음 소환 체력 +{cardEffect.dmg}", true, new Color(1f, 0.27f, 0.27f)), cardEffect));
                    StartMotionQueue();
                }
                break;
            }
            case EffectType.AddGrave:
            {
                // 무덤 증가는 즉시 반영(다음 효과의 graveCost 판정에 바로 쓰임) — 연출만 큐잉한다.
                Piece caster = isSelectedButtonActive() ? GetButtonScript(selectedButton).GetPieceScript() : null;
                Piece target = (targetPos.x >= 0 && targetPos.y >= 0 ? GetButtonScript(targetPos).GetPieceScript() : null) ?? caster;
                target?.AddGrave(cardEffect.dmg);
                if (caster != null && cardEffect.animTrigger != null)
                {
                    motionQueue.Enqueue(TriggerAnimCor(caster, cardEffect.animTrigger, cardEffect: cardEffect));
                    StartMotionQueue();
                }
                CardCanvas.instance?.UpdateCardInteractability();
                break;
            }
            default:
                Debug.LogError("효과 타입을 찾지 못했습니다");
                break;
        }
    }

    void SummonPieceAt(Vector2Int targetPos, CardEffect cardEffect)
    {
        if (targetPos.x < 0 || targetPos.y < 0) return;

        PieceInfo info = cardEffect.summonPieceInfo;
        if (info == null) { Debug.LogError("[Board] Summon 효과에 summonPieceInfo가 없습니다."); return; }

        // 점유된 targetPos(적 위치) 자신을 중심으로 사거리를 다시 잡는다 — 시전자(selectedButton) 기준으로
        // 두면 사거리 경계가 시전자 쪽에 치우쳐서, 적과는 가깝지만 시전자에게서 먼 빈 칸이 부당하게 걸러진다.
        HashSet<Vector2Int> inRange = new HashSet<Vector2Int>();
        if (cardEffect.effectRange != null)
            foreach (Vector2Int offset in cardEffect.effectRange.GetAbleRange())
                inRange.Add(targetPos + offset);

        Vector2Int spawnPos = FindEmptySummonCell(new List<Vector2Int> { targetPos }, inRange, new HashSet<Vector2Int> { targetPos });
        if (spawnPos.x < 0) return; // 사거리 내에 더 시도할 빈 칸이 없음 — 이 효과는 스킵

        GameObject prefab = piecedatabase.GetPiece(info.PieceName);
        if (prefab == null)
        {
            Debug.LogError($"[Board] 소환 실패: '{info.PieceName}' 을(를) PieceDatabase에서 찾을 수 없습니다.");
            return;
        }

        GameObject pieceObj = Instantiate(prefab);
        ApplySpawnOccupancy(spawnPos, pieceObj); // 소환 시 효과가 이 효과 바로 다음(카드의 이후 효과보다 먼저)에 실행되게 큐 맨 앞에 들어간다
        Piece pieceScript = pieceObj.GetComponent<Piece>();

        // 소환 카드를 실제로 쓴 시전자 — SummonMasteryCard가 이 시전자에게 쌓아둔 다음 소환 보너스가
        // 있으면 소모한다(적 소환 카드가 먼저 가로채지 않도록 시전자 개인에게 귀속).
        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        bool hasSummonBuff = caster != null && (caster.summonColDamagePending > 0 || caster.summonMaxHpPending > 0);

        // SummonVisualEffect가 재생되는 타이밍까지 보이지 않게 숨겨둔다 — Instantiate 직후 원래
        // 스케일을 기억해뒀다가, motionQueue에서 그 연출이 실행될 때 다시 키워서 "짠" 하고 나타나게 한다.
        Vector3 summonScale = pieceObj.transform.localScale;
        pieceObj.transform.localScale = Vector3.zero;

        // 소환 등장 연출(SummonVisualEffect)에, 소환 보너스가 있었으면 그 버프 텍스트/파티클도 같이 묶는다.
        var summonReactions = new List<IEnumerator> { pieceScript.SummonVisualEffect(summonScale) };

        if (pieceScript is AutoPiece && pieceScript.teamID == 1)
        {
            enemyPositions.Add(spawnPos);
        }
        else if (pieceScript is AutoPiece)
        {
            autoAllyPositions.Add(spawnPos); // 손패 없음, AI가 자동 행동
            if (hasSummonBuff) summonReactions.AddRange(ApplyAndConsumeSummonBuff(pieceScript, caster));
        }
        else if (pieceScript.teamID == 0)
        {
            // 전투 한정 소환: DataManager에 영구 등록하지 않음 (pieceDataIndex는 -1로 유지)
            PieceData data = DataManager.Instance.BuildPieceData(info, info.DefaultDeckCardIDs);
            pieceScript.SetPieceData(data); // 스탯 전체를 덮어쓰므로 버프는 반드시 이 다음에 적용
            if (hasSummonBuff) summonReactions.AddRange(ApplyAndConsumeSummonBuff(pieceScript, caster));
        }

        // 다른 모든 시전자→대상 연출(PieceAttackCor/PieceHealCor 등)과 동일하게 PlayCasterAndTargetReaction을
        // 거쳐서, 시전자 캐스팅 애니메이션의 Animation Event(또는 Attack 트리거의 DOPunch 폴백 콜백) 시점에
        // 맞춰 기물이 나타나게 한다 — 예전엔 Parallel로 캐스팅 시작과 동시에 나타나 타이밍이 어긋났었다.
        motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, Parallel(summonReactions.ToArray()), cardEffect));
        StartMotionQueue();
    }

    // SummonMasteryCard/SummonGrowthCard가 caster에게 쌓아둔 "다음 소환 보너스"를 새로 소환된 기물에
    // 적용하고 소모한다. 실제로 적용된 스탯만큼 버프 텍스트/파티클 반응을 만들어 반환 — 호출부(SummonPieceAt)가
    // 소환 등장 연출과 함께 재생한다.
    List<IEnumerator> ApplyAndConsumeSummonBuff(Piece newPiece, Piece caster)
    {
        var reactions = new List<IEnumerator>();
        if (caster.summonColDamagePending != 0)
        {
            int delta = caster.summonColDamagePending;
            newPiece.AddColDamage(delta, showReaction: false);
            reactions.Add(newPiece.StatusTextReaction($"이동공격력 +{delta}", true, new Color(1f, 0.27f, 0.27f)));
        }
        if (caster.summonMaxHpPending != 0)
        {
            int delta = caster.summonMaxHpPending;
            newPiece.maxhp += delta;
            newPiece.hp += delta;
            reactions.Add(newPiece.StatusTextReaction($"체력 +{delta}", true, new Color(1f, 0.27f, 0.27f)));
        }
        caster.summonColDamagePending = 0;
        caster.summonMaxHpPending = 0;
        CardCanvas.instance?.RefreshAllCardViews();
        return reactions;
    }

    // 상하좌우(직교) 먼저, 대각선은 나중 — 같은 프론티어(같은 홉 거리) 안에서 상하좌우 빈 칸이 있으면
    // 그쪽을 먼저 반환하도록 순서로 우선순위를 준다. 8방향 모두 홉 거리는 동일하게 취급하되(대각선도
    // 인접으로 인정), 동률일 때만 대각선보다 직교를 우선한다.
    static readonly Vector2Int[] EightDirectionOffsets =
    {
        new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1),
        new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(1, 1),
    };

    // frontier(현재 탐색 중인, 점유된 대상으로부터 같은 거리에 있는 칸 무리)를 한 칸씩 검사해 비어있는
    // 칸을 찾는다. 전부 점유돼 있으면 그 칸들의 인접(8방향)·inRange·미방문 칸으로 다음 frontier를 만들어
    // 재귀한다 — 거리 단계별로(가까운 칸 무리를 전부 검사한 뒤에야 한 칸 더 먼 무리로 넘어가므로) "가장
    // 가까운" 빈 칸을 보장한다(한 방향으로만 깊이 파고드는 방식이면 더 가까운 다른 방향을 놓칠 수 있음).
    // inRange 내에 더 시도할 칸이 없으면 (-1,-1) 반환 — 호출부(SummonPieceAt)가 스킵 신호로 쓴다.
    Vector2Int FindEmptySummonCell(List<Vector2Int> frontier, HashSet<Vector2Int> inRange, HashSet<Vector2Int> tried)
    {
        if (frontier.Count == 0) return new Vector2Int(-1, -1);

        List<Vector2Int> nextFrontier = new List<Vector2Int>();
        foreach (Vector2Int candidate in frontier)
        {
            if (GetPieceAt(candidate) == null) return candidate;

            foreach (Vector2Int dir in EightDirectionOffsets)
            {
                Vector2Int next = candidate + dir;
                if (next.x < 0 || next.x >= N || next.y < 0 || next.y >= M) continue;
                if (!inRange.Contains(next) || tried.Contains(next)) continue;
                tried.Add(next);
                nextFrontier.Add(next);
            }
        }
        return FindEmptySummonCell(nextFrontier, inRange, tried);
    }

    // 기절로 턴을 그냥 흘려보낼 때의 사운드 — 캐스터 애니메이션의 OnAnimationEvent 시점에 맞춰 재생.
    // 상태 변경이 없는 순수 사운드 반응이라 Piece.PieceDeathSound와 동일한 모양.
    IEnumerator StunSkipReaction()
    {
        AudioManager.instance?.PlayStunSkip();
        yield return null;
    }

    void ApplyTurnEffectToTarget(Vector2Int targetPos, CardEffect cardEffect)
    {
        if (cardEffect.onTurnEndEffect == null) return;
        Piece target = GetButtonScript(targetPos).GetPieceScript();
        if (target == null) return;
        // 효과 적용(AddStatusEffect)은 GetHeal/GetShield처럼 즉시 실행 — 텍스트/파티클/사운드만
        // StatusTextReaction으로 캐스터의 OnAnimationEvent 시점까지 미룬다.
        TurnEffect turnEffect = new TurnEffect(cardEffect.turnPhase, cardEffect.onTurnEndEffect, cardEffect.turnDuration);
        target.AddStatusEffect(turnEffect);
        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger,
            target.StatusTextReaction(turnEffect.DisplayName, turnEffect.IsBuff, turnEffect.EffectColor), cardEffect));
        StartMotionQueue();
    }

    // 동반되는 공격/힐/실드 없이 상태이상만 단독으로 거는 카드(ApplyStatus)에서 쓰는 경로. 캐스터가
    // 자신의 animTrigger를 재생하고, 그 OnAnimationEvent 시점에 모든 대상의 상태이상 반응이 함께 뜬다.
    void ApplyStatusToTarget(Piece caster, Vector2Int targetPos, CardEffect cardEffect)
    {
        // 유효하지 않은 targetPos여도(예: 방어적 가드) 캐스터 애니메이션 자체는 그대로 재생한다 —
        // 빈 리스트를 넘기면 반응은 없이 캐스터 트리거만 재생됨(기존 동작과 동일).
        var targets = (targetPos.x >= 0 && targetPos.y >= 0) ? new List<Vector2Int> { targetPos } : new List<Vector2Int>();
        ApplyStatusToTarget(caster, targets, cardEffect);
    }

    void ApplyStatusToTarget(Piece caster, List<Vector2Int> targets, CardEffect cardEffect)
    {
        var reactions = new List<IEnumerator>();
        foreach (Vector2Int pos in targets)
        {
            Piece target = GetButtonScript(pos).GetPieceScript();
            StatusEffect effect = ApplyStatusEffect(target, cardEffect); // 즉시 적용
            if (effect != null)
                reactions.Add(target.StatusTextReaction(effect.DisplayName, effect.IsBuff, effect.EffectColor));
        }
        motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, Parallel(reactions.ToArray()), cardEffect));
        StartMotionQueue();
    }

    // cardEffect에 statusEffectType이 설정돼 있으면 target에게 상태이상을 즉시 걸고 그 효과를 반환한다
    // (없으면 null) — GetHeal/GetShield/GetDamage와 동일한 "즉시 실행되는 상태 변경" 역할. 텍스트/파티클/
    // 사운드는 반환된 효과 정보로 호출부가 Piece.StatusTextReaction을 통해 원하는 시점에 재생한다.
    StatusEffect ApplyStatusEffect(Piece target, CardEffect cardEffect)
    {
        if (target == null || cardEffect == null || cardEffect.statusEffectType == StatusEffectType.None) return null;
        StatusEffect effect = CreateStatusEffect(cardEffect.statusEffectType, cardEffect.statusDuration, cardEffect.statusPower,
            cardEffect.effectRange, cardEffect.targetlogic);
        if (effect == null) return null;
        target.AddStatusEffect(effect);
        return effect;
    }

    // 정화/무효화 대상 효과를 즉시 제거한다(GetHeal/GetShield와 동일하게 즉시 실행). 실제로 뭔가
    // 지워졌으면 표시할 텍스트/isBuff/색상을 반환하고, 아니면 null — 호출부가 null이 아닐 때만
    // Piece.StatusTextReaction으로 감싸 캐스터의 OnAnimationEvent 시점에 재생한다.
    (string text, bool isBuff, Color color)? CleanseTarget(Piece target, CardEffect cardEffect)
    {
        if (target == null) return null;
        bool removedAny = false;
        for (int i = target.activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = target.activeEffects[i];
            if (effect.IsBuff != cardEffect.cleanseBuffs) continue;
            target.activeEffects.RemoveAt(i);
            effect.OnRemove(target);
            removedAny = true;
        }
        if (!removedAny) return null;
        return (cardEffect.cleanseBuffs ? "무효화" : "정화", cardEffect.cleanseBuffs, new Color(0.6f, 0.85f, 1f));
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
            // 자기 자신에게 걸리는 DoT(예: 독/화상류)는 디버프, 적을 매 턴 때리는 광역형은 캐스터 입장에서 버프.
            StatusEffectType.TurnDamageStart    => new TurnEffect(TurnPhase.OwnTurnStart,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.self, isBuff = false }, duration),
            StatusEffectType.TurnDamageEnd      => new TurnEffect(TurnPhase.OwnTurnEnd,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.self, isBuff = false }, duration),
            StatusEffectType.TurnAoEDamageStart => new TurnEffect(TurnPhase.OwnTurnStart,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.AllEnemiesInRange, effectRange = range, isBuff = true }, duration),
            StatusEffectType.TurnAoEDamageEnd   => new TurnEffect(TurnPhase.OwnTurnEnd,
                new CardEffect { requiredMode = BoardMode.Inspect, type = EffectType.Damage, dmg = power, targetlogic = TargetLogic.AllEnemiesInRange, effectRange = range, isBuff = true }, duration),
            StatusEffectType.Thorn              => new ThornEffect(duration, power),
            StatusEffectType.MovementDisabled   => new MovementDisabledEffect(duration),
            StatusEffectType.Vulnerable         => new VulnerableEffect(duration, power),
            StatusEffectType.Taunt              => new TauntEffect(duration),
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

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();

        // 아군/적 판정은 효과에 기록된 시전자(cardEffect.caster)의 진영을 기준으로 한다 — 위치가 아니라 기물
        // 참조라 MouseCentered AoE처럼 시전자 칸을 선택하지 않는 카드에서도 정확하다. 기록이 없을 때만 예전처럼
        // 카드 자체의 user(Ally/Enemy), 그마저 없으면(TurnEffect/유물 같은 예약 효과) caster 칸 기물의 teamID로 판정한다.
        int userTeam = cardEffect.caster != null ? cardEffect.caster.teamID
            : currentActiveCard != null ? (currentActiveCard.user == User.Ally ? 0 : 1)
            : caster.teamID;
        int targetTeam = cardEffect.targetlogic == TargetLogic.AllEnemiesInRange
            ? (userTeam == 0 ? 1 : 0)
            : userTeam;
        var targets = new List<Vector2Int>();
        var cells = new List<Vector2Int>();

        if (cardEffect.lockOnAllyPositions)
        {
            // 아군 위치 고정 공격: 플레이어 턴 시작 때 잠가둔 보드 절대 좌표를 그대로 친다(Board.LockEnemyTelegraphs).
            if ((cardEffect.caster ?? caster) is AutoPiece lockOwner && lockOwner.lockedTargetCells != null)
                cells.AddRange(lockOwner.lockedTargetCells);
        }
        else
        {
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
                cells.Add(actualCenter + offset);
        }

        foreach (Vector2Int pos in cells)
        {
            if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;

            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            if (cardEffect.targetlogic != TargetLogic.AllPiecesInRange && p.teamID != targetTeam) continue;
            targets.Add(pos);
        }

        // 고정 공격을 전원이 피했으면 AreaAttackPiece가 아무 연출 없이 끝나므로, 헛스윙 + 잠긴 칸 표시만 재생해
        // "피했다"는 피드백을 준다.
        if (cardEffect.lockOnAllyPositions && targets.Count == 0 && caster != null)
        {
            motionQueue.Enqueue(TriggerAnimCor(caster, cardEffect.animTrigger, cardEffect: cardEffect));
            StartMotionQueue();
            return;
        }

        switch (cardEffect.type)
        {
            case EffectType.Damage:
                AreaAttackPiece(selectedButton, targets, ResolveDamageWithColDamage(cardEffect, caster), cardEffect);
                break;
            case EffectType.Shield:
                AreaShieldPiece(targets, ResolveShieldWithBonus(cardEffect, caster), cardEffect);
                break;
            case EffectType.Heal:
                AreaHealPiece(targets, cardEffect.dmg, cardEffect);
                break;
            case EffectType.ApplyStatus:
                ApplyStatusToTarget(caster, targets, cardEffect);
                break;
            case EffectType.AddGrave:
                foreach (Vector2Int pos in targets)
                    GetButtonScript(pos).GetPieceScript()?.AddGrave(cardEffect.dmg);
                CardCanvas.instance?.UpdateCardInteractability();
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
        currentCardCaster = null;
        lastEffectTargetPos = new Vector2Int(-1, -1);
        discardedThisCard = 0;
        ClearSelectedButton();
        CancelPieceSelection();
        ClearUseEligibilityPreview();
        SetCasterIndicator(CardCanvas.instance?.ActivePiece, false);
        // effectApplied는 원래 Board.UseCard(다음 카드 시작)에서만 리셋됐다 — isCardEffecting이
        // 카드 로직 완료 시점으로 좁혀지면서(CardCanvas.FinishUseCard), 그 사이 새로 집은 카드의
        // 취소 가드(RevertNowUsingCardToHeld/CancelCardUsage)가 이전 카드의 값으로 계속 막히지
        // 않도록 카드 로직이 끝나는 시점에 함께 리셋한다.
        effectApplied = false;
    }

    void FinishCardUsage()
    {
        if (currentActiveCard != null && currentActiveCard.blocksMovementAfterUse && casterPiece != null)
            casterPiece.movedThisTurn = true;
        // FinishUseCard 시점엔 이 카드의 마지막 효과가 유발한 연출이 이미 전부 motionQueue에 들어가
        // 있다(로직은 동기, 연출만 큐잉 — Stage 2 참고) — 여기서 신호용 항목을 뒤이어 넣어두면 FIFO
        // 특성상 정확히 "이 카드가 유발한 연출이 다 끝난 시점"에 실행된다. 손패에서 쓴 카드(User.Ally)만
        // CardCanvas에 대기 항목을 남기므로, 그 경우에만 신호를 예약한다.
        bool needsFlightSignal = currentActiveCard != null && currentActiveCard.user == User.Ally;
        CardCanvas.instance.FinishUseCard();
        if (needsFlightSignal)
        {
            motionQueue.Enqueue(SignalCardAnimationsDone());
            StartMotionQueue();
        }
        ResetBoardAfterCardUse();
    }

    IEnumerator SignalCardAnimationsDone()
    {
        CardCanvas.instance.OnUsedCardAnimationsComplete();
        yield break;
    }

    // 카드 선택 패널에서 플레이어가 선택을 확정한 후 호출됨
    void ApplyCardSelectionEffect(CardEffect effect, List<RectTransform> selected)
    {
        PayGrave(effect); // 카드 선택 효과는 dmg를 쓰지 않으므로 차감만
        switch (effect.type)
        {
            case EffectType.SelectAndDiscard:
                foreach (var card in selected)
                    CardCanvas.instance.MoveCardToDiscard(card);
                discardedThisCard += selected.Count;
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
        effect = PayGrave(effect); // 선택한 기물 수와 무관하게 한 번만 차감(카드 시전자가 지불)
        foreach (var piece in selected)
            ExecuteCardEffectOnPiece(FindPiecePos(piece), piece, effect with { caster = piece });
        ScheduleNextCardEffect();
    }
}
