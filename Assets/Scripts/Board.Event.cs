using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    // 휴식 오브젝트의 휴식 버튼에서 호출: 보드 위 모든 아군을 각자의 최대 HP까지만 회복시킨다.
    public void RestHeal()
    {
        var healedPieces = new List<Piece>();
        var textCoroutines = new List<IEnumerator>();

        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2(x, y));
                if (p == null || p.teamID != 0) continue;

                int healAmount = p.maxhp - p.hp;
                if (healAmount <= 0) continue;

                p.GetHeal(healAmount);
                healedPieces.Add(p);
                textCoroutines.Add(p.HealText(healAmount));
            }

        if (healedPieces.Count == 0) return;

        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        motionQueue.Enqueue(PieceAreaHealCor(caster, healedPieces, null, null, textCoroutines));
        StartMotionQueue();
    }
}
