using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<CardEffect> pendingEffects = new Queue<CardEffect>();
    Card currentActiveCard;
    bool effectApplied = false;
    public bool EffectApplied => effectApplied;
    // 카드를 쓴 기물(UseCard 호출부가 넘김) — 손패 카드는 카드를 낸 기물(ActivePiece), 적/자동행동 카드는 행동 중인
    // AutoPiece. 무덤 비용(CardEffect.graveCost)도 이 기물이 낸다. 카드 사용 동안 고정된다.
    Piece currentCardCaster;
    // 지금 쓰는 카드가 손패에서 플레이어가 낸 카드인지 — AutoPiece(적·자동 아군)가 쓰면 대상을 자동으로 고르고,
    // 에너지 차감·카드 사용 시 유물·손패 연출 신호 같은 손패 카드 처리를 하지 않는다.
    bool IsHandCard => currentActiveCard != null && currentCardCaster is not AutoPiece;
    // CardEffect.useLastTarget이 재사용하는 직전 효과의 targetPos와, 그 시점에 그 칸에 있던 기물(빈 칸이면 null) —
    // 기물은 useLastTarget 효과가 "같은 대상이 아직 거기 있는지" 판정할 때 쓴다(IsTargetMissing).
    Vector2Int lastEffectTargetPos = new Vector2Int(-1, -1);
    Piece lastEffectTargetPiece;
    // 이번 카드 사용 중 카드 효과(DiscardHand/SelectAndDiscard)로 실제로 버려진 카드 수 — CardEffect.hitsPerDiscarded가 읽는다.
    // 직전 효과 하나가 아니라 카드 전체 누적이라, 사이에 다른 효과가 끼어도 유지된다.
    int discardedThisCard;

    // 카드 사용 확정 — 이 호출부터 카드는 취소할 수 없다. 손패 카드는 대상 지정 검사를 통과한 드롭 시점에 CardCanvas가,
    // 적/자동 아군은 행동 차례에 TurnControl이 호출한다. 카드를 집은 동안은 ShowUseEligibilityPreview로 범위만 보여주고 큐는 비워 둔다.
    // 손패 카드는 CommitHandCardUse(카드 사용 시 유물 → 에너지 차감 → 취소 차단) 뒤에 카드의 효과를 큐에 넣고 처리를 시작한다.
    // 예외: 기물 선택 효과로 시작하는 카드는 기물을 다 고를 때(ApplyPieceSelectionEffect)까지 확정을 미룬다 — 그 전에는 취소할 수 있다.
    // 처리는 카드의 첫 효과에서 입력을 기다리며 멈추고(카드·기물 선택 효과면 선택 UI를 띄운다), 호출부가 이어서 부르는
    // ConfirmCasterOnDrop(시전자 확정 — self 효과는 그 자리에서 실행)과 드롭 칸 클릭으로 진행된다.
    // targetPos: 카드를 놓은 칸(대상이 필요 없는 카드·기물 선택 카드·자동 기물은 생략 → 시전자 칸). 카드 사용 시 유물의 기준 칸으로 쓴다.
    public void UseCard(Card card, Piece caster, Vector2Int? targetPos = null)
    {
        // 사용 불가 카드는 어떤 경로로 쓰여도 효과 큐에 효과(effects)를 넣지 않는다. 버려졌을 때 효과(onDiscardEffects)는
        // 사용이 아니라 반응 효과라 이 함수를 거치지 않고 그대로 발동한다(OnCardDiscarded).
        if (card == null || card.IsUnplayable) return;

        Debug.Log($"UseCard: {card.name} (caster: {(caster != null ? caster.name : "null")}, effects: {card.effects.Count})");
        bool handCard = caster is not AutoPiece;
        if (handCard)
            ShowUseEligibilityPreview(card);
        currentCardCaster = caster;
        lastEffectTargetPos = new Vector2Int(-1, -1);
        lastEffectTargetPiece = null;
        discardedThisCard = 0;
        currentActiveCard = card;
        stunHandledThisCard = false;
        RemoveCardEffects(); // 카드는 큐가 빈 상태에서만 쓰인다 — 혹시 남은 카드 효과만 치우고 엔진 항목은 건드리지 않는다

        // 기물 선택 효과로 시작하는 손패 카드는 보드에서 기물을 다 고른 순간이 대상 지정 완료다 — 그때 사용 확정한다
        // (ApplyPieceSelectionEffect). 그 전까지는 취소(우클릭·다른 카드 잡기·턴 종료)하면 손패로 돌아가고 에너지 변화도 없다.
        bool commitOnPieceSelection = handCard && card.effects.Count > 0 && card.effects[0].pieceSelectCount > 0;
        if (handCard && !commitOnPieceSelection)
        {
            Vector2Int casterPos = caster != null ? FindPiecePos(caster) : new Vector2Int(-1, -1);
            insertNextIndex = 0;
            CommitHandCardUse(casterPos, targetPos ?? casterPos);
        }
        else if (!handCard)
        {
            effectApplied = true;
            CardCanvas.instance.isCardEffecting = true;
        }

        foreach (var effect in card.effects)
            pendingEffects.Enqueue(effect with { caster = currentCardCaster });
        ProcessNextCardEffect();
    }

    // 손패 카드의 사용 확정 — ① 카드 사용 시 효과(유물)를 큐 앞에 넣고(카드의 효과보다 먼저 실행) ② 코스트만큼 에너지를 차감한 뒤
    // ③ 취소를 막는다(EffectApplied를 CardCanvas.CancelCardUsage/RevertNowUsingCardToHeld가 본다).
    // 에너지를 여기서 먼저 차감하므로 카드 자신의 효과(예: ReduceCost)가 이후 Cost를 바꿔도 이번 사용분에는 영향이 없다.
    // 대상 지정 검사를 통과한 Board.UseCard, 기물 선택 카드는 기물을 다 고른 ApplyPieceSelectionEffect가 부른다.
    void CommitHandCardUse(Vector2Int casterPos, Vector2Int targetPos)
    {
        TriggerRelicsOnCardUsed(casterPos, targetPos);
        CardCanvas.instance.DeductEnergyForCard(currentActiveCard);
        effectApplied = true;
        CardCanvas.instance.isCardEffecting = true;
    }

    // 기절한 시전자의 카드 효과를 기절 효과로 바꿔 처리했는지 — 카드 한 장에 기절 연출은 한 번만 낸다.
    bool stunHandledThisCard;

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
            insertNextIndex = 0;
        }
    }

    void ProcessNextCardEffectStep()
    {
        insertNextIndex = 0; // 이번 단계에서 실행되는 효과가 유발한 반응은 큐 맨 앞부터 차례로 들어간다(InsertEffectNext)

        if (pendingEffects.Count == 0)
        {
            // 카드를 쓰는 중이었을 때만 카드 종료 처리 — 타이밍 효과(턴 효과·유물·상태이상 틱)만 처리하고 비었을 때는
            // 선택·모드·손패 상태를 건드리지 않는다.
            if (currentActiveCard != null) FinishCardUsage();
            return;
        }

        // 효과 큐 처리 중 승리하면(적 기물 수 0 — 소환수 제외) 큐에 남은 효과는 모두 무시한다.
        if (boardReady && !IsEventLevel && !HasNonSummonPiece(1))
        {
            pendingEffects.Clear();
            if (currentActiveCard != null) FinishCardUsage();
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

        // 카드 밖에서 넣은 효과(EnqueueEffect/InsertEffectNext — 턴 효과·유물·상태이상 틱·처치 시·소환 시·기물 선택 결과):
        // 대상이 시전자로 정해져 있어 입력 모드를 보지 않고 시전자 칸에서 바로 실행한다.
        // 직전 대상은 되돌려 카드의 이후 효과(useLastTarget)에 새지 않게 한다.
        if (nextEffect.executeOnCaster)
        {
            pendingEffects.Dequeue();
            if (nextEffect.caster != null && (nextEffect.graveCost <= 0 || nextEffect.caster.HasGrave(nextEffect.graveCost)))
            {
                var saved = (lastEffectTargetPos, lastEffectTargetPiece);
                ExecuteEffect(nextEffect, FindPiecePos(nextEffect.caster));
                (lastEffectTargetPos, lastEffectTargetPiece) = saved;
            }
            ScheduleNextCardEffect();
            return;
        }

        // 여기부터는 지금 쓰는 카드(currentActiveCard)의 효과다. 카드 밖에서는 EnqueueEffect/InsertEffectNext만 쓰므로
        // 카드 없이 이런 항목이 남는 일은 없어야 한다 — 생기면 큐가 멈추지 않게 버린다.
        if (currentActiveCard == null)
        {
            Debug.LogWarning("[Board] 쓰는 카드가 없는데 카드 효과가 효과 큐에 남아 있어 건너뜁니다.");
            pendingEffects.Dequeue();
            ScheduleNextCardEffect();
            return;
        }

        // 기절: 시전자가 기절 상태면 카드 효과를 기절 연출만 하는 기절 효과로 처리한다. 카드 한 장에 한 번만 연출하고
        // 남은 카드 효과는 버린다 — 카드 처리 도중에 기절이 걸려도 남은 효과에 똑같이 적용된다.
        // 엔진이 넣은 효과(턴 효과·유물·상태이상 틱 등)는 위에서 이미 처리돼 기절과 무관하게 실행된다.
        if (nextEffect.caster != null && nextEffect.caster.IsStunned())
        {
            Piece stunned = nextEffect.caster;
            RemoveCardEffects();
            if (!stunHandledThisCard)
            {
                stunHandledThisCard = true;
                Vector2Int stunnedPos = FindPiecePos(stunned);
                ApplyCardEffectNow(new CardEffect { type = EffectType.Stun, targetlogic = TargetLogic.self, caster = stunned }, stunnedPos, stunnedPos);
            }
            ScheduleNextCardEffect();
            return;
        }

        // 무덤 부족: 타겟팅에 들어가기 전에 판정해서, 실행되지 않을 효과 때문에 타겟을 고르게 하지 않는다.
        // 첫 효과면 카드 전체 무효(아군은 CardCanvas.UseCard에서 이미 막히므로 사실상 적/자동행동 경로),
        // 2번째 이후면 이 효과만 건너뛴다. 로직이 동기라 앞 효과가 만든 무덤 변화가 여기서 바로 반영된다.
        if (nextEffect.graveCost > 0 && (currentCardCaster == null || !currentCardCaster.HasGrave(nextEffect.graveCost)))
        {
            if (pendingEffects.Count == currentActiveCard.effects.Count) RemoveCardEffects();
            else pendingEffects.Dequeue();
            ScheduleNextCardEffect();
            return;
        }

        if (nextEffect.useLastTarget && lastEffectTargetPos.x >= 0)
        {
            ExecuteEffect(pendingEffects.Dequeue(), lastEffectTargetPos);
            ScheduleNextCardEffect();
            return;
        }

        if (currentCardCaster is AutoPiece)
        {
            ProcessEnemyCardEffect(nextEffect);
            return;
        }

        // 카드 선택 효과: 카드 선택 패널을 띄우고 확정(ApplyCardSelectionEffect)을 기다린다.
        if (nextEffect.IsCardSelection)
        {
            CardEffect effect = pendingEffects.Dequeue();
            CardCanvas.instance.ShowCardSelectionPanel(
                effect.cardZone,
                effect.selectCount,
                effect,
                (selected) => ApplyCardSelectionEffect(effect, selected));
            return;
        }

        // 기물 선택 효과: 보드에서 아군 기물을 직접 클릭해 고르게 한다(ApplyPieceSelectionEffect).
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

        if (nextEffect.caster != null && pendingEffects.Count < currentActiveCard.effects.Count)
        {
            // 후속 효과(첫 효과가 아님 — 위 무덤 판정과 같은 기준, 앞에 끼워 넣은 반응 효과는 이미 소진됨):
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
        if (pendingEffects.Count == 0 || currentActiveCard == null) return;
        if (isSelectedButtonActive()) return;
        // 카드·기물 선택 UI가 떠 있는 동안에는 시전자를 확정하지 않는다 — 확정하면 다음 self 효과가 선택 확정 전에 실행된다.
        // 선택이 끝나면 다음 효과가 후속 효과 경로(시전자 재선택)로 이어진다.
        if (CardCanvas.cardSelectionMode || PieceSelectionActive) return;

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
        Vector2Int casterPos = CasterPosOf(nextEffect);

        // 2순위 "AI 선택 범위" 표시 — 대상 계산(ResolveTargetPos)은 표시를 건드리지 않으므로 여기서 띄운다.
        // NearestEnemy·LowestHP만 띄우고 범위·self 효과는 띄우지 않는다.
        if (casterPos.x >= 0 &&
            (nextEffect.targetlogic == TargetLogic.NearestEnemy || nextEffect.targetlogic == TargetLogic.LowestHP))
            AddMovableButtons(casterPos, nextEffect.GetRangeOffsets(GetPieceAt(casterPos)));

        // self·범위 효과는 시전자 칸이 대상(중심)이다. 방향성 범위 효과의 방향은 여기서 고르지 않는다 — 다음 행동으로 정해질 때
        // 이미 시전자의 facing으로 정해져 있고(AutoPiece.PlanNextMove), 범위는 그 방향대로 펼쳐진다(CardEffect.GetRangeOffsets).
        // 대상을 못 찾으면(-1) 이 효과만 건너뛴다.
        Vector2Int targetPos = ResolveTargetPos(nextEffect, casterPos);
        CardEffect effect = pendingEffects.Dequeue();
        if (targetPos.x >= 0)
            ExecuteEffect(effect, targetPos);
        ScheduleNextCardEffect();
    }

    // 도발 판정용: effect가 casterPos에서 pos의 기물에 실제로 닿는지. 이동은 이동공격 도착 칸이 있어야 닿는다
    // (MoveAttack이 실패하는 조건과 같은 GetAdjacentLocation 기준, noMoveAttack이면 닿지 않음). 그 외 효과는 사거리 안이면 닿는다.
    bool CanEffectReach(CardEffect effect, Vector2Int casterPos, Vector2Int pos) =>
        effect == null || effect.type != EffectType.Move
        || (!effect.noMoveAttack && GetAdjacentLocation(casterPos, pos).x >= 0);

    // 도발 때문에 pos를 대상으로 고를 수 없는지(플레이어의 대상 지정 검사): 단일 대상 공격/이동 효과이고, 사거리(rangeCells) 안에
    // 닿는 상대 도발 기물이 있는데 pos가 그 기물이 아니면 true. 이동공격이 불가한 도발 기물은 무시한다(CanEffectReach).
    bool IsTauntBlocked(CardEffect effect, Piece caster, Vector2Int pos, List<Vector2Int> rangeCells)
    {
        if (effect == null || caster == null || effect.IsAreaLogic || effect.targetlogic == TargetLogic.self) return false;
        if (effect.type != EffectType.Damage && effect.type != EffectType.Move) return false;
        Vector2Int casterPos = FindPiecePos(caster);
        if (casterPos.x < 0) return false;
        int foeTeam = caster.teamID == 0 ? 1 : 0;
        List<Vector2Int> taunting = rangeCells.FindAll(cell =>
        {
            Piece p = GetPieceAt(cell);
            return p != null && p.teamID == foeTeam && p.HasTaunt() && CanEffectReach(effect, casterPos, cell);
        });
        return taunting.Count > 0 && !taunting.Contains(pos);
    }

    // 도달 불가 판정: effect가 casterPos에서 pos의 상대 기물에게 이동공격하려는데 도착 칸이 없는지.
    // 이동 효과가 아니거나, 대상 칸이 비었거나 같은 편이거나, noMoveAttack(충돌 없는 이동)이면 해당 없음(false).
    // 플레이어 카드 드롭(IsUnreachableDropPos)과 AI 이동 대상 선택(ResolveNearestEnemyTarget, ResolveTargetPos 경유)이 쓴다.
    bool IsMoveAttackUnreachable(CardEffect effect, Vector2Int casterPos, Vector2Int pos)
    {
        if (effect == null || effect.type != EffectType.Move || effect.noMoveAttack) return false;
        Piece caster = GetPieceAt(casterPos);
        Piece target = GetPieceAt(pos);
        if (caster == null || target == null || target.teamID == caster.teamID) return false;
        return GetAdjacentLocation(casterPos, pos).x < 0;
    }

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

    // AI 조준 효과의 대상 칸 — targetlogic대로 고른다. 자기 대상·범위 효과(AllXInRange)는 시전자 칸(origin)이 기준이다.
    // 대상이 없으면 (-1,-1).
    // origin = 시전자가 서 있는 칸. 시전자는 그 칸의 기물로 찾고, 이동공격 도착 칸 판정도 그 기물이 거기 있다고 보고 계산하므로
    // 기물이 없는 가상 위치 기준 계산은 지원하지 않는다.
    // 선택 상태(selectedButton)·범위 표시(selectedButtonMovable)를 건드리지 않아 어디서든 호출할 수 있다 —
    // 적 행동 처리(ProcessEnemyCardEffect)는 표시를 따로 한다.
    public static Vector2Int ResolveTargetPos(CardEffect effect, Vector2Int origin)
    {
        Board board = instance;
        if (board == null || effect == null ||
            origin.x < 0 || origin.x >= board.N || origin.y < 0 || origin.y >= board.M)
            return new Vector2Int(-1, -1);

        switch (effect.targetlogic)
        {
            case TargetLogic.NearestEnemy:
                return board.ResolveNearestEnemyTarget(effect, origin);
            case TargetLogic.LowestHP:
                return board.ResolveLowestHPTarget(effect, origin);
            default:
                return origin;
        }
    }

    // AI 행동 결정용 미리보기: caster가 card를 지금 쓰면 맞는 대상이 있는지. 실제 실행과 같은 ResolveTargetPos·GetAreaEffectCells로
    // 계산하고 상태를 바꾸지 않는다. 카드의 주 효과(self가 아닌 첫 효과)만 본다 — ResolveTargetPos는 시전자의 실제 칸 기준이라
    // "이동한 뒤의 두 번째 효과"는 판정할 수 없다. 주 효과가 없는 카드(자기 강화·회복)는 항상 true.
    public static bool HasTargetIfUsedNow(Card card, AutoPiece caster)
    {
        if (card == null || caster == null) return false;
        CardEffect main = card.effects.Find(e => e.targetlogic != TargetLogic.self);
        return main == null || EffectHasTarget(main, caster);
    }

    static bool EffectHasTarget(CardEffect effect, AutoPiece caster)
    {
        Board board = instance;
        if (board == null) return false;
        Vector2Int origin = board.FindPiecePos(caster);
        if (origin.x < 0) return false;

        int foeTeam = caster.teamID == 0 ? 1 : 0;
        CardEffect e = effect with { caster = caster }; // 원본 CardEffect는 바꾸지 않는다

        // 아군 위치 고정 공격: 칸은 플레이어 턴 시작에 상대 위치로 잠기므로 상대가 하나라도 있으면 누군가를 노린다.
        // (GetNearestPlayerPos는 상대가 없으면 origin을 돌려주므로 그 칸의 기물 진영으로 판정한다.)
        if (e.lockOnAllyPositions)
            return board.GetPieceAt(board.GetNearestPlayerPos(origin, foeTeam))?.teamID == foeTeam;

        Vector2Int target = ResolveTargetPos(e, origin);
        if (target.x < 0) return false;

        switch (e.targetlogic)
        {
            case TargetLogic.AllEnemiesInRange:
            case TargetLogic.AllAlliesInRange:
            case TargetLogic.AllPiecesInRange:
            {
                int areaTeam = e.targetlogic == TargetLogic.AllEnemiesInRange ? foeTeam : caster.teamID;
                // 방향성 범위 효과는 이 행동으로 정해지면 시전자가 돌아설 방향(PlanFacing과 같은 목표 → FacingToward)으로 회전한
                // 범위에 상대가 있는지로 판정한다 — 지금 facing은 이전 행동 때의 방향이라 쓰지 않는다.
                Vector2Int? prospectiveFacing = null;
                if (e.RotatesRangeWithFacing)
                {
                    Vector2Int facingTarget = board.FindFacingTarget(origin, foeTeam);
                    if (facingTarget.x >= 0) prospectiveFacing = FacingToward(origin, facingTarget, caster.facing);
                }
                foreach (Vector2Int pos in GetAreaEffectCells(e, origin, target, out _, prospectiveFacing))
                {
                    Piece p = board.GetPieceAt(pos);
                    if (p != null && (e.targetlogic == TargetLogic.AllPiecesInRange || p.teamID == areaTeam))
                        return true;
                }
                return false;
            }
            default:
            {
                // 단일 대상: 이동 효과는 대상이 없으면 다가갈 빈 칸을 돌려주므로 그 칸에 상대가 있어야 대상이 있는 것이다.
                Piece p = board.GetPieceAt(target);
                return p != null && p.teamID == foeTeam;
            }
        }
    }

    // AI가 방향성 효과로 바라볼 목표 칸 — 단일 대상 효과(NearestEnemy·LowestHP)는 그 효과가 고르는 대상, 범위·self 효과는 단일 대상이
    // 없으므로 가장 가까운 상대(FindFacingTarget)다. 목표를 못 찾으면 (-1,-1).
    Vector2Int ResolveFacingTarget(CardEffect effect, Vector2Int origin)
    {
        int foeTeam = effect.caster.teamID == 0 ? 1 : 0;
        if (effect.targetlogic == TargetLogic.NearestEnemy || effect.targetlogic == TargetLogic.LowestHP)
        {
            Vector2Int target = ResolveTargetPos(effect, origin);
            if (target.x >= 0 && target != origin) return target;
        }
        return FindFacingTarget(origin, foeTeam);
    }

    // origin에서 가장 가까운 foeTeam 기물의 칸(거리 동률은 IsCloser 규칙). 도발 기물이 있으면 그중에서 고른다. 상대가 없으면 (-1,-1).
    Vector2Int FindFacingTarget(Vector2Int origin, int foeTeam)
    {
        Vector2Int teamDir = TeamDirection(foeTeam == 0 ? 1 : 0);
        Vector2Int best = new Vector2Int(-1, -1);
        bool bestTaunt = false;
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < M; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece p = GetPieceAt(pos);
                if (p == null || p.teamID != foeTeam) continue;
                bool taunt = p.HasTaunt();
                if ((taunt && !bestTaunt) || (taunt == bestTaunt && IsCloser(origin, pos, best, teamDir)))
                {
                    best = pos;
                    bestTaunt = taunt;
                }
            }
        }
        return best;
    }

    // AutoPiece가 card를 다음 행동으로 정한 순간(AutoPiece.PlanNextMove)에 부른다 — 바라보는 방향을 이 행동에 맞춘다.
    // ① 이전 행동의 방향 고정을 풀고 ② 카드의 첫 방향성(TowardTarget) 효과의 목표를 바라보게 돌린 뒤(TurnToward)
    // ③ 방향 고정은 이 카드의 Card.LocksFacing로 바로 정한다 — 이전 행동이 고정이었어도 이 행동이 고정이 아니면 풀려 있다.
    // 방향은 행동 확정 시점의 보드로 정해지고, 효과를 실행할 때는 바꾸지 않는다(facing만 읽는다).
    // 다음 행동 판정 뒤에 불러야 한다 — 조건부 행동 판정(HasTargetIfUsedNow)은 이 방향이 정해지기 전 보드로 한다.
    public void PlanFacing(AutoPiece piece, Card card)
    {
        piece.facingLocked = false;

        Vector2Int origin = FindPiecePos(piece);
        if (origin.x >= 0 && card != null)
        {
            CardEffect facingEffect = card.effects.Find(e => e.facingMode == FacingMode.TowardTarget);
            if (facingEffect != null)
            {
                Vector2Int target = ResolveFacingTarget(facingEffect with { caster = piece }, origin); // 원본 CardEffect는 바꾸지 않는다
                if (target.x >= 0) TurnToward(piece, origin, target);
            }
        }

        piece.facingLocked = card != null && card.LocksFacing;
    }

    // 사거리 안 상대 기물 중 체력이 가장 낮은 기물. 사거리 안에 올바른 대상이 없으면 이동 효과만 가장 가까운 상대 쪽으로
    // 다가가고(ResolveApproachCell), 그 외 효과는 대상 없음(-1,-1)이라 무시된다.
    Vector2Int ResolveLowestHPTarget(CardEffect effect, Vector2Int origin)
    {
        Piece caster = GetPieceAt(origin);
        int targetTeam = caster != null && caster.teamID == 0 ? 1 : 0;

        // 지금 처리 중인 효과의 범위(effectRange, 비어 있으면 이동 범위).
        List<Vector2Int> candidates = CellsInBoard(origin, effect.GetRangeOffsets(caster));

        Vector2Int target = PickLowestHPTarget(candidates, targetTeam, effect, origin);
        if (target.x >= 0) return target;
        return effect.type == EffectType.Move ? ResolveApproachCell(candidates, origin, targetTeam) : new Vector2Int(-1, -1);
    }

    // candidates(보드 안 칸) 중 targetTeam 기물이 있는 칸에서 체력(hp)이 가장 낮은 칸을 고른다. 닿는 도발 기물이 있으면
    // 그중에서만 고른다. 체력이 같으면 casterPos에서 더 가까운 칸(거리 동률은 IsCloser 규칙), 대상이 없으면 (-1,-1).
    // 이동 효과는 이동공격 도착 칸이 없는(도달 불가) 기물을 고르지 않는다.
    Vector2Int PickLowestHPTarget(List<Vector2Int> candidates, int targetTeam, CardEffect effect, Vector2Int casterPos)
    {
        Vector2Int teamDir = TeamDirection(targetTeam == 0 ? 1 : 0);
        int lowestHP = int.MaxValue;
        Vector2Int target = new Vector2Int(-1, -1);

        foreach (Vector2Int pos in PrioritizeTauntTargets(candidates, targetTeam, effect, casterPos))
        {
            Piece p = GetPieceAt(pos);
            if (p == null || p.teamID != targetTeam || IsMoveAttackUnreachable(effect, casterPos, pos)) continue;
            if (p.hp < lowestHP || (p.hp == lowestHP && IsCloser(casterPos, pos, target, teamDir)))
            {
                lowestHP = p.hp;
                target = pos;
            }
        }

        return target;
    }

    // 사거리 안 상대 기물 중 가장 가까운 기물. 사거리 안에 올바른 대상이 없으면 이동 효과만 가장 가까운 상대 쪽으로
    // 다가가고(ResolveApproachCell), 그 외 효과는 대상 없음(-1,-1)이라 무시된다.
    Vector2Int ResolveNearestEnemyTarget(CardEffect effect, Vector2Int origin)
    {
        // 캐스터 자신의 teamID 기준으로 상대팀을 동적으로 계산 — teamID==1(적)뿐 아니라
        // teamID==0(자동행동 아군)이 이 로직을 써도 올바르게 반대팀을 노리게 하기 위함.
        Piece caster = GetPieceAt(origin);
        int targetTeam = caster != null && caster.teamID == 0 ? 1 : 0;

        // 지금 처리 중인 효과의 범위(effectRange, 비어 있으면 이동 범위).
        List<Vector2Int> candidates = CellsInBoard(origin, effect.GetRangeOffsets(caster));

        Vector2Int target = PickNearestTarget(candidates, targetTeam, effect, origin);
        if (target.x >= 0) return target;
        return effect.type == EffectType.Move ? ResolveApproachCell(candidates, origin, targetTeam) : new Vector2Int(-1, -1);
    }

    // candidates(보드 안 칸) 중 targetTeam 기물이 있는 칸에서 casterPos와 가장 가까운 칸(거리 동률은 IsCloser 규칙)을 고른다.
    // 닿는 도발 기물이 있으면 그중에서만 고른다. 이동 효과는 이동공격 도착 칸이 없는(도달 불가) 기물을 고르지 않는다.
    // 대상이 없으면 (-1,-1). 선택 상태를 건드리지 않으므로 연쇄 이동공격처럼 카드 처리 도중에도 쓸 수 있다.
    Vector2Int PickNearestTarget(List<Vector2Int> candidates, int targetTeam, CardEffect effect, Vector2Int casterPos)
    {
        Vector2Int teamDir = TeamDirection(targetTeam == 0 ? 1 : 0);
        Vector2Int target = new Vector2Int(-1, -1);

        foreach (Vector2Int pos in PrioritizeTauntTargets(candidates, targetTeam, effect, casterPos))
        {
            Piece p = GetPieceAt(pos);
            if (p == null || p.teamID != targetTeam || IsMoveAttackUnreachable(effect, casterPos, pos)) continue;
            if (IsCloser(casterPos, pos, target, teamDir))
                target = pos;
        }

        return target;
    }

    // 사거리 안에 올바른 대상이 없는 이동 효과의 대상 칸: 보드에서 가장 가까운 상대 쪽으로 가장 가까워지는 사거리 안 빈 칸.
    // 상대가 없거나 갈 수 있는 빈 칸이 없으면 (-1,-1) — 이동하지 않는다.
    Vector2Int ResolveApproachCell(List<Vector2Int> candidates, Vector2Int origin, int targetTeam)
    {
        Vector2Int nearestFoe = GetNearestPlayerPos(origin, targetTeam);
        Piece foe = GetPieceAt(nearestFoe);
        if (foe == null || foe.teamID != targetTeam) return new Vector2Int(-1, -1);

        Vector2Int teamDir = TeamDirection(targetTeam == 0 ? 1 : 0);
        Vector2Int best = new Vector2Int(-1, -1);
        foreach (Vector2Int cell in candidates)
        {
            if (GetPieceAt(cell) != null) continue; // 다른 기물이 있는 칸은 이동 후보에서 제외
            if (IsCloser(nearestFoe, cell, best, teamDir))
                best = cell;
        }
        return best;
    }

    // useColDamageAsDmg면 힘 전체 수치를 그대로 사용(이동공격 충돌 데미지와 동일 기준),
    // Damage 타입이면 dmg에 힘 Delta(영구 강화분 + 이번 전투 임시 버프분)를 가산
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

    // 기물을 대상으로 하는 효과인지 — 대상 칸에 기물이 없으면 무시된다(IsTargetMissing). 이동·소환·범위 중심처럼 칸이 대상인
    // 효과와, 손패·덱·자원 효과(대상 기물은 ApplyCardEffectNow가 따로 판정)는 제외한다.
    static bool NeedsPieceTarget(EffectType type) =>
        type is EffectType.Damage or EffectType.Heal or EffectType.Shield or EffectType.ApplyStatus or EffectType.Cleanse
            or EffectType.ColDamageUp or EffectType.BaseColDamageUp or EffectType.ShieldBonusUp or EffectType.BaseShieldBonusUp
            or EffectType.GrantChainMoveAttack or EffectType.AddCard;

    // 처리할 효과의 대상이 더 이상 존재하지 않는지 — 기물이 대상인 단일 대상 효과인데 실행 시점에 대상 칸에 살아 있는 기물이
    // 없으면 true(그 효과는 연출 없이 무시된다). useLastTarget이면 직전 효과의 대상과 같은 기물이어야 한다(그 칸에 다른 기물이
    // 들어와 있어도 무시). 로직이 동기라 앞 효과로 죽은 기물은 이미 칸에서 빠져 있다(ClearDeadPieceOccupancy).
    bool IsTargetMissing(CardEffect e, Vector2Int targetPos)
    {
        if (e.IsAreaLogic || !NeedsPieceTarget(e.type)) return false;
        if (targetPos.x < 0 || targetPos.x >= N || targetPos.y < 0 || targetPos.y >= M) return true;
        Piece p = GetPieceAt(targetPos);
        if (p == null || p.hp <= 0) return true;
        return e.useLastTarget && !ReferenceEquals(lastEffectTargetPiece, null) && p != lastEffectTargetPiece;
    }

    // 효과에 기록된 시전자(CardEffect.caster)의 현재 칸. 시전자가 없거나 보드에 없으면 (-1,-1).
    Vector2Int CasterPosOf(CardEffect effect) =>
        effect.caster != null ? FindPiecePos(effect.caster) : new Vector2Int(-1, -1);

    // 효과 하나를 실행한다. 시전자 칸은 선택 상태가 아니라 효과에 기록된 시전자(cardEffect.caster)의 현재 위치로 정한다.
    // 카드 사용 확정(에너지·카드 사용 시 유물)은 Board.UseCard에서 이미 끝났다.
    void ExecuteEffect(CardEffect cardEffect, Vector2Int targetPos)
    {
        Vector2Int casterPos = CasterPosOf(cardEffect);

        // 아래 기록이 lastEffectTargetPiece를 덮기 전에 판정한다.
        bool targetMissing = IsTargetMissing(cardEffect, targetPos);
        lastEffectTargetPos = targetPos;
        lastEffectTargetPiece = targetPos.x >= 0 && targetPos.x < N && targetPos.y >= 0 && targetPos.y < M
            ? GetPieceAt(targetPos) : null;

        // 대상이 더 이상 존재하지 않는 효과는 무시한다 — 호출부가 ScheduleNextCardEffect로 큐를 이어간다.
        // 무덤은 아래 PayGrave보다 먼저 빠져나가므로 차감되지 않는다.
        if (targetMissing) return;

        // 대상이 시전자 자신인 이동 효과는 무시한다(이동할 칸이 없다).
        if (cardEffect.type == EffectType.Move && cardEffect.targetlogic == TargetLogic.self) return;

        cardEffect = PayGrave(cardEffect);
        cardEffect = ApplyDiscardedHits(cardEffect);

        // 버려진 카드가 없어 타격 수가 0이면 스킵 — AttackPiece 타격 루프의 Mathf.Max(1, hitCount)가 0타를 1타로 바꾸지 않게.
        if (cardEffect.hitsPerDiscarded > 0 && cardEffect.hitCount <= 0) return;

        // 큐가 보드에 없는 시전자의 효과를 미리 건너뛰고(ProcessNextCardEffectStep) 모든 효과에 시전자가 기록되므로
        // 정상 경로에서는 생기지 않는다.
        if (casterPos.x < 0)
        {
            Debug.LogWarning($"[Board] 시전자가 없거나 보드에 없어 효과({cardEffect.type})를 건너뜁니다.");
            return;
        }

        ApplyCardEffectNow(cardEffect, casterPos, targetPos);
    }

    // 손패·덱 효과(드로우·버리기·덱으로·소멸)의 대상 기물 — 대상 칸의 기물이다. 전투 덱이 없는 기물(적·소환수)이나
    // 빈 칸이면 null이라 그 효과는 무시된다.
    Piece DeckOwnerAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) return null;
        Piece p = GetPieceAt(pos);
        return p != null && p.pieceDeck != null ? p : null;
    }

    // 화면에 띄우지 않은 기물의 손패·덱이 바뀌었을 때: 카드 오브젝트가 꺼져 있어 카드 연출 대신 보드 위 그 기물에 텍스트를 띄운다.
    void EnqueueDeckChangeText(Piece owner, string text)
    {
        EnqueueTextAnimation(owner, owner.StatusTextReaction(text, false, new Color(0.75f, 0.6f, 1f), textOnly: true));
    }

    // CardCanvas의 손패↔덱/버림/소멸 로직 메서드(HandtoDiscardCount 등)가 돌려준 "이동해야 할 카드"
    // 목록을 받아, 각 카드의 비주얼 이동(MoveCardVisualCor)을 motionQueue에 그대로 enqueue한다.
    // ShieldPiece/AttackPiece가 자기 애니메이션을 enqueue하는 것과 완전히 같은 패턴이라, 같은 카드효과가
    // 먼저 큐에 넣어둔 보드 애니메이션(예: 실드 연출)이 다 끝난 뒤에야 카드 이동 연출이 재생된다.
    // 마지막에 AlignCardsCor()를 하나 더 enqueue해서, 남은 손패의 재정렬(AlignCards)도 카드가 실제로
    // 날아가는 연출이 끝난 시점에 맞춰 재생되게 한다(그 전엔 이미 즉시 갱신된 handNumber 기준으로
    // 손패 조작은 정상 동작함 — CardCanvas.RefreshHandIndices 참고).
    // owner가 화면에 띄운 기물이 아니면 카드 연출 대신 그 기물에 "label N" 텍스트만 띄운다.
    void EnqueueCardMoves(Piece owner, List<(RectTransform card, Vector3 pos, Quaternion rot)> moves, float duration, string label)
    {
        if (moves.Count == 0) return;
        if (owner != CardCanvas.instance.ActivePiece)
        {
            EnqueueDeckChangeText(owner, $"{label} {moves.Count}");
            return;
        }
        foreach (var (card, pos, rot) in moves)
            motionQueue.Enqueue(CardCanvas.instance.MoveCardVisualCor(card, pos, rot, duration));
        motionQueue.Enqueue(CardCanvas.instance.AlignCardsCor());
        StartMotionQueue();
    }

    // EnqueueCardMoves의 소멸판 — 날아가는 대신 그 자리에서 사라진 뒤 소멸 더미로 옮기는 연출(ExileCardVisualCor)을 넣는다.
    void EnqueueCardExileFades(Piece owner, List<(RectTransform card, Vector3 pos, Quaternion rot)> moves, float duration)
    {
        if (moves.Count == 0) return;
        if (owner != CardCanvas.instance.ActivePiece)
        {
            EnqueueDeckChangeText(owner, $"카드 소멸 {moves.Count}");
            return;
        }
        foreach (var (card, _, _) in moves)
            motionQueue.Enqueue(CardCanvas.instance.ExileCardVisualCor(card, duration));
        motionQueue.Enqueue(CardCanvas.instance.AlignCardsCor());
        StartMotionQueue();
    }

    // CardEffect 하나를 targetPos 기준으로 실제로 적용한다. 카드 효과뿐 아니라 턴 효과·유물·상태이상 틱·처치 시·소환 시·
    // 기물 선택 결과도 전부 효과 큐(pendingEffects)를 거쳐 ExecuteEffect → 여기로 온다 — 게임의 모든 CardEffect가 이 함수
    // 하나로 실행된다. 시전자는 효과에 기록된 cardEffect.caster, casterPos는 그 기물의 현재 칸(ExecuteEffect가 구함).
    void ApplyCardEffectNow(CardEffect cardEffect, Vector2Int casterPos, Vector2Int targetPos)
    {
        Piece caster = cardEffect.caster;

        // Charge는 지속시간을 따로 추적하는 상태이상이 아니라 "시전자의 다음 카드 효과가 나올 때까지"만 유효한
        // 예고 연출이라, 매번 여기서 일단 꺼두고 cardEffect.type이 진짜 Charge일 때만(아래 case) 다시 켠다.
        // 엔진이 넣은 효과(executeOnCaster — 턴 효과·상태이상 틱 등)는 그 기물의 행동이 아니므로 끄지 않는다.
        if (!cardEffect.executeOnCaster)
            caster?.SetAnimBool("Charge", false);

        // 방향성(TowardTarget): 시전자가 목표 쪽을 바라본다(바라보는 방향 규칙은 FacingToward). 범위는 이 방향대로 펼쳐지므로
        // 범위 계산보다 먼저 한다. AutoPiece는 효과를 실행할 때가 아니라 다음 행동으로 정해질 때 이미 돌았다(PlanFacing).
        if (cardEffect.facingMode == FacingMode.TowardTarget && caster != null && caster is not AutoPiece)
            TurnToward(caster, casterPos, targetPos);

        if (cardEffect.targetlogic == TargetLogic.AllEnemiesInRange ||
            cardEffect.targetlogic == TargetLogic.AllAlliesInRange ||
            cardEffect.targetlogic == TargetLogic.AllPiecesInRange)
        {
            ExecuteAreaEffect(cardEffect, casterPos, targetPos);
            return;
        }

        switch (cardEffect.type)
        {
            case EffectType.Move:
            {
                if (caster != null && caster.IsMovementDisabled())
                {
                    // 카드의 이동이 막히면 그 카드의 남은 효과를 취소한다. 엔진이 넣은 Move(유물·턴 효과)가 막혀도
                    // 진행 중인 카드의 효과는 지우지 않는다.
                    if (!cardEffect.executeOnCaster) RemoveCardEffects();
                    // 막힌 기물 위에 "이동 불가!" 텍스트(이동 불가 상태 색, 파티클·사운드 없음)를 띄우고 흔든다.
                    EnqueueTextAnimation(caster, caster.StatusTextReaction("이동 불가!", false, new Color(0.6f, 0.6f, 0.6f), textOnly: true));
                    EnqueueBoardAnimation(ImmobileShakeCor(caster));
                    break;
                }
                MovePiece(casterPos, targetPos, cardEffect);
                break;
            }
            case EffectType.Damage:
            {
                int resolvedDmg = ResolveDamageWithColDamage(cardEffect, caster);
                // 다중 타격(hitCount/hitsPerDiscarded)도 한 번 호출 — 피해는 타격마다 따로 들어가고, 시전자 애니메이션은 1회만 재생된다.
                if (cardEffect.countsAsMoveAttack)
                    MoveAttackInPlace(casterPos, targetPos, resolvedDmg, cardEffect);
                else
                    AttackPiece(casterPos, targetPos, resolvedDmg, cardEffect, cardEffect.hitCount);
                break;
            }
            case EffectType.Heal:
            {
                int resolvedDmg = ResolveDamageWithColDamage(cardEffect, caster);
                HealPiece(casterPos, targetPos, resolvedDmg, cardEffect);
                break;
            }
            case EffectType.Shield:
            {
                int resolvedDmg = ResolveShieldWithBonus(cardEffect, caster);
                ShieldPiece(casterPos, targetPos, resolvedDmg, cardEffect);
                break;
            }
            case EffectType.SelfDamage:
                SelfDamagePiece(casterPos, cardEffect.dmg);
                break;
            case EffectType.Draw:
            {
                // 대상이 카드를 1장 드로우한다. 화면에 띄운 기물이면 드로우 연출, 아니면 그 기물에 텍스트.
                Piece owner = DeckOwnerAt(targetPos);
                if (owner == null) break;
                int drawn = CardCanvas.instance.DrawCard(owner);
                if (drawn > 0 && owner != CardCanvas.instance.ActivePiece)
                    EnqueueDeckChangeText(owner, $"드로우 {drawn}");
                break;
            }
            case EffectType.ApplyStatus:
            {
                ApplyStatusToTarget(caster, targetPos, cardEffect);
                break;
            }
            case EffectType.ColDamageUp:
            case EffectType.BaseColDamageUp:
            {
                Piece p = GetButtonScript(targetPos).GetPieceScript();
                if (p != null)
                {
                    // Base*는 colDamageBonus도 함께 올려 GetPieceData 저장을 거쳐 다음 전투로도 이어지게 한다(영구 강화).
                    p.AddColDamage(cardEffect.dmg, permanent: cardEffect.type == EffectType.BaseColDamageUp, showReaction: false);
                    EnqueueCasterReaction(caster, p, cardEffect, p.ColDamageUpReaction(cardEffect.dmg));
                    CardCanvas.instance?.RefreshAllCardViews();
                    p.ActionText(); // 적의 행동 예고 숫자(dmg + ColDamageDelta)가 턴 종료 패시브 등으로 오른 값을 따라가게
                }
                break;
            }
            case EffectType.ShieldBonusUp:
            case EffectType.BaseShieldBonusUp:
            {
                Piece p = GetButtonScript(targetPos).GetPieceScript();
                if (p != null)
                {
                    p.AddShieldBonus(cardEffect.dmg, permanent: cardEffect.type == EffectType.BaseShieldBonusUp, showReaction: false);
                    EnqueueCasterReaction(caster, p, cardEffect, p.ShieldBonusUpReaction(cardEffect.dmg));
                    CardCanvas.instance?.RefreshAllCardViews();
                }
                break;
            }
            case EffectType.RestoreEnergy:
            {
                // 코스트(에너지)는 기물별이 아니라 아군 전체가 공유하는 자원 — 즉시 회복하고(setter가 UI·사용 가능 여부 갱신) 연출만 큐잉.
                if (CardCanvas.instance != null)
                    CardCanvas.instance.currentenergy = Mathf.Min(CardCanvas.instance.currentenergy + cardEffect.dmg, CardCanvas.instance.maxenergy);
                if (caster != null && cardEffect.animTrigger != null)
                {
                    motionQueue.Enqueue(TriggerAnimCor(caster, cardEffect.animTrigger, cardEffect: cardEffect));
                    StartMotionQueue();
                }
                break;
            }
            // 손패 효과는 대상(대상 칸의 기물)의 손패에 적용한다. 전투 덱이 없는 기물이 대상이면 무시된다.
            case EffectType.DiscardHand:
            {
                Piece owner = DeckOwnerAt(targetPos);
                if (owner == null) break;
                // 손패가 요청 수보다 적으면 있는 만큼만 버려지므로 moves.Count가 곧 실제로 버려진 수
                var discardMoves = CardCanvas.instance.HandtoDiscardCount(owner, cardEffect.dmg);
                discardedThisCard += discardMoves.Count;
                EnqueueCardMoves(owner, discardMoves, 0.2f, "카드 버림");
                break;
            }
            case EffectType.ShuffleHandToDeck:
            {
                Piece owner = DeckOwnerAt(targetPos);
                if (owner == null) break;
                EnqueueCardMoves(owner, CardCanvas.instance.HandtoDeckCount(owner, cardEffect.dmg), 0.2f, "카드 덱으로");
                break;
            }
            case EffectType.ExileHand:
            {
                Piece owner = DeckOwnerAt(targetPos);
                if (owner == null) break;
                EnqueueCardExileFades(owner, CardCanvas.instance.HandtoExileCount(owner, cardEffect.dmg), 0.25f);
                break;
            }
            case EffectType.HandToDeckTop:
            {
                Piece owner = DeckOwnerAt(targetPos);
                if (owner == null) break;
                EnqueueCardMoves(owner, CardCanvas.instance.HandtoDeckTop(owner, cardEffect.dmg), 0.2f, "카드 덱 위로");
                break;
            }
            case EffectType.AddCard:
            {
                // targetPos 칸의 기물 덱에 넣는다 — self 효과(FetchAttackCard 등)는 시전자 칸, 적 행동은 ResolveTargetPos가 풀어 둔 대상.
                // 시전자 애니메이션은 트리거가 있을 때만 재생한다 — 트리거 없이 시전자를 넘기면 오지 않을 애니메이션 이벤트를
                // 기다리느라(RunImpactCor) 카드 등장이 AnimationEventFallbackTimeout만큼 늦어진다.
                Piece animCaster = cardEffect.animTrigger != null ? caster : null;
                Piece recipient = targetPos.x >= 0 && targetPos.x < N && targetPos.y >= 0 && targetPos.y < M ? GetPieceAt(targetPos) : null;
                IEnumerator reaction = AddCardReaction(recipient, cardEffect.addCardID, cardEffect.addCardZone);
                if (reaction == null) break;
                motionQueue.Enqueue(PlayCasterAndTargetReaction(animCaster, cardEffect.animTrigger, reaction, cardEffect));
                StartMotionQueue();
                break;
            }
            case EffectType.Cleanse:
            {
                Piece cleanseTarget = GetButtonScript(targetPos)?.GetPieceScript();
                var cleanseResult = CleanseTarget(cleanseTarget, cardEffect); // 즉시 적용
                IEnumerator cleanseReaction = cleanseResult.HasValue
                    ? cleanseTarget.StatusTextReaction(cleanseResult.Value.text, cleanseResult.Value.isBuff, cleanseResult.Value.color)
                    : Parallel(); // 지울 게 없었으면 아무 것도 안 하는 반응
                EnqueueCasterReaction(caster, cleanseTarget, cardEffect, cleanseReaction);
                break;
            }
            case EffectType.Charge:
            {
                // 텔레그래프 포즈: 다른 애니메이션을 무시하고 즉시 켜지며, 함수 맨 위에서 항상 꺼둔 걸
                // 여기서만 다시 켠다 — 이 기물에게 다음 CardEffect가 뭐가 됐든 적용되는 순간 자동으로 꺼짐.
                caster?.SetAnimBool("Charge", true);
                break;
            }
            case EffectType.Stun:
            {
                // 상태 자체는 의도적인 무효과(기절 소모 표시일 뿐) — 캐스터 애니메이션만 재생.
                // 실제 기절 포즈(Stun 토글)는 StunEffect.OnApply/OnRemove가 지속시간 기준으로 관리한다.
                // stunSkip 사운드도 캐스터 애니메이션의 OnAnimationEvent 시점에 맞춰 재생한다.
                motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, StunSkipReaction(), cardEffect));
                StartMotionQueue();
                break;
            }
            case EffectType.Summon:
                SummonPieceAt(cardEffect, casterPos, targetPos);
                break;
            case EffectType.ReduceCost:
                currentActiveCard.Cost = Mathf.Max(0, currentActiveCard.Cost - cardEffect.dmg);
                currentActiveCard.RefreshView();
                break;
            case EffectType.GrantChainMoveAttack:
            {
                // 버프를 받는 대상 칸의 기물(self 카드라 보통 시전자 자신)이 애니메이션도 재생한다.
                Piece buffTarget = GetButtonScript(targetPos).GetPieceScript();
                if (buffTarget != null)
                {
                    buffTarget.AddStatusEffect(new ChainMoveAttackEffect()); // 즉시 적용
                    // 다른 버프 카드(ColDamageUp 등)와 동일하게 animTrigger 재생 + 상태 텍스트/파티클/사운드를
                    // 캐스터의 OnAnimationEvent 시점에 맞춰 재생 — 이게 빠져 있어서 카드를 써도 아무 연출이 없었음.
                    EnqueueCasterReaction(buffTarget, buffTarget, cardEffect,
                        buffTarget.StatusTextReaction("연쇄 이동공격", true, new Color(1f, 0.27f, 0.27f)));
                }
                break;
            }
            case EffectType.GrantSummonColDamage:
            {
                if (caster != null)
                {
                    caster.summonColDamagePending += cardEffect.dmg;
                    // 다른 버프 카드와 동일하게 animTrigger 재생 + 상태 텍스트/파티클/사운드 표시.
                    EnqueueCasterReaction(caster, caster, cardEffect,
                        caster.StatusTextReaction($"다음 소환 힘 +{cardEffect.dmg}", true, new Color(1f, 0.27f, 0.27f)));
                }
                break;
            }
            case EffectType.GrantSummonMaxHp:
            {
                // GrantSummonColDamage와 동일하게 자체 버프 애니메이션/텍스트를 재생한다 — 두 효과가
                // 체인으로 이어져도(SummonMasteryCard/SummonGrowthCard) 각자 수치를 분명히 보여주기 위함.
                if (caster != null)
                {
                    caster.summonMaxHpPending += cardEffect.dmg;
                    EnqueueCasterReaction(caster, caster, cardEffect,
                        caster.StatusTextReaction($"다음 소환 체력 +{cardEffect.dmg}", true, new Color(1f, 0.27f, 0.27f)));
                }
                break;
            }
            case EffectType.AddGrave:
            {
                // 무덤 증가는 즉시 반영(다음 효과의 graveCost 판정에 바로 쓰임) — 연출만 큐잉한다.
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

    void SummonPieceAt(CardEffect cardEffect, Vector2Int casterPos, Vector2Int targetPos)
    {
        if (targetPos.x < 0 || targetPos.y < 0) return;

        PieceInfo info = cardEffect.summonPieceInfo;
        if (info == null) { Debug.LogError("[Board] Summon 효과에 summonPieceInfo가 없습니다."); return; }

        // 점유된 targetPos(적 위치) 자신을 중심으로 사거리를 다시 잡는다 — 시전자 칸 기준으로
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
            Debug.LogError($"[Board] 소환 실패: '{info.PieceName}' 을(를) PieceDatabase에서 찾을 수 없습니다(PieceInfo 이름 기준).");
            return;
        }

        GameObject pieceObj = Instantiate(prefab);
        ApplySpawnOccupancy(spawnPos, pieceObj); // 소환 시 효과가 이 효과 바로 다음(카드의 이후 효과보다 먼저)에 실행되게 큐 맨 앞에 들어간다
        Piece pieceScript = pieceObj.GetComponent<Piece>();
        if (pieceScript is AutoPiece summonedAuto) summonedAuto.PlanNextMove(); // 소환된 자동 기물의 첫 행동(조건부 행동 판정)

        // 소환 카드를 실제로 쓴 시전자 — SummonMasteryCard가 이 시전자에게 쌓아둔 다음 소환 보너스가
        // 있으면 소모한다(적 소환 카드가 먼저 가로채지 않도록 시전자 개인에게 귀속).
        Piece caster = cardEffect.caster;
        bool hasSummonBuff = caster != null && (caster.summonColDamagePending > 0 || caster.summonMaxHpPending > 0);

        // 공격(AttackPiece)과 같이 효과 처리 시점에 시전자를 소환수가 나타날 칸 쪽으로 돌려둔다.
        if (caster != null && spawnPos != casterPos)
            caster.transform.rotation = Quaternion.LookRotation(GetButtonScript(spawnPos).Piecelocation - GetButtonScript(casterPos).Piecelocation);

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
            reactions.Add(newPiece.StatusTextReaction($"힘 +{delta}", true, new Color(1f, 0.27f, 0.27f)));
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

    // 동반되는 공격/힐/실드 없이 상태이상만 단독으로 거는 카드(ApplyStatus)에서 쓰는 경로. 캐스터가
    // 자신의 animTrigger를 재생하고, 그 OnAnimationEvent 시점에 모든 대상의 상태이상 반응이 함께 뜬다.
    void ApplyStatusToTarget(Piece caster, Vector2Int targetPos, CardEffect cardEffect)
    {
        // 유효하지 않은 targetPos여도(예: 방어적 가드) 캐스터 애니메이션 자체는 그대로 재생한다 —
        // 빈 리스트를 넘기면 반응은 없이 캐스터 트리거만 재생됨(기존 동작과 동일).
        var targets = (targetPos.x >= 0 && targetPos.y >= 0) ? new List<Vector2Int> { targetPos } : new List<Vector2Int>();
        ApplyStatusToTarget(caster, targets, cardEffect);
    }

    // rangeOrigin: 애니메이션 중 범위 표시의 기준 칸(범위 효과에서 시전자 칸이 아닌 중심을 쓸 때만).
    void ApplyStatusToTarget(Piece caster, List<Vector2Int> targets, CardEffect cardEffect, Vector2Int? rangeOrigin = null)
    {
        var reactions = new List<IEnumerator>();
        foreach (Vector2Int pos in targets)
        {
            Piece target = GetButtonScript(pos).GetPieceScript();
            StatusEffect effect = ApplyStatusEffect(target, cardEffect); // 즉시 적용
            if (effect != null)
                reactions.Add(target.StatusTextReaction(effect.DisplayName, effect.IsBuff, effect.EffectColor));
        }
        // 시전자 자신 하나에게만 거는 상태는 시전자 강화류라 같은 종류가 이어지면 묶여 재생된다(EnqueueCasterReaction).
        // 그 외(다른 기물·범위)는 대상 쪽 범위 표시가 있어 하나씩 재생한다.
        IEnumerator reaction = Parallel(reactions.ToArray());
        Piece soleTarget = targets.Count == 1 && rangeOrigin == null ? GetButtonScript(targets[0]).GetPieceScript() : null;
        EnqueueCasterReaction(caster, soleTarget, cardEffect, reaction,
            PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, reaction, cardEffect, originPos: rangeOrigin));
    }

    // cardEffect에 statusEffectType이 설정돼 있으면 target에게 상태이상을 즉시 걸고 그 효과를 반환한다
    // (없으면 null) — GetHeal/GetShield/GetDamage와 동일한 "즉시 실행되는 상태 변경" 역할. 텍스트/파티클/
    // 사운드는 반환된 효과 정보로 호출부가 Piece.StatusTextReaction을 통해 원하는 시점에 재생한다.
    StatusEffect ApplyStatusEffect(Piece target, CardEffect cardEffect)
    {
        if (target == null || cardEffect == null || cardEffect.statusEffectType == StatusEffectType.None) return null;
        StatusEffect effect = CreateStatusEffect(cardEffect);
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
        foreach (StatusEffect effect in new List<StatusEffect>(target.activeEffects))
        {
            if (effect.IsBuff != cardEffect.cleanseBuffs) continue;
            target.RemoveStatusEffect(effect);
            removedAny = true;
        }
        if (!removedAny) return null;
        return (cardEffect.cleanseBuffs ? "무효화" : "정화", cardEffect.cleanseBuffs, new Color(0.6f, 0.85f, 1f));
    }

    // source의 statusEffectType·statusDuration·statusPower(와 종류별 추가 필드 — NextMoveAttackStatus는 onMoveAttackHitEffect,
    // TurnEffect는 turnPhase·onTurnEffect)로 상태를 만든다. 보드 상태를 쓰지 않는 순수 생성이라 static — 표시 문구가 걸릴
    // 상태의 이름을 얻을 때도 쓴다(NextMoveAttackStatusEffect.InflictLabel).
    public static StatusEffect CreateStatusEffect(CardEffect source)
    {
        if (source == null) return null;
        int duration = source.statusDuration;
        int power = source.statusPower;
        return source.statusEffectType switch
        {
            StatusEffectType.Poison             => new PoisonEffect(duration, power),
            StatusEffectType.Burning            => new BurningEffect(duration, power),
            StatusEffectType.Regen              => new RegenEffect(duration, power),
            StatusEffectType.Stun               => new StunEffect(duration),
            StatusEffectType.Strengthen         => new StrengthenEffect(duration, power),
            StatusEffectType.Weaken             => new WeakenEffect(duration, power),
            StatusEffectType.TurnEffect         => source.onTurnEffect != null ? new TurnEffect(source.turnPhase, source.onTurnEffect, duration) : null,
            StatusEffectType.Thorn              => new ThornEffect(duration, power),
            StatusEffectType.MovementDisabled   => new MovementDisabledEffect(duration),
            StatusEffectType.Vulnerable         => new VulnerableEffect(duration, power),
            StatusEffectType.Taunt              => new TauntEffect(duration),
            StatusEffectType.NextMoveAttackDamage     => new NextMoveAttackDamageEffect(duration, power),
            StatusEffectType.NextMoveAttackMultiplier => new NextMoveAttackMultiplierEffect(duration, power),
            StatusEffectType.NextMoveAttackStatus     => new NextMoveAttackStatusEffect(duration, source.onMoveAttackHitEffect),
            StatusEffectType.ExileOnUse         => new ExileOnUseEffect(duration),
            StatusEffectType.WoundOnHit         => new WoundOnHitEffect(duration),
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

    // 범위 효과(All*InRange)가 닿는 칸(보드 안 절대 좌표). 선택 상태를 읽지 않아 다른 스크립트에서도 쓸 수 있다.
    // - 아군 위치 고정 공격(lockOnAllyPositions): 시전자(effect.caster, AutoPiece)가 플레이어 턴 시작 때 잠가 둔 칸.
    // - Fixed: 시전자 칸(casterPos) 중심.
    // - MouseCentered: center(클릭·호버한 칸) 중심.
    // 방향성(TowardTarget) 범위 효과는 범위가 시전자가 바라보는 방향(effect.caster.facing)으로 회전한다(CardEffect.GetRangeOffsets).
    // facingOverride: 바라보는 방향을 바꾸지 않고 다른 방향으로 미리 보여줄 때(마우스 호버 미리보기·행동 조건 판정)만 넘긴다.
    // rangeOrigin: 범위를 펼친 기준 칸(고정 공격이면 casterPos) — 행동 연출의 범위 표시 기준. effectRange가 없으면 빈 목록.
    public static List<Vector2Int> GetAreaEffectCells(CardEffect effect, Vector2Int casterPos, Vector2Int center, out Vector2Int rangeOrigin, Vector2Int? facingOverride = null)
    {
        rangeOrigin = casterPos;
        var cells = new List<Vector2Int>();
        Board board = instance;
        if (board == null || effect == null || effect.effectRange == null) return cells;

        if (effect.lockOnAllyPositions)
        {
            if (effect.caster is AutoPiece lockOwner && lockOwner.lockedTargetCells != null)
                cells.AddRange(board.CellsInBoard(Vector2Int.zero, lockOwner.lockedTargetCells));
            return cells;
        }

        List<Vector2Int> offsets = effect.GetRangeOffsets(effect.caster, facingOverride);
        if (effect.areaTargetMode == AreaTargetMode.MouseCentered)
            rangeOrigin = center;

        return board.CellsInBoard(rangeOrigin, offsets);
    }

    void ExecuteAreaEffect(CardEffect cardEffect, Vector2Int casterPos, Vector2Int center)
    {
        if (cardEffect.effectRange == null) return;

        // 아군/적 판정은 효과에 기록된 시전자(cardEffect.caster)의 진영을 기준으로 한다 — 위치가 아니라 기물
        // 참조라 MouseCentered AoE처럼 시전자 칸을 선택하지 않는 카드에서도 정확하다.
        Piece caster = cardEffect.caster;
        int userTeam = caster.teamID;
        int targetTeam = cardEffect.targetlogic == TargetLogic.AllEnemiesInRange
            ? (userTeam == 0 ? 1 : 0)
            : userTeam;
        var targets = new List<Vector2Int>();
        // rangeOrigin: 애니메이션 중 범위 표시의 기준 칸 — 실제로 범위를 펼친 중심과 같게 넘긴다. MouseCentered면 클릭한 칸이라
        // 시전자 칸과 다르다(넘기지 않으면 TriggerAnimCor가 시전자 칸 기준으로 그려 실제로 맞은 칸과 어긋남).
        // 방향성 범위 효과의 방향은 시전자의 facing이다 — 플레이어는 ApplyCardEffectNow가 효과 실행 직전에, AutoPiece는 다음 행동으로
        // 정해질 때(PlanNextMove) 이미 바꿔 두었다.
        List<Vector2Int> cells = GetAreaEffectCells(cardEffect, casterPos, center, out Vector2Int rangeOrigin);

        foreach (Vector2Int pos in cells)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            if (cardEffect.targetlogic != TargetLogic.AllPiecesInRange && p.teamID != targetTeam) continue;
            targets.Add(pos);
        }

        // 고정 공격·AutoPiece의 방향성 범위 공격(예고한 방향 그대로 친다)을 전원이 피했으면 AreaAttackPiece가 아무 연출 없이
        // 끝나므로, 헛스윙 + 범위 표시만 재생해 "피했다"는 피드백을 준다.
        if ((cardEffect.lockOnAllyPositions || (caster is AutoPiece && cardEffect.RotatesRangeWithFacing)) && targets.Count == 0 && caster != null)
        {
            motionQueue.Enqueue(TriggerAnimCor(caster, cardEffect.animTrigger, cardEffect: cardEffect));
            StartMotionQueue();
            return;
        }

        switch (cardEffect.type)
        {
            case EffectType.Damage:
                AreaAttackPiece(casterPos, targets, ResolveDamageWithColDamage(cardEffect, caster), cardEffect, rangeOrigin);
                break;
            case EffectType.Shield:
                AreaShieldPiece(caster, targets, ResolveShieldWithBonus(cardEffect, caster), cardEffect, rangeOrigin);
                break;
            case EffectType.Heal:
                AreaHealPiece(caster, targets, cardEffect.dmg, cardEffect, rangeOrigin);
                break;
            case EffectType.ApplyStatus:
                ApplyStatusToTarget(caster, targets, cardEffect, rangeOrigin);
                break;
            case EffectType.AddGrave:
                foreach (Vector2Int pos in targets)
                    GetButtonScript(pos).GetPieceScript()?.AddGrave(cardEffect.dmg);
                CardCanvas.instance?.UpdateCardInteractability();
                break;
            case EffectType.AddCard:
            {
                var reactions = new List<IEnumerator>();
                foreach (Vector2Int pos in targets)
                {
                    IEnumerator r = AddCardReaction(GetButtonScript(pos).GetPieceScript(), cardEffect.addCardID, cardEffect.addCardZone);
                    if (r != null) reactions.Add(r);
                }
                motionQueue.Enqueue(PlayCasterAndTargetReaction(caster, cardEffect.animTrigger, Parallel(reactions.ToArray()), cardEffect, rangeOrigin));
                StartMotionQueue();
                break;
            }
        }
    }

    // AddCard: recipient의 전투 덱에 카드를 count장 넣고(로직 즉시 — 원본 덱은 그대로), 연출을 돌려준다. 화면에 표시 중인
    // 기물이면 카드 등장 연출을 한 장씩 차례로, 아니면 보드 위 그 기물에 "카드 +N" 텍스트(카드는 그 기물로 전환할 때 보인다).
    // 전투 덱이 없는 기물(적, 덱 없는 소환수)이면 null.
    IEnumerator AddCardReaction(Piece recipient, string cardID, CardPositionZone zone, int count = 1)
    {
        var reveals = new List<IEnumerator>();
        for (int i = 0; i < count; i++)
        {
            IEnumerator reveal = CardCanvas.instance?.AddCardDuringCombat(recipient, cardID, zone);
            if (reveal != null) reveals.Add(reveal);
        }
        if (reveals.Count == 0) return null;
        IEnumerator revealAll = RunSequentially(reveals);
        if (recipient == CardCanvas.instance.ActivePiece) return revealAll;
        return Parallel(revealAll, recipient.StatusTextReaction($"카드 +{reveals.Count}", false, new Color(0.75f, 0.6f, 1f), textOnly: true));
    }

    public void CancelCardUsage()
    {
        RemoveCardEffects();
        currentActiveCard = null;
        ResetBoardAfterCardUse();
    }

    void ResetBoardAfterCardUse()
    {
        ClearHoverRange();
        currentCardCaster = null;
        lastEffectTargetPos = new Vector2Int(-1, -1);
        lastEffectTargetPiece = null;
        discardedThisCard = 0;
        ClearSelectedButton();
        CancelPieceSelection();
        ClearUseEligibilityPreview();
        SetCasterIndicator(CardCanvas.instance?.ActivePiece, false);
        // 사용 확정(Board.UseCard)에서 켠 effectApplied를 카드 로직이 끝나는 시점에 끈다 — 그 뒤에 새로 집은 카드의
        // 취소 가드(RevertNowUsingCardToHeld/CancelCardUsage)가 이전 카드의 값으로 막히지 않게.
        effectApplied = false;
    }

    void FinishCardUsage()
    {
        if (currentActiveCard != null && currentActiveCard.blocksMovementAfterUse && casterPiece != null)
            casterPiece.movedThisTurn = true;
        // FinishUseCard 시점엔 이 카드의 마지막 효과가 유발한 연출이 이미 전부 motionQueue에 들어가
        // 있다(로직은 동기, 연출만 큐잉 — Stage 2 참고) — 여기서 신호용 항목을 뒤이어 넣어두면 FIFO
        // 특성상 정확히 "이 카드가 유발한 연출이 다 끝난 시점"에 실행된다. 손패에서 쓴 카드만
        // CardCanvas에 대기 항목을 남기므로, 그 경우에만 신호를 예약한다(시전자는 아래 ResetBoardAfterCardUse에서 지워지므로 그 전에 판정).
        bool needsFlightSignal = IsHandCard;
        CardCanvas.instance.FinishUseCard();
        if (needsFlightSignal)
        {
            motionQueue.Enqueue(SignalCardAnimationsDone());
            StartMotionQueue();
        }
        ResetBoardAfterCardUse();
        // 위에서 이 카드의 설정을 다 읽은 뒤 비운다 — 남겨 두면 IsResolvingEffects가 계속 true가 되고, 이후 타이밍 효과가
        // 이 카드를 기준으로 카드 사용을 다시 확정(에너지 차감·카드 사용 시 유물)하게 된다.
        currentActiveCard = null;
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
            {
                // DiscardHand와 같은 버리기 로직·연출(CardCanvas.DiscardCards) — 선택 패널은 화면에 띄운 기물의 더미만 보여 준다.
                // 버려졌을 때 효과는 여기서 큐 맨 앞에 들어가 아래 ScheduleNextCardEffect가 이 카드의 다음 효과보다 먼저 처리한다.
                Piece owner = CardCanvas.instance.ActivePiece;
                var discardMoves = CardCanvas.instance.DiscardCards(owner, selected);
                discardedThisCard += discardMoves.Count;
                EnqueueCardMoves(owner, discardMoves, 0.2f, "카드 버림");
                break;
            }
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

    // RequestPieceSelection으로 보드에서 직접 고른 기물들 각각을 시전자로 한 effect 사본을 효과 큐 맨 앞에 넣는다
    // (InsertEffectNext — executeOnCaster라 큐가 입력 모드를 보지 않고 각 기물 칸에서 바로 실행한다).
    void ApplyPieceSelectionEffect(CardEffect effect, List<Piece> selected)
    {
        // 기물 선택 카드는 기물을 다 고른 지금 사용 확정한다(Board.UseCard가 미뤄 둠) — 카드 사용 시 유물이 아래 고른 기물들의
        // 효과보다 먼저 실행되도록 먼저 큐 앞에 넣는다. 고를 수 있는 기물이 없어 0명이어도 카드는 사용된 것이다.
        if (IsHandCard && !effectApplied)
        {
            Vector2Int casterPos = CasterPosOf(effect);
            CommitHandCardUse(casterPos, casterPos);
        }
        effect = PayGrave(effect); // 선택한 기물 수와 무관하게 한 번만 차감(카드 시전자가 지불)
        // 무덤은 위에서 이미 냈으므로 사본에서는 비용을 지운다(큐는 사본의 시전자 = 선택된 기물에게 무덤을 요구하므로).
        CardEffect perPiece = effect with { graveCost = 0, consumeAllGrave = false, pieceSelectCount = 0 };
        foreach (var piece in selected)
            InsertEffectNext(perPiece, piece);
        ScheduleNextCardEffect();
    }
}
