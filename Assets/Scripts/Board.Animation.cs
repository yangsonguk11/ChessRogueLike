using System.Collections;
using System.Collections.Generic;
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

    IEnumerator PieceMoveCor(Button button1, Button button2, float moveDuration)
    {
        Vector3 pos1 = button1.transform.position;
        Vector3 pos2 = button2.transform.position;
        Debug.LogFormat("{0} {1}", pos1, pos2);
        GameObject piece = button1.GetPiece();
        if (button1 == button2)
            yield break;

        piece.transform.rotation = Quaternion.LookRotation(pos2 - pos1);
        float time = 0f;
        while (time < moveDuration)
        {
            time += Time.deltaTime;
            piece.transform.position = Vector3.Lerp(pos1, pos2, time / moveDuration);
            yield return null;
        }
        piece.transform.position = pos2;
        button2.SetPiece(button1.GetPiece());
        button1.RemovePiece();

        Piece pScript = piece.GetComponent<Piece>();
        if (pScript != null && pScript.teamID == 1)
            UpdateEnemyPositionList(button1.GetLocation(), button2.GetLocation());
    }

    IEnumerator PieceAttackCor(Button button1, Button button2, float moveDuration)
    {
        Vector3 pos1 = button1.Piecelocation;
        Vector3 pos2 = button2.Piecelocation;
        Debug.LogFormat("{0} {1}", pos1, pos2);
        GameObject piece = button1.GetPiece();
        if (button1 == button2)
            yield break;

        piece.transform.rotation = Quaternion.LookRotation(pos2 - pos1);
        GameObject piece1 = button1.GetPiece();
        float time = 0f;
        Vector3 pRotation = piece1.transform.rotation.eulerAngles;
        Vector3 tiltedRotation = pRotation + new Vector3(90, 0, 0);

        while (time < moveDuration / 2)
        {
            piece1.transform.rotation = Quaternion.Euler(Vector3.Lerp(pRotation, tiltedRotation, time / moveDuration * 2));
            time += Time.deltaTime;
            yield return null;
        }
        while (time < moveDuration)
        {
            piece1.transform.rotation = Quaternion.Euler(Vector3.Lerp(tiltedRotation, pRotation, time / moveDuration * 2 - 1));
            time += Time.deltaTime;
            yield return null;
        }
        piece1.transform.rotation = Quaternion.Euler(pRotation);
    }

    IEnumerator PieceAreaAttackCor(Button button, float duration)
    {
        GameObject piece = button.GetPiece();
        if (piece == null) yield break;

        Vector3 startEuler = piece.transform.eulerAngles;
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            piece.transform.rotation = Quaternion.Euler(startEuler + new Vector3(0f, 360f * t, 0f));
            time += Time.deltaTime;
            yield return null;
        }
        piece.transform.rotation = Quaternion.Euler(startEuler);
    }

    IEnumerator PieceShieldCor(Button button1, Button button2, float moveDuration)
    {
        yield return new WaitForSeconds(moveDuration);
    }

    IEnumerator PieceHealCor(Button button1, Button button2, float moveDuration)
    {
        yield return new WaitForSeconds(moveDuration);
    }

    // 위치 이동(PieceMoveCor)과 이동 트리거 애니메이션(+범위 표시)을 동시에 재생.
    // MovePiece의 일반 이동과 MoveAttack의 인접 칸 접근이 공유하는 로직.
    IEnumerator MovePieceWithAnim(Button button1, Button button2, float moveDuration, string animTrigger)
    {
        if (button1 == button2) yield break; // PieceMoveCor와 동일하게, 실제로 이동할 필요 없으면 즉시 종료
        yield return Parallel(
            PieceMoveCor(button1, button2, moveDuration),
            TriggerAnimCor(button1.GetPieceScript(), animTrigger, moveDuration));
    }

    IEnumerator MoveAdjacent(Button button1, Button button2, float moveDuration, string animTrigger = null)
    {
        Vector2 adjacentPos = GetAdjacentLocation(button1.GetLocation(), button2.GetLocation());
        Button newTarget = GetButtonScript(adjacentPos);
        yield return MovePieceWithAnim(button1, newTarget, moveDuration, animTrigger);
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
    // triggerName이 없거나 Animator/클립이 없으면 normalizedTime을 폴링하지 않고 fallbackDuration만큼만 대기
    // (animTrigger 없는 효과에도 그대로 호출해서 범위 표시용으로 쓸 수 있음).
    IEnumerator TriggerAnimCor(Piece piece, string triggerName, float fallbackDuration = 0.3f, bool showRange = true, CardEffect cardEffect = null)
    {
        if (piece == null) yield break;

        List<Vector2> rangeButtons = new List<Vector2>();
        if (showRange)
        {
            Vector2 piecePos = FindPiecePos(piece);
            if (piecePos.x >= 0)
            {
                List<Vector2> offsets = cardEffect?.effectRange?.GetAbleRange();
                Debug.Log($"[TriggerAnimCor] piece={piece.name} piecePos={piecePos} cardEffect={(cardEffect != null)} effectRange={(cardEffect?.effectRange != null)} rawOffsetsCount={offsets?.Count ?? -1} areaTargetMode={cardEffect?.areaTargetMode}");
                if (offsets != null && (cardEffect.areaTargetMode == AreaTargetMode.Directional4 || cardEffect.areaTargetMode == AreaTargetMode.Directional8))
                    offsets = RotateOffsets(offsets, currentHoverDirection);
                Debug.Log($"[TriggerAnimCor] currentHoverDirection={currentHoverDirection} finalOffsetsCount={offsets?.Count ?? -1} usingFallback={offsets == null}");

                foreach (Vector2 offset in offsets ?? piece.GetMoveableButton())
                {
                    Vector2 target = piecePos + offset;
                    Debug.Log($"[TriggerAnimCor] offset={offset} -> target={target}");
                    if (target.x < 0 || target.x >= N || target.y < 0 || target.y >= M) continue;
                    GetButtonScript(target).RangeOn(piece.teamID);
                    rangeButtons.Add(target);
                }
            }
        }

        float waitTime = fallbackDuration;
        Animator animator = piece.GetComponent<Animator>();
        if (!string.IsNullOrEmpty(triggerName) && animator != null)
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
        yield return new WaitForSeconds(waitTime);

        foreach (Vector2 v in rangeButtons)
            GetButtonScript(v).RangeOff(piece.teamID);
    }

}
