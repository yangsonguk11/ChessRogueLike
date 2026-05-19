using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    Queue<IEnumerator> motionQueue = new Queue<IEnumerator>();
    public bool queuecoroutineworking;

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

    IEnumerator MoveAdjacent(Button button1, Button button2, float moveDuration)
    {
        Vector2 adjacentPos = GetAdjacentLocation(button1.GetLocation(), button2.GetLocation());
        Button newTarget = GetButtonScript(adjacentPos);
        yield return PieceMoveCor(button1, newTarget, moveDuration);
    }

    // CardEffect에 AnimationClip이 있으면 해당 이름의 Animator 상태를 재생하고 끝날 때까지 대기.
    // Animator가 없거나 clip이 null이면 즉시 반환 (호출부에서 폴백 사용).
    IEnumerator PieceCustomAnimCor(GameObject piece, AnimationClip clip)
    {
        if (piece == null || clip == null) yield break;
        Animator animator = piece.GetComponent<Animator>();
        if (animator == null) yield break;

        animator.Play(clip.name);
        yield return null; // 한 프레임 대기 — Animator 상태 전환 반영

        while (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
    }
}
