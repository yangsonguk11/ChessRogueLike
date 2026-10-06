using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    void MovePiece(Vector2Int pos1, Vector2Int pos2, CardEffect cardEffect = null)
    {
        Button button1script = GetButtonScript(pos1);
        Button button2script = GetButtonScript(pos2);

        Piece movingPiece = button1script.GetPieceScript();
        if (movingPiece != null && movingPiece.teamID == 0)
            movingPiece.movedThisTurn = true;

        if (button1script.GetPiece() != null)
        {
            GameObject piece2 = button2script.GetPiece();
            if (piece2)
            {
                Piece p1 = button1script.GetPiece().GetComponent<Piece>();
                Piece p2 = piece2.GetComponent<Piece>();
                // 아군 충돌이거나 이동공격 도착 칸이 없으면(MoveAttack false) 이동 실패 — 시전자는 원래 칸에 남는다
                if (p1.teamID != p2.teamID && !(cardEffect?.noMoveAttack ?? false))
                    MoveAttack(p1, p2, button1script, button2script, cardEffect);
            }
            else
            {
                GameObject movingObj = ApplyMoveOccupancy(button1script, button2script);
                motionQueue.Enqueue(MovePieceWithAnim(movingObj, button1script, button2script, 1f, cardEffect?.animTrigger, cardEffect));
                StartMotionQueue();
            }
        }

        ClearSelectedButton();
        button1script.SelectedFalse();
        button2script.SelectedFalse();
    }

    // 이동공격 도착 칸을 찾지 못하면(후보 전부 막힘) false를 반환해 호출부가 일반 이동 실패와 동일하게 처리하게 함.
    bool MoveAttack(Piece pScript1, Piece pScript2, Button bScript1, Button bScript2, CardEffect cardEffect = null)
    {
        Vector2Int adjacentPos = GetAdjacentLocation(bScript1.GetLocation(), bScript2.GetLocation());
        if (adjacentPos.x < 0) return false; // 도착할 칸이 없음: 공격 취소, 이동 실패로 처리

        // 공격자가 인접 칸까지 이동 — 애니메이션이 재생되기 전에 점유부터 동기로 확정해서,
        // 이 카드효과가 끝나는 즉시(다음 카드가 예약되더라도) 최신 보드 상태를 참조할 수 있게 한다.
        Button adjacentButton = GetButtonScript(adjacentPos);
        GameObject attackerObj = ApplyMoveOccupancy(bScript1, adjacentButton);

        // 목적지(impactPos)에 적이 있어 그 직전 칸(adjacentPos)까지만 이동 — "부딪혀서 멈춘" 펀치 이펙트 재생.
        // 코루틴은 만들기만 하고, 큐에 넣는 시점은 ResolveMoveAttackHit이 정한다(피해 처리 다음, 공격 연출 앞).
        IEnumerator approach = MovePieceWithAnim(attackerObj, bScript1, adjacentButton, 1f, "Move", bumpOnArrival: true);
        ResolveMoveAttackHit(pScript1, adjacentPos, pScript2, bScript2.GetLocation(), pScript1.colDamage, cardEffect, approach);
        return true;
    }

    // 이동공격 판정 공격(CardEffect.countsAsMoveAttack): 다가가지 않고 그 자리에서 이동공격으로 판정되는 타격.
    // 대상이 없거나(헛스윙) 같은 편이면 이동공격이 아니므로 일반 공격(AttackPiece)과 똑같이 처리한다 — 버프도 소모하지 않는다.
    void MoveAttackInPlace(Vector2Int casterPos, Vector2Int targetPos, int dmg, CardEffect cardEffect)
    {
        Piece attacker = GetPieceAt(casterPos);
        Piece defender = targetPos.x >= 0 && targetPos.y >= 0 ? GetPieceAt(targetPos) : null;
        if (attacker == null || defender == null || defender.teamID == attacker.teamID)
        {
            AttackPiece(casterPos, targetPos, dmg, cardEffect, cardEffect?.hitCount ?? 1);
            return;
        }
        ResolveMoveAttackHit(attacker, casterPos, defender, targetPos, dmg, cardEffect, approach: null);
    }

    // 소모된 다음 이동공격 버프 텍스트 사이 간격(초). 상태 텍스트는 같은 자리에서 생겨 위로 떠오르므로, 앞 텍스트가 조금
    // 올라가고 흐려진 뒤에 다음 텍스트를 띄워 겹쳐 보이지 않게 한다.
    const float ConsumedBuffTextInterval = 0.4f;

    IEnumerator WaitSecondsCor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    // 이동공격의 타격부 — 실제 이동공격(MoveAttack, approach = 다가가는 연출)과 이동공격 판정 공격(MoveAttackInPlace,
    // approach = null)이 공유한다. AttackPiece와 같은 타격 루프 구조: 타격마다 피해/상태이상을 즉시 적용하고 그 타의
    // 반응을 모아뒀다가, hitCount가 1이면 1회 반응, 2 이상이면 대상마다 MultiHitReactionCor로 재생한다.
    // attackerPos: 공격자가 실제로 서 있는 칸(이동공격이면 도착 칸 adjacentPos, 판정 공격이면 시전자 칸).
    // 다음 이동공격 버프 소모, 연쇄(OnMoveAttackPerformed), 가시 반격은 타격 수와 무관하게 이동공격 1회당 한 번씩이다.
    void ResolveMoveAttackHit(Piece attacker, Vector2Int attackerPos, Piece defender, Vector2Int impactPos, int baseDmg,
        CardEffect cardEffect, IEnumerator approach)
    {
        bool moved = approach != null;

        // 다음 이동공격 버프(가산·배율)는 이 이동공격의 모든 타에 적용된다.
        MoveAttackBonus bonus = attacker.ConsumeNextMoveAttackBonus();
        int dmg = bonus.Apply(baseDmg);

        // 스플래시 기준 칸: 실제 이동공격은 도착 칸, 판정 공격은 대상 바로 앞의 "가상 도착 칸" — 멀리서 쳐도 스플래시가
        // 대상 앞에서 펼쳐지게 한다(이미 붙어 있으면 시전자 칸과 같다).
        Vector2Int anchor = moved ? attackerPos : impactPos - GetSnappedDirection(attackerPos, impactPos, true);
        // DirectionalAttackCard와 동일하게, 기준 칸→대상 방향으로 moveAttackRange를 회전
        Vector2Int attackDir = GetSnappedDirection(anchor, impactPos, true);
        List<Vector2Int> moveAttackOffsets = RotateOffsets(attacker.GetMoveAttackRange(), attackDir);
        bool isAreaAttack = !(moveAttackOffsets.Count == 1 && moveAttackOffsets[0] == Vector2Int.zero);
        string attackTrigger = isAreaAttack ? "AreaAttack" : "Attack";

        // 공격 범위 표시용 — TriggerAnimCor가 기준 칸(anchor)·방향(attackDir) 스냅샷으로 회전해서 보여준다.
        CardEffect attackRangeEffect = new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.Damage,
            dmg = dmg,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = attacker.MoveAttackRangeInfoSO,
            areaTargetMode = AreaTargetMode.Directional8,
            animTrigger = attackTrigger,
        };

        // 맞을 대상: [0]이 주 대상, 나머지는 moveAttackRange 내 다른 적(스플래시). 로직이 동기라 타격 사이에 위치가
        // 바뀌지 않으므로 루프 전에 한 번 확정한다.
        var targets = new List<(Vector2Int pos, Piece piece)> { (impactPos, defender) };
        if (isAreaAttack)
        {
            Vector3 attackerWorldPos = GetButtonScript(attackerPos).Piecelocation;
            foreach (Vector2Int offset in moveAttackOffsets)
            {
                Vector2Int pos = anchor + offset;
                if (pos == impactPos) continue;
                if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
                Piece p = GetButtonScript(pos).GetPieceScript();
                if (p == null || p.teamID == attacker.teamID) continue;
                p.transform.rotation = Quaternion.LookRotation(attackerWorldPos - GetButtonScript(pos).Piecelocation);
                targets.Add((pos, p));
            }
        }

        // 타격 루프 — 죽은 대상은 다음 타부터 빠지고, 전원이 죽으면 남은 타는 없다.
        int hitCount = Mathf.Max(1, cardEffect?.hitCount ?? 1);
        var hitExtras = new List<List<IEnumerator>>[targets.Count];
        var hpLeft = new int[targets.Count];
        for (int t = 0; t < targets.Count; t++)
        {
            hitExtras[t] = new List<List<IEnumerator>>();
            hpLeft[t] = 1;
        }
        int totalDealt = 0;
        for (int i = 0; i < hitCount; i++)
        {
            bool anyAlive = false;
            for (int t = 0; t < targets.Count; t++)
            {
                if (hpLeft[t] <= 0) continue;
                Piece p = targets[t].piece;
                List<IEnumerator> extra = StrikeTarget(p, dmg, cardEffect, out int dealt, out hpLeft[t]);
                totalDealt += dealt;
                // 다음 이동공격 버프의 상태이상은 대상마다 첫 적중 때 1회만 — 타마다 같은 상태이상이 중복으로 쌓이지 않게.
                if (i == 0)
                {
                    foreach (CardEffect inflict in bonus.onHitEffects)
                    {
                        StatusEffect s = ApplyStatusEffect(p, inflict); // 즉시 적용
                        if (s != null) extra.Add(p.StatusTextReaction(s.DisplayName, s.IsBuff, s.EffectColor));
                    }
                }
                hitExtras[t].Add(extra);
                if (hpLeft[t] > 0) anyAlive = true;
            }
            if (!anyAlive) break;
        }

        // 대상 반응: 1타면 대상별 Hit/Die + 그 타의 extra, 다중 타격이면 대상마다 MultiHitReactionCor.
        // 모두 공격자 애니메이션의 실제 타격 프레임(Animation Event)에 맞춰 PlayCasterAndTargetReaction이 동시에 시작한다.
        var targetCoroutines = new List<IEnumerator>();
        for (int t = 0; t < targets.Count; t++)
        {
            Piece p = targets[t].piece;
            bool died = hpLeft[t] <= 0;
            if (hitCount <= 1)
            {
                targetCoroutines.Add(TriggerAnimCor(p, died ? "Die" : "Hit", 0.3f, false));
                targetCoroutines.AddRange(hitExtras[t][0]);
                if (died) targetCoroutines.Add(p.PieceDeathSound());
            }
            else
            {
                targetCoroutines.Add(MultiHitReactionCor(p, hitExtras[t], hitCount, died));
            }
        }

        // 시전자 자신에게 걸리는 보너스(실드/회복)도 같은 타격 프레임에 맞춰 같이 재생되도록 합류시킨다.
        if (currentActiveCard != null && currentActiveCard.shieldOnMoveAttack && currentActiveCard.moveAttackShieldAmount > 0)
        {
            int shieldAfter = attacker.GetShield(currentActiveCard.moveAttackShieldAmount);
            targetCoroutines.Add(attacker.ShieldText(currentActiveCard.moveAttackShieldAmount));
            targetCoroutines.Add(attacker.ShieldVisualOn(shieldAfter));
        }
        // 대상마다 취약 등으로 실제 피해가 다를 수 있으므로 대상별·타별 dealt를 합산한 만큼 한 번에 회복한다.
        if (cardEffect != null && cardEffect.healOnHit && totalDealt > 0)
        {
            int healed = attacker.GetHeal(totalDealt);
            targetCoroutines.Add(attacker.HealText(healed));
        }
        Button defenderButton = GetButtonScript(impactPos);
        // 소모된 다음 이동공격 버프는 공격자가 다가가기 직전(이동이 없으면 공격 직전)에 버프마다 텍스트를 따로 큐에 넣어
        // 차례로 띄운다 — 파티클/사운드 없이 텍스트만. 텍스트는 모두 같은 자리에서 생기므로 다음 텍스트 전에만 잠깐 기다리고,
        // 마지막 텍스트 뒤에는 기다리지 않아 이동이 바로 이어진다.
        for (int i = 0; i < bonus.consumedNames.Count; i++)
        {
            if (i > 0) motionQueue.Enqueue(WaitSecondsCor(ConsumedBuffTextInterval));
            motionQueue.Enqueue(attacker.StatusTextReaction(bonus.consumedNames[i] + " 소모", true, new Color(1f, 0.27f, 0.27f), textOnly: true));
        }
        if (approach != null) motionQueue.Enqueue(approach);
        // 이동 스텝(PieceMoveCor)이 이동 방향으로 회전을 덮어써버리므로, 공격 애니메이션이 재생되기
        // 직전(이동 완료 후)에 공격자/피격자를 다시 서로 마주보게 회전시킨다.
        motionQueue.Enqueue(RotateForMoveAttack(attacker, defender, attackerPos, defenderButton));
        motionQueue.Enqueue(PlayCasterAndTargetReaction(attacker, attackTrigger, Parallel(targetCoroutines.ToArray()), attackRangeEffect,
            originPos: anchor, directionOverride: attackDir));

        // 실제 도착 위치: 실제 이동공격으로 주 대상을 처치했으면 그 칸까지 계속 전진, 아니면(생존·판정 공격) 지금 칸.
        bool primaryDied = hpLeft[0] <= 0;
        bool advance = moved && primaryDied;
        Vector2Int finalAttackerPos = advance ? impactPos : attackerPos;
        if (primaryDied)
        {
            ClearDeadPieceOccupancy(impactPos, defender);
            if (advance) ApplyMoveOccupancy(GetButtonScript(attackerPos), defenderButton); // 공격자가 빈 자리까지 계속 이동
            motionQueue.Enqueue(defender.DeathCor());
            if (advance) motionQueue.Enqueue(PieceMoveCor(attacker.gameObject, GetButtonScript(attackerPos), defenderButton, 1f));
            TriggerOnKillEffect(finalAttackerPos, attacker, cardEffect);
        }
        for (int t = 1; t < targets.Count; t++)
        {
            if (hpLeft[t] > 0) continue;
            ClearDeadPieceOccupancy(targets[t].pos, targets[t].piece);
            motionQueue.Enqueue(targets[t].piece.DeathCor());
            TriggerOnKillEffect(finalAttackerPos, attacker, cardEffect);
        }

        // 연쇄 이동공격 같은, 이동공격 자체를 구독해 반응하는 부가 효과를 위한 이벤트 발화.
        // Board.Combat.cs는 누가 구독하는지 몰라도 된다(ChainMoveAttackBuff 참고).
        // 주의: isAreaAttack 자체를 넘기면 안 된다 — 이 값은 "MoveAttackRangeInfoSO가 (0,0) 센티널이
        // 아닌 값으로 설정돼 있는지"만 볼 뿐이라, BasicFrontRangeInfo처럼 칸 하나짜리 방향성 범위(회전 후
        // 정확히 impactPos와 겹쳐서 스플래시 루프의 자체 중복 제거로 걸러지는 경우, 예: Warrior)에도 true가
        // 나온다. 실제로 스플래시 대상이 있었는지(targets.Count > 1)를 넘겨야 "사실상 단일 타격"을
        // 정확히 구분해서, 이런 기물의 평범한 이동공격까지 연쇄가 막혀버리는 걸 방지한다.
        attacker.RaiseMoveAttackPerformed(finalAttackerPos, impactPos, dmg, targets.Count > 1);

        // 반격(가시 등)은 본체 공격과 별개의 시점에 일어나는 반응이라 자체 Parallel로 한 항목만 큐에 넣는다 —
        // TriggerAnim을 즉시(동기) 호출하던 예전 방식은 애니메이션이 큐 순번을 기다리는 사운드보다 먼저
        // 재생돼 타이밍이 어긋났다. TriggerAnimCor로 바꿔 애니메이션과 DamageText(사운드)가 같은 큐 항목
        // 안에서 함께 시작되게 한다.
        int counterDmg = defender.TriggerReceiveMoveAttack(attacker);
        if (counterDmg > 0)
        {
            int attackerHp = attacker.GetDamage(counterDmg);
            var counterReaction = new List<IEnumerator>
            {
                TriggerAnimCor(attacker, attackerHp <= 0 ? "Die" : "Hit", 0.3f, false),
                attacker.DamageText(counterDmg, isCounter: true)
            };
            if (attackerHp <= 0) counterReaction.Add(attacker.PieceDeathSound());
            motionQueue.Enqueue(Parallel(counterReaction.ToArray()));
            if (attackerHp <= 0)
            {
                // 이동 전 위치가 아니라 공격자의 최종 위치(finalAttackerPos) 기준으로 지워야 한다 — 점유가 이미
                // 이동 애니메이션보다 앞서 확정돼 있으므로.
                ClearDeadPieceOccupancy(finalAttackerPos, attacker);
                motionQueue.Enqueue(attacker.DeathCor());
            }
        }

        StartMotionQueue();
    }

    // ChainMoveAttackBuff처럼 Piece.OnMoveAttackPerformed를 구독하는 컴포넌트의 공용 진입점.
    // attacker 기준 이동범위(GetMoveableButton) 내에서 excludePos(이동공격의 피격자 칸)를 제외한 적 중
    // 체력이 가장 낮은 적(도발 우선)을 같은 피해로 공격한다. 그런 적이 없으면 피격자가 살아 있을 때 그 피격자를 한 번 더 공격한다.
    public void TryChainMoveAttack(Piece attacker, Vector2Int attackerFinalPos, Vector2Int excludePos, int dmg)
    {
        // animTrigger를 반드시 채워서 넘긴다 — cardEffect가 null(또는 animTrigger 없음)이면 공격자가
        // 아무 트리거도 재생하지 않아 WaitAnimationEventThenRun이 OnAnimationEvent를 영영 못 받고
        // AnimationEventFallbackTimeout(1.2초)만큼 그냥 멈췄다가 데미지 텍스트/피격 반응이 뜬다.
        // type은 Damage로 둔다 — 기본값(Move)이면 도발 판정(CanEffectReach)이 이동 착지 칸까지 따진다.
        CardEffect chainEffect = new CardEffect { type = EffectType.Damage, animTrigger = "Attack" };

        // 다른 적이 여럿이면 LowestHP 카드와 같은 기준(도발 우선 → 체력 최저)으로 고른다.
        int targetTeam = attacker.teamID == 0 ? 1 : 0;
        var candidates = new List<Vector2Int>();
        foreach (Vector2Int offset in attacker.GetMoveableButton())
        {
            Vector2Int pos = attackerFinalPos + offset;
            if (pos == excludePos) continue;
            if (pos.x < 0 || pos.x >= N || pos.y < 0 || pos.y >= M) continue;
            candidates.Add(pos);
        }
        Vector2Int chainTarget = PickLowestHPTarget(candidates, targetTeam, chainEffect, attackerFinalPos);
        if (chainTarget.x >= 0)
        {
            AttackPiece(attackerFinalPos, chainTarget, dmg, chainEffect); // 데미지/애니메이션/처치 로직 재사용
            return;
        }

        // 피격자가 이미 처치됐으면 칸이 비었거나(공격자가 전진했다면) 공격자 자신이 서 있으므로 아래 검사에서 걸러진다.
        if (excludePos == attackerFinalPos) return;
        if (excludePos.x < 0 || excludePos.x >= N || excludePos.y < 0 || excludePos.y >= M) return;
        Piece primary = GetButtonScript(excludePos).GetPieceScript();
        if (primary == null || primary.teamID != targetTeam) return;
        AttackPiece(attackerFinalPos, excludePos, dmg, chainEffect);
    }

    // 이동 스텝(PieceMoveCor)이 이동 방향으로 덮어쓴 회전을, 공격 애니메이션이 재생되기 직전(이동
    // 완료 후)에 다시 공격자↔피격자가 서로 마주보는 방향으로 되돌린다. attackerPos는 이동이 끝난
    // 실제 도착 칸(adjacentPos) — bScript1은 이동 후엔 빈 칸이라 위치 기준으로 쓸 수 없다.
    IEnumerator RotateForMoveAttack(Piece attacker, Piece defender, Vector2Int attackerPos, Button defenderButton)
    {
        Vector3 attackerWorldPos = GetButtonScript(attackerPos).Piecelocation;
        defender.transform.rotation = Quaternion.LookRotation(attackerWorldPos - defenderButton.Piecelocation);
        attacker.transform.rotation = Quaternion.LookRotation(defenderButton.Piecelocation - attackerWorldPos);
        yield break;
    }

    // 처치가 확정된 시점에 호출: OnKill 유물을 발동시키고, cardEffect에 onKillEffect가 설정돼 있으면
    // 시전자를 대상으로 그 효과도 실행한다(예: 처치 시 ColDamageUp). 기존 4개 호출 지점을 그대로 재사용.
    void TriggerOnKillEffect(Vector2Int casterPos, Piece caster, CardEffect cardEffect)
    {
        TriggerRelicsOnKill(caster);
        if (caster == null || cardEffect?.onKillEffect == null) return;
        ExecuteCardEffectOnPiece(casterPos, caster, cardEffect.onKillEffect with { caster = caster });
    }

    // attack/moveattack 계열 공통: 대상 1명에게 공격 피해를 적용한다. 취약처럼 "공격으로 받는 피해"에만
    // 반응하는 보정은 여기서만 반영되므로, 독/반격/자해/자기 DoT 경로는 GetDamage를 직접 호출한다.
    // dealt(보정 후 실제 피해)를 DamageText/healOnHit에 그대로 써야 한다.
    (int dealt, int hpLeft) ApplyAttackDamage(Piece target, int dmg)
    {
        int dealt = target.ModifyIncomingAttackDamage(dmg);
        int hpLeft = target.GetDamage(dealt);
        if (target.teamID == 0) playerDamagedThisTurn = true;
        return (dealt, hpLeft);
    }

    // 공격 1타: 대상에게 공격 피해를 적용하고 카드 효과의 상태이상을 건 뒤(둘 다 즉시), 그 타의 대상 반응(피해 텍스트 +
    // 상태이상 텍스트)을 돌려준다. AttackPiece와 이동공격(ResolveMoveAttackHit)의 타격 루프가 공유한다 — Hit/Die 트리거와
    // 흡혈(healOnHit)은 호출부마다 묶는 방식이 달라 호출부가 처리한다.
    List<IEnumerator> StrikeTarget(Piece target, int dmg, CardEffect cardEffect, out int dealt, out int hpLeft)
    {
        (dealt, hpLeft) = ApplyAttackDamage(target, dmg);
        var extra = new List<IEnumerator> { target.DamageText(dealt) };
        StatusEffect statusEffect = ApplyStatusEffect(target, cardEffect); // 즉시 적용
        if (statusEffect != null)
            extra.Add(target.StatusTextReaction(statusEffect.DisplayName, statusEffect.IsBuff, statusEffect.EffectColor));
        return extra;
    }

    // hitCount: 같은 대상을 연달아 때리는 횟수(DoubleAttackCard의 hitCount, FinalAttackCard의 hitsPerDiscarded).
    // 피해/상태이상/흡혈은 타격마다 따로 적용하고, 연출은 시전자 애니메이션 1회 + 피격 반응 hitCount회(MultiHitReactionCor)로 묶는다.
    void AttackPiece(Vector2Int pos1, Vector2Int pos2, int dmg, CardEffect cardEffect = null, int hitCount = 1)
    {
        // pos1 == pos2: 자기 자신에게 거는 예약 Damage 효과(예: StatusEffectType.TurnDamageStart/End의
        // DoT 틱 — CreateStatusEffect가 targetlogic=self로 만들어 EnqueueScheduledEffect를 통해 자기
        // 자신을 targetPos로 넘김). 캐스터와 피격자가 같은 기물이므로 회전/공격 애니메이션 없이
        // 피격자의 Hit/Die 반응만 재생한다.
        if (pos1 == pos2)
        {
            Piece target = GetButtonScript(pos2).GetPieceScript();
            if (target == null) { StartMotionQueue(); return; }

            int hpLeftDirect = target.GetDamage(dmg);
            if (target.teamID == 0) playerDamagedThisTurn = true;

            var directReaction = new List<IEnumerator>
            {
                TriggerAnimCor(target, hpLeftDirect <= 0 ? "Die" : "Hit", 0.3f, false),
                target.DamageText(dmg)
            };
            StatusEffect directStatusEffect = ApplyStatusEffect(target, cardEffect); // 즉시 적용
            if (directStatusEffect != null)
                directReaction.Add(target.StatusTextReaction(directStatusEffect.DisplayName, directStatusEffect.IsBuff, directStatusEffect.EffectColor));
            if (hpLeftDirect <= 0) directReaction.Add(target.PieceDeathSound());
            motionQueue.Enqueue(Parallel(directReaction.ToArray()));
            if (hpLeftDirect <= 0)
            {
                ClearDeadPieceOccupancy(pos2, target);
                motionQueue.Enqueue(target.DeathCor());
            }
            StartMotionQueue();
            return;
        }

        Piece pScript1 = GetButtonScript(pos1).GetPieceScript();
        if (pScript1 == null) { StartMotionQueue(); return; }

        Piece pScript2 = GetButtonScript(pos2).GetPieceScript();
        if (pScript2 == null)
        {
            // 피격자가 없는 헛스윙: 시전자 공격 애니메이션만 재생하고 실제 효과는 적용하지 않음 (카드는 정상 소모됨)
            // 회전은 TriggerAnimCor에 넘기는 attackTargetPos를 통해 PlayAttackPunchFallback이 처리한다.
            motionQueue.Enqueue(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect, attackTargetPos: pos2));
            StartMotionQueue();
            return;
        }

        // 타격마다 피해/상태이상/흡혈을 적용하고 그 타의 반응(extra)을 모은다. 대상이 죽으면 남은 타격은 없다 —
        // 예전엔 타격마다 AttackPiece를 따로 불러 죽은 뒤 호출이 헛스윙 분기로 빠졌는데, 그 역할을 hpLeft > 0 조건이 대신한다.
        var hitExtras = new List<List<IEnumerator>>();
        int hpLeft = 1;
        for (int i = 0; i < Mathf.Max(1, hitCount) && hpLeft > 0; i++)
        {
            List<IEnumerator> extra = StrikeTarget(pScript2, dmg, cardEffect, out int dealt, out hpLeft);
            if (cardEffect != null && cardEffect.healOnHit && dealt > 0)
            {
                int healed = pScript1.GetHeal(dealt);
                extra.Add(pScript1.HealText(healed));
            }
            hitExtras.Add(extra);
        }

        pScript2.transform.rotation = Quaternion.LookRotation(GetButtonScript(pos1).Piecelocation - GetButtonScript(pos2).Piecelocation);
        pScript1.transform.rotation = Quaternion.LookRotation(GetButtonScript(pos2).Piecelocation - GetButtonScript(pos1).Piecelocation);

        if (hitCount <= 1)
            motionQueue.Enqueue(PieceAttackCor(pScript1, pScript2, cardEffect?.animTrigger, hpLeft <= 0 ? "Die" : "Hit", cardEffect, hitExtras[0]));
        else
            motionQueue.Enqueue(PlayCasterAndTargetReaction(pScript1, cardEffect?.animTrigger,
                MultiHitReactionCor(pScript2, hitExtras, hitCount, hpLeft <= 0), cardEffect));
        if (hpLeft <= 0)
        {
            ClearDeadPieceOccupancy(pos2, pScript2);
            motionQueue.Enqueue(pScript2.DeathCor());
            TriggerOnKillEffect(pos1, pScript1, cardEffect);
        }

        StartMotionQueue();
    }

    void HealPiece(Vector2Int pos1, Vector2Int pos2, int dmg, CardEffect cardEffect = null)
    {
        Piece pScript1 = GetButtonScript(pos1).GetPieceScript();
        if (pScript1 == null) { StartMotionQueue(); return; }

        Piece pScript2 = GetButtonScript(pos2).GetPieceScript();
        if (pScript2 == null)
        {
            // 대상이 없는 헛스윙: 시전자 애니메이션만 재생
            motionQueue.Enqueue(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect));
            StartMotionQueue();
            return;
        }

        int healed = pScript2.GetHeal(dmg);

        var healExtra = new List<IEnumerator> { pScript2.HealText(healed) };
        StatusEffect healStatusEffect = ApplyStatusEffect(pScript2, cardEffect); // 즉시 적용
        if (healStatusEffect != null)
            healExtra.Add(pScript2.StatusTextReaction(healStatusEffect.DisplayName, healStatusEffect.IsBuff, healStatusEffect.EffectColor));
        motionQueue.Enqueue(PieceHealCor(pScript1, pScript2, cardEffect, healExtra));

        StartMotionQueue();
    }

    // 피격자 반응(Hit/Die + DamageText)만 재생 — 캐스터=피격자라 별도 캐스터 애니메이션은 없음.
    void SelfDamagePiece(Vector2Int casterPos, int dmg)
    {
        if (casterPos.x < 0 || casterPos.y < 0) return;
        Piece p = GetButtonScript(casterPos).GetPieceScript();
        if (p == null) return;
        int hpLeft = p.GetDamage(dmg);
        if (p.teamID == 0) playerDamagedThisTurn = true;

        var selfDamageReaction = new List<IEnumerator>
        {
            TriggerAnimCor(p, hpLeft <= 0 ? "Die" : "Hit", 0.3f, false),
            p.DamageText(dmg)
        };
        if (hpLeft <= 0) selfDamageReaction.Add(p.PieceDeathSound());
        motionQueue.Enqueue(Parallel(selfDamageReaction.ToArray()));
        if (hpLeft <= 0)
        {
            ClearDeadPieceOccupancy(casterPos, p);
            motionQueue.Enqueue(p.DeathCor());
        }
        StartMotionQueue();
    }

    void AreaAttackPiece(Vector2Int casterPos, List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        if (casterPos.x < 0 || casterPos.y < 0 || targets.Count == 0) return;

        Button casterButton = GetButtonScript(casterPos);
        if (casterButton.GetPiece() == null) return;

        Piece caster = casterButton.GetPieceScript();

        if (targets.Count > 0)
            casterButton.GetPiece().transform.rotation = Quaternion.LookRotation(GetButtonScript(targets[0]).Piecelocation - casterButton.Piecelocation);

        int totalHeal = 0;
        var hitTargets = new List<(Piece piece, bool died)>();
        var textCoroutines = new List<IEnumerator>();
        var deathCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            var (dealt, hpLeft) = ApplyAttackDamage(p, dmg);
            p.transform.rotation = Quaternion.LookRotation(casterButton.Piecelocation - GetButtonScript(pos).Piecelocation);
            bool died = hpLeft <= 0;
            hitTargets.Add((p, died));
            textCoroutines.Add(p.DamageText(dealt));
            StatusEffect areaStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (areaStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(areaStatusEffect.DisplayName, areaStatusEffect.IsBuff, areaStatusEffect.EffectColor));
            if (died)
            {
                ClearDeadPieceOccupancy(pos, p);
                deathCoroutines.Add(p.DeathCor());
                TriggerOnKillEffect(casterPos, caster, cardEffect);
            }
            if (cardEffect != null && cardEffect.healOnHit)
                totalHeal += dealt;
        }

        if (totalHeal > 0 && caster != null)
        {
            int healed = caster.GetHeal(totalHeal);
            textCoroutines.Add(caster.HealText(healed));
        }

        motionQueue.Enqueue(PieceAreaAttackCor(caster, hitTargets, cardEffect?.animTrigger, cardEffect, textCoroutines));
        foreach (var d in deathCoroutines)
            motionQueue.Enqueue(d);

        StartMotionQueue();
    }

    // 캐스터(시전자) 없이 여러 기물에게 동시에 피해를 준다. 카드가 아니라 DamageAllAllies처럼 특정
    // 기물이 아닌 보드/이벤트 자체에서 발생하는, 애초에 캐스터 개념이 없는 전역 효과 전용.
    void AreaAttackPiece(List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        if (targets.Count == 0) return;

        var hitTargets = new List<(Piece piece, bool died)>();
        var textCoroutines = new List<IEnumerator>();
        var deathCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int hpLeft = p.GetDamage(dmg);
            if (p.teamID == 0) playerDamagedThisTurn = true;
            bool died = hpLeft <= 0;
            hitTargets.Add((p, died));
            textCoroutines.Add(p.DamageText(dmg));
            StatusEffect areaStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (areaStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(areaStatusEffect.DisplayName, areaStatusEffect.IsBuff, areaStatusEffect.EffectColor));
            if (died)
            {
                ClearDeadPieceOccupancy(pos, p);
                deathCoroutines.Add(p.DeathCor());
            }
        }

        motionQueue.Enqueue(PieceAreaAttackCor(null, hitTargets, cardEffect?.animTrigger, cardEffect, textCoroutines));
        foreach (var d in deathCoroutines)
            motionQueue.Enqueue(d);

        StartMotionQueue();
    }

    void AreaShieldPiece(List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        Piece caster = casterBtn.GetPieceScript();
        var shieldedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int shieldAfter = p.GetShield(dmg);
            shieldedPieces.Add(p);
            textCoroutines.Add(p.ShieldText(dmg));
            textCoroutines.Add(p.ShieldVisualOn(shieldAfter));
            StatusEffect shieldStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (shieldStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(shieldStatusEffect.DisplayName, shieldStatusEffect.IsBuff, shieldStatusEffect.EffectColor));
        }

        motionQueue.Enqueue(PieceAreaShieldCor(caster, shieldedPieces, cardEffect?.animTrigger, cardEffect, textCoroutines));

        StartMotionQueue();
    }

    void AreaHealPiece(List<Vector2Int> targets, int dmg, CardEffect cardEffect = null)
    {
        Button casterBtn = GetButtonScript(selectedButton);
        Piece caster = casterBtn.GetPieceScript();
        var healedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        foreach (Vector2Int pos in targets)
        {
            Piece p = GetButtonScript(pos).GetPieceScript();
            if (p == null) continue;
            int healed = p.GetHeal(dmg);
            healedPieces.Add(p);
            textCoroutines.Add(p.HealText(healed));
            StatusEffect healAreaStatusEffect = ApplyStatusEffect(p, cardEffect); // 즉시 적용
            if (healAreaStatusEffect != null)
                textCoroutines.Add(p.StatusTextReaction(healAreaStatusEffect.DisplayName, healAreaStatusEffect.IsBuff, healAreaStatusEffect.EffectColor));
        }

        motionQueue.Enqueue(PieceAreaHealCor(caster, healedPieces, cardEffect?.animTrigger, cardEffect, textCoroutines));

        StartMotionQueue();
    }

    void ShieldPiece(Vector2Int pos1, Vector2Int pos2, int dmg, CardEffect cardEffect = null)
    {
        Piece pScript1 = GetButtonScript(pos1).GetPieceScript();
        if (pScript1 == null) { StartMotionQueue(); return; }

        Piece pScript2 = GetButtonScript(pos2).GetPieceScript();
        if (pScript2 == null)
        {
            // 대상이 없는 헛스윙: 시전자 애니메이션만 재생
            motionQueue.Enqueue(TriggerAnimCor(pScript1, cardEffect?.animTrigger, cardEffect: cardEffect));
            StartMotionQueue();
            return;
        }

        int resultingShield = pScript2.GetShield(dmg);

        var shieldExtra = new List<IEnumerator> { pScript2.ShieldText(dmg), pScript2.ShieldVisualOn(resultingShield) };
        StatusEffect shieldPieceStatusEffect = ApplyStatusEffect(pScript2, cardEffect); // 즉시 적용
        if (shieldPieceStatusEffect != null)
            shieldExtra.Add(pScript2.StatusTextReaction(shieldPieceStatusEffect.DisplayName, shieldPieceStatusEffect.IsBuff, shieldPieceStatusEffect.EffectColor));
        motionQueue.Enqueue(PieceShieldCor(pScript1, pScript2, cardEffect, shieldExtra));

        StartMotionQueue();
    }
}
