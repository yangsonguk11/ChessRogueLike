using UnityEngine;

// 이동공격 시 연쇄 공격 버프 — StatusEffect 시스템 대신 순수 컴포넌트로 표현한다(AutoPiece가
// StunnedCard를 동적으로 붙이는 것과 같은 패턴). Piece.OnMoveAttackPerformed를 스스로 구독해 반응하므로
// Board.Combat.cs는 이 컴포넌트의 존재를 몰라도 된다.
public class ChainMoveAttackBuff : MonoBehaviour
{
    Piece piece;

    void Awake()
    {
        piece = GetComponent<Piece>();
        piece.OnMoveAttackPerformed += HandleMoveAttackPerformed;
    }

    void OnDestroy()
    {
        if (piece != null) piece.OnMoveAttackPerformed -= HandleMoveAttackPerformed;
    }

    void HandleMoveAttackPerformed(Vector2Int attackerFinalPos, Vector2Int impactPos, int dmg, bool hitMultipleTargets)
    {
        if (hitMultipleTargets) return; // 실제로 스플래시 대상을 맞힌 이동공격과는 안 겹치게
        Board.instance.TryChainMoveAttack(piece, attackerFinalPos, impactPos, dmg);
    }
}
