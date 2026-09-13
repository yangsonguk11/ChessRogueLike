using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class Board
{
    Queue<IEnumerator> motionQueue = new Queue<IEnumerator>();
    public bool queuecoroutineworking;

    // 이미 처리 중이면 새로 시작하지 않음 — 한 프레임 안에서 여러 기물의 효과를 연달아
    // motionQueue에 넣어도(예: ProcessTeamTurnEffects) 큐 소비자가 중복 실행되지 않게 함.
    void StartMotionQueue()
    {
        if (!queuecoroutineworking)
            StartCoroutine(ProcessQueue());
    }

    // CardCanvas 등 다른 컴포넌트가 자신의 코루틴을 motionQueue에 직접 넣고 싶을 때 쓰는 얇은 래퍼
    // (motionQueue/StartMotionQueue는 private). 예: DrawCard의 배치 드로우 연출이 같은 카드효과가
    // 먼저 큐에 넣은 보드 애니메이션 뒤로 순서를 맞추기 위해 사용.
    public void EnqueueBoardAnimation(IEnumerator cor)
    {
        motionQueue.Enqueue(cor);
        StartMotionQueue();
    }

    IEnumerator ProcessQueue()
    {
        queuecoroutineworking = true;
        TurnManager.instance.TurnStateProcessing();
        while (motionQueue.Count > 0)
        {
            IEnumerator nextAction = motionQueue.Dequeue();
            yield return StartCoroutine(nextAction);
        }
        queuecoroutineworking = false;
        TurnManager.instance.RollbackStateProcessing();
    }

    // moveDuration을 "칸당 기준 시간"으로 해석 — 이동 거리(체비셰프)에 비례해 늘리되
    // 너무 느려지지 않도록 기준값의 0.6~1.6배 범위로 clamp한다.
    // PieceMoveCor와 MovePieceWithAnim(TriggerAnimCor의 fallback)이 같은 값을 쓰도록 공용 헬퍼로 뺐다.
    static float ResolveMoveDuration(Button button1, Button button2, float baseDuration)
    {
        Vector2Int loc1 = button1.GetLocation();
        Vector2Int loc2 = button2.GetLocation();
        int distance = Mathf.Max(1, Mathf.Max(Mathf.Abs(loc1.x - loc2.x), Mathf.Abs(loc1.y - loc2.y)));
        return Mathf.Clamp(baseDuration * distance, baseDuration * 0.6f, baseDuration * 1.6f);
    }

    // bumpOnArrival로 넘어갈 때 pos1→pos2 이동 벡터의 몇 배 지점까지 들어갔다가 튕겨나올지.
    // 1.15면 도착 지점(pos2)을 지나 "이동 거리의 15%"만큼 더 들어갔다가 복귀 — 고정 거리가 아니라
    // 이동 거리에 비례하므로 칸 수가 멀어도 일정한 비율로 파고든다.
    const float BumpOvershootRatio = 1.15f;

    // bumpOnArrival: 목적지가 막혀 원래 가려던 곳까지 못 가고 그 직전 칸에서 멈추는 경우(MoveAttack)에만
    // true로 넘겨서 "부딪혀서 멈춘" 이펙트를 재생한다. 일반 이동에는 쓰지 않음.
    //
    // 순수 비주얼 코루틴 — 점유(논리) 이전은 이미 ApplyMoveOccupancy가 enqueue 이전에 동기로 끝내놨으므로
    // 여기선 건드리지 않는다. piece를 파라미터로 받는 이유: 이 코루틴은 motionQueue에서 dequeue될 때(즉
    // enqueue 시점보다 나중에) 실행되는데, 그때는 button1이 이미 점유상 비어있을 수 있어
    // button1.GetPiece()를 다시 읽으면 안 된다(널 위험).
    IEnumerator PieceMoveCor(GameObject piece, Button button1, Button button2, float moveDuration, bool bumpOnArrival = false)
    {
        if (piece == null) yield break; // 큐에서 대기하는 동안 죽어서 파괴된 경우
        if (button1 == button2)
            yield break;

        Vector3 pos1 = button1.transform.position;
        Vector3 pos2 = button2.transform.position;

        piece.transform.rotation = Quaternion.LookRotation(pos2 - pos1);
        float actualDuration = ResolveMoveDuration(button1, button2, moveDuration);

        yield return piece.transform.DOMove(pos2, actualDuration).SetEase(Ease.Linear).WaitForCompletion();

        if (bumpOnArrival)
        {
            // 막힌 칸(목적지) 방향으로 살짝 들어갔다가 도착 위치(pos2)로 튕겨나오는 넉백 연출.
            // DOPunchPosition은 현재 위치(pos2)를 기준으로 punch만큼 나아갔다가 그 자리로 자동 복귀하므로,
            // pos1→pos2 벡터의 (BumpOvershootRatio - 1)배만큼만 punch로 얹어주면 전체적으로 1.15배 지점까지 간 셈이 된다.
            Vector3 punchDir = (pos2 - pos1) * (BumpOvershootRatio - 1f);
            piece.transform.DOPunchPosition(punchDir, 0.25f, 8, 0.6f);
            piece.transform.DOPunchScale(new Vector3(0.15f, -0.15f, 0.15f), 0.12f, 6, 0.5f);
        }

        // 점유는 이미 button2로 넘어가 있다 — 여기선 비주얼(부모+정확한 좌표)만 마무리.
        button2.AttachPieceVisual(piece);
        button2.SnapPieceToCell();
    }

    const float AnimationEventFallbackTimeout = 1.2f; // 이벤트가 아직 안 심어진 클립을 위한 안전장치

    // 시전자(caster) 애니메이션을 재생하면서, 시전자 클립에 심어둔 Animation Event(OnAnimationEvent)가
    // 호출되는 순간 targetReaction(대상이 하는 행동 — Hit/Die/Heal/Shield 애니메이션 + 텍스트/파티클 등)을
    // 시작한다. 공격/힐/실드/버프 등 시전자-대상 애니메이션을 갖는 모든 Piece*Cor가 이 함수 하나를 공유한다.
    IEnumerator PlayCasterAndTargetReaction(Piece caster, string casterTrigger, IEnumerator targetReaction, CardEffect cardEffect = null)
    {
        yield return Parallel(
            TriggerAnimCor(caster, casterTrigger, cardEffect: cardEffect),
            WaitAnimationEventThenRun(caster, targetReaction));
    }

    // caster.OnAnimationEvent()가 호출될 때까지 기다렸다가 targetReaction을 실행하고 완료까지 대기.
    // 이벤트가 아직 안 심어진 클립이면 AnimationEventFallbackTimeout만큼만 기다리다 강제로 넘어간다
    // (기존과 비슷하게 "곧바로 시작"하는 타이밍으로 자연히 폴백됨 — 클립별로 점진적으로 이벤트를 추가해도 안전).
    IEnumerator WaitAnimationEventThenRun(Piece caster, IEnumerator targetReaction)
    {
        // 캐스터 없이 즉시발동하는 효과(AreaAttackPiece 등)는 기다릴 애니메이션 자체가 없으므로 바로 실행.
        if (caster == null)
        {
            yield return targetReaction;
            yield break;
        }

        caster.animationEventFired = false;
        float t = 0f;
        while (!caster.animationEventFired && t < AnimationEventFallbackTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
        yield return targetReaction;
    }

    // 공격자/피격자의 TriggerAnim을 extra(데미지 텍스트 등)와 함께 재생하고 전부 끝나야 종료(motionQueue가 다음으로 넘어감).
    IEnumerator PieceAttackCor(Piece attacker, Piece defender, string attackTrigger, string hitOrDieTrigger, CardEffect cardEffect = null, List<IEnumerator> extra = null)
    {
        var targetCoroutines = new List<IEnumerator> { TriggerAnimCor(defender, hitOrDieTrigger, 0.3f, false) };
        // "Die" 트리거와 같은 시점(캐스터 OnAnimationEvent)에 사망 사운드도 함께 재생 — DeathCor는
        // 그 뒤 오브젝트 파괴(1초 대기 후 Destroy)만 담당하도록 역할이 나뉘어 있다.
        if (hitOrDieTrigger == "Die") targetCoroutines.Add(defender.PieceDeathSound());
        if (extra != null) targetCoroutines.AddRange(extra);
        yield return PlayCasterAndTargetReaction(attacker, attackTrigger, Parallel(targetCoroutines.ToArray()), cardEffect);
    }

    // 실드는 시전자가 하는 행동이지 대상이 하는 행동이 아니다 — 대상은 자신의 애니메이터 트리거를 갖지
    // 않고, extra(ShieldVisualOn/ShieldText 등)만 시전자의 OnAnimationEvent 시점에 맞춰 재생한다.
    IEnumerator PieceShieldCor(Piece caster, Piece target, CardEffect cardEffect = null, List<IEnumerator> extra = null)
    {
        var targetCoroutines = new List<IEnumerator>();
        if (extra != null) targetCoroutines.AddRange(extra);
        yield return PlayCasterAndTargetReaction(caster, cardEffect?.animTrigger, Parallel(targetCoroutines.ToArray()), cardEffect);
    }

    // 힐은 시전자만 애니메이터 트리거를 갖는다 — 대상은 HealText(텍스트+PlayHealEffect)만 반응.
    // target은 이제 트리거 재생에 안 쓰이지만 호출부 시그니처를 유지하기 위해 남겨둔다.
    IEnumerator PieceHealCor(Piece healer, Piece target, CardEffect cardEffect = null, List<IEnumerator> extra = null)
    {
        var targetCoroutines = new List<IEnumerator>();
        if (extra != null) targetCoroutines.AddRange(extra);
        yield return PlayCasterAndTargetReaction(healer, cardEffect?.animTrigger, Parallel(targetCoroutines.ToArray()), cardEffect);
    }

    // 시전자 + 여러 대상의 TriggerAnim을 extra와 함께 한꺼번에 재생 (AreaAttack/AreaHeal/AreaShield 공용 패턴).
    IEnumerator PieceAreaAttackCor(Piece caster, List<(Piece piece, bool died)> targets, string attackTrigger, CardEffect cardEffect = null, List<IEnumerator> extra = null)
    {
        var targetCoroutines = new List<IEnumerator>();
        foreach (var (piece, died) in targets)
        {
            targetCoroutines.Add(TriggerAnimCor(piece, died ? "Die" : "Hit", 0.3f, false));
            if (died) targetCoroutines.Add(piece.PieceDeathSound());
        }
        if (extra != null) targetCoroutines.AddRange(extra);
        yield return PlayCasterAndTargetReaction(caster, attackTrigger, Parallel(targetCoroutines.ToArray()), cardEffect);
    }

    // targets는 이제 트리거 재생에 쓰이지 않지만(힐도 대상이 애니메이터 트리거를 갖지 않음),
    // 호출부 시그니처를 유지하기 위해 남겨둔다 — 실제 반응은 전부 extra(HealText)로 넘어온다.
    IEnumerator PieceAreaHealCor(Piece caster, List<Piece> targets, string healTrigger, CardEffect cardEffect = null, List<IEnumerator> extra = null)
    {
        var targetCoroutines = new List<IEnumerator>();
        if (extra != null) targetCoroutines.AddRange(extra);
        yield return PlayCasterAndTargetReaction(caster, healTrigger, Parallel(targetCoroutines.ToArray()), cardEffect);
    }

    // targets는 이제 트리거 재생에 쓰이지 않지만(실드는 대상이 애니메이터 트리거를 갖지 않음),
    // 호출부 시그니처를 유지하기 위해 남겨둔다 — 실제 반응은 전부 extra로 넘어온다.
    IEnumerator PieceAreaShieldCor(Piece caster, List<Piece> targets, string shieldTrigger, CardEffect cardEffect = null, List<IEnumerator> extra = null)
    {
        var targetCoroutines = new List<IEnumerator>();
        if (extra != null) targetCoroutines.AddRange(extra);
        yield return PlayCasterAndTargetReaction(caster, shieldTrigger, Parallel(targetCoroutines.ToArray()), cardEffect);
    }

    // 위치 이동(PieceMoveCor)과 이동 트리거 애니메이션(+범위 표시)을 동시에 재생.
    // MovePiece의 일반 이동과 MoveAttack의 인접 칸 접근이 공유하는 로직.
    // piece를 파라미터로 받는 이유는 PieceMoveCor와 동일 — 점유가 이미 앞당겨져 있어 dequeue 시점의
    // button1.GetPiece()/GetPieceScript()는 비어있을 수 있다. 호출부(enqueue 시점)가 캡처해서 넘긴다.
    IEnumerator MovePieceWithAnim(GameObject piece, Button button1, Button button2, float moveDuration, string animTrigger, CardEffect cardEffect = null, bool bumpOnArrival = false)
    {
        if (button1 == button2 || piece == null) yield break; // PieceMoveCor와 동일하게, 실제로 이동할 필요 없으면 즉시 종료
        float actualDuration = ResolveMoveDuration(button1, button2, moveDuration);
        yield return Parallel(
            PieceMoveCor(piece, button1, button2, moveDuration, bumpOnArrival),
            TriggerAnimCor(piece.GetComponent<Piece>(), animTrigger, actualDuration, cardEffect: cardEffect, originPos: button1.GetLocation()));
    }

    // 여러 코루틴을 동시에 실행하고 전부 끝날 때까지 대기.
    IEnumerator Parallel(params IEnumerator[] coroutines)
    {
        var running = new Coroutine[coroutines.Length];
        for (int i = 0; i < coroutines.Length; i++)
            running[i] = StartCoroutine(coroutines[i]);
        foreach (var c in running)
            yield return c;
    }

    // 트리거 발동 + 애니메이션 길이만큼 범위 표시.
    // cardEffect.effectRange가 있으면 그 범위를 표시(Directional4/8이면 currentHoverDirection으로 회전),
    // 없으면 기물 기본 범위(GetMoveableButton)로 폴백.
    // 단, cardEffect.targetlogic이 self면(Shield/Heal/Buff처럼 자기 자신 대상이라 범위 개념이 없는 효과) 폴백하지 않고 범위를 아예 표시하지 않음.
    // triggerName이 없거나 Animator/클립이 없으면 normalizedTime을 폴링하지 않고 fallbackDuration만큼만 대기
    // (animTrigger 없는 효과에도 그대로 호출해서 범위 표시용으로 쓸 수 있음).
    // originPos/directionOverride: 점유(논리) 상태가 이 코루틴이 실제로 재생되는 시점보다 먼저 바뀔 수
    // 있으므로(예: 이동/사망이 즉시 처리되고 이 연출은 모션 큐에서 나중에 실행), "지금(dequeue 시점)"이
    // 아니라 "이 효과가 enqueue됐을 때"의 위치/방향을 쓰고 싶으면 호출부가 스냅샷으로 넘긴다.
    // 안 넘기면(null) 기존처럼 현재 값을 그대로 읽는다.
    IEnumerator TriggerAnimCor(Piece piece, string triggerName, float fallbackDuration = 0.3f, bool showRange = true, CardEffect cardEffect = null, Vector2Int? attackTargetPos = null, Vector2Int? originPos = null, Vector2Int? directionOverride = null)
    {
        if (piece == null) yield break;

        List<Vector2Int> rangeButtons = new List<Vector2Int>();
        if (showRange && cardEffect?.targetlogic != TargetLogic.self)
        {
            Vector2Int piecePos = originPos ?? FindPiecePos(piece);
            if (piecePos.x >= 0)
            {
                List<Vector2Int> offsets = cardEffect?.effectRange?.GetAbleRange();
                if (offsets != null && (cardEffect.areaTargetMode == AreaTargetMode.Directional4 || cardEffect.areaTargetMode == AreaTargetMode.Directional8))
                    offsets = RotateOffsets(offsets, directionOverride ?? currentHoverDirection);

                foreach (Vector2Int offset in offsets ?? piece.GetMoveableButton())
                {
                    Vector2Int target = piecePos + offset;
                    if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                    GetButtonScript(target).RangeOn(piece.teamID);
                    rangeButtons.Add(target);
                }
            }
        }

        float waitTime = fallbackDuration;
        Animator animator = piece.GetComponent<Animator>();
        bool hasWorkingAnimator = animator != null && animator.enabled && animator.runtimeAnimatorController != null;
        // State 이름과 트리거 이름이 항상 같다는 이 프로젝트의 관례(아래 IsName(triggerName) 비교와 동일 전제)를
        // 이용해, SetTrigger를 부르기 전에 HasState로 즉시·동기적으로 실제 애니메이션 존재 여부를 확인한다.
        // length > 0.01f로 사후 추측하는 것보다 정확하고(짧은 클립이 폴링에 안 걸리는 경우 없음), Attack 폴백
        // 여부 판단에 이 값을 그대로 쓴다.
        bool hasAnimationState = hasWorkingAnimator && !string.IsNullOrEmpty(triggerName)
            && animator.HasState(0, Animator.StringToHash(triggerName));

        if (hasAnimationState)
        {
            animator.SetTrigger(triggerName);

            // 전환이 시작될 때까지 대기 (같은 프레임엔 아직 안 반영될 수 있음)
            int safety = 0;
            while (!animator.IsInTransition(0) && safety++ < 5)
                yield return null;

            float length = 0f;
            if (animator.IsInTransition(0))
            {
                // 전환 "완료"를 기다리지 않고 목적지 상태 정보를 바로 읽음 — 클립이 짧으면
                // 전환이 끝나기 전에 이미 다음 상태(Idle 등)로 빠져나가 버려서 GetCurrentAnimatorStateInfo로는
                // 못 잡는 경우가 있었음.
                AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);
                if (nextInfo.IsName(triggerName))
                    length = nextInfo.length;
            }
            if (length <= 0.01f)
            {
                AnimatorStateInfo curInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (curInfo.IsName(triggerName))
                    length = curInfo.length;
            }
            waitTime = length > 0.01f ? length : fallbackDuration;
        }

        // Attack/AreaAttack인데 실제 애니메이션이 없으면(Animator 자체가 없는 기물이 대부분) DOPunch
        // 기반 기본 공격 연출로 대체 — 조용히 fallbackDuration만 흘려보내던 기존 동작 대신 시각 피드백을 준다.
        bool isAttackTrigger = triggerName == "Attack" || triggerName == "AreaAttack";
        if (isAttackTrigger && !hasAnimationState)
            yield return PlayAttackPunchFallback(piece, fallbackDuration, attackTargetPos);
        else
            yield return new WaitForSeconds(waitTime);

        foreach (Vector2Int v in rangeButtons)
            GetButtonScript(v).RangeOff(piece.teamID);
    }

    const float AttackPunchDistance = 0.3f;  // transform.forward로 뻗는 거리(월드 유닛) — 보드 셀 크기 대비 플레이테스트로 튜닝
    const float AttackPunchOutRatio = 0.4f;  // fallbackDuration 중 뻗는 데 쓰는 비율(나머지는 복귀)

    // Animator가 없거나(대부분의 기물) 있어도 Attack/AreaAttack에 연결된 State가 없을 때 재생하는
    // DOPunch 기반 기본 공격 연출. DOPunchPosition(단일 진동 곡선) 대신 DOMove 2단으로 나눠서, 뻗는
    // 지점에 도달한 순간 정확히 OnAnimationEvent()를 걸 수 있게 한다 — bumpOnArrival과 동일한 이유
    // (PieceMoveCor 참고). 이 호출이 WaitAnimationEventThenRun의 animationEventFired를 대신 켜주므로,
    // Animator 없는 기물도 AnimationEventFallbackTimeout(1.2초)을 다 기다리지 않고 피격 반응이 바로 이어진다.
    // targetPos: 실제 타겟이 있는 공격은 호출부가 이미 회전을 시켜놨지만(Board.Combat.cs), 헛스윙처럼
    // 회전 없이 호출되는 경로를 위한 안전장치 — 넘어오면 뻗기 전에 그 방향으로 먼저 회전시킨다.
    IEnumerator PlayAttackPunchFallback(Piece piece, float fallbackDuration, Vector2Int? targetPos)
    {
        if (targetPos.HasValue)
        {
            Vector3 targetWorldPos = GetButtonScript(targetPos.Value).Piecelocation;
            piece.transform.rotation = Quaternion.LookRotation(targetWorldPos - piece.transform.position);
        }

        float outDuration = fallbackDuration * AttackPunchOutRatio;
        float returnDuration = fallbackDuration - outDuration;

        Vector3 startPos = piece.transform.position;
        Vector3 punchPos = startPos + piece.transform.forward * AttackPunchDistance;

        Sequence seq = DOTween.Sequence();
        seq.Append(piece.transform.DOMove(punchPos, outDuration).SetEase(Ease.OutQuad));
        seq.AppendCallback(() =>
        {
            if (piece == null) return; // 콜백 실행 전 파괴된 경우 방어(Piece.DamageText 등과 동일 패턴)
            piece.OnAnimationEvent();
            piece.transform.DOPunchScale(new Vector3(0.15f, -0.15f, 0.15f), 0.12f, 6, 0.5f);
        });
        seq.Append(piece.transform.DOMove(startPos, returnDuration).SetEase(Ease.InQuad));
        yield return seq.WaitForCompletion();
    }
}
