using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 효과 큐(pendingEffects, Board.CardEffect.cs)의 공용 진입점. 카드 효과뿐 아니라 턴 효과·유물·상태이상 틱·처치 시·
// 소환 시 효과 등 게임의 모든 CardEffect가 이 큐 하나로 실행된다.
// - 타이밍 효과(턴 시작/종료, 전투 시작): EnqueueEffect로 큐 뒤에 넣고 ResolveEffects로 큐가 빌 때까지 처리한 뒤
//   다음 타이밍으로 넘어간다(Board.TurnControl.cs).
// - 반응 효과(처치 시·피격 시·소환 시·카드 사용 시·드로우 시): 효과 처리 도중에 InsertEffectNext로 큐 앞에 넣는다 —
//   유발한 효과 직후, 다음 효과보다 먼저 실행된다.
// 카드 밖에서 넣는 효과는 모두 executeOnCaster(시전자 칸에서 바로 실행)이므로, 큐 안의 !executeOnCaster 항목은
// 항상 "지금 쓰는 카드(currentActiveCard)의 효과"다.
public partial class Board
{
    // 이번 처리 단계에서 InsertEffectNext로 앞에 넣은 개수 — 같은 효과가 유발한 반응끼리 유발 순서를 지키도록 그 뒤에 이어 붙인다.
    // ProcessNextCardEffectStep 시작과 드레인 종료 때 0으로 되돌린다.
    int insertNextIndex;

    // 큐에 효과가 남았거나 카드를 쓰는 중(입력 대기 포함)이면 true.
    public bool IsResolvingEffects => pendingEffects.Count > 0 || currentActiveCard != null;

    // 타이밍 효과: caster를 시전자로 한 사본을 큐 뒤에 넣는다. 실행은 RunEffects/ResolveEffects가 한다.
    public void EnqueueEffect(CardEffect effect, Piece caster)
    {
        pendingEffects.Enqueue(effect with { caster = caster, executeOnCaster = true });
    }

    // 반응 효과: caster를 시전자로 한 사본을 큐 맨 앞(이번 단계에서 앞서 넣은 반응 뒤)에 넣는다. 지금 처리 중인 효과가
    // 끝나고 다음 효과를 꺼낼 때 먼저 실행된다. 드레인 밖(전투 시작 배치 등)에서 넣었으면 다음 RunEffects가 처리한다.
    public void InsertEffectNext(CardEffect effect, Piece caster)
    {
        var list = new List<CardEffect>(pendingEffects);
        list.Insert(Mathf.Min(insertNextIndex, list.Count), effect with { caster = caster, executeOnCaster = true });
        insertNextIndex++;
        pendingEffects = new Queue<CardEffect>(list);
    }

    // 지금 쓰는 카드의 효과(!executeOnCaster)만 큐에서 지운다 — 카드 취소·이동 불가 등이 엔진이 넣은 효과는 건드리지 않게.
    void RemoveCardEffects()
    {
        pendingEffects = new Queue<CardEffect>(pendingEffects.Where(e => e.executeOnCaster));
    }

    // 큐에 쌓인 효과를 처리한다. 이미 처리 중이면 드레인 가드가 이어서 처리한다.
    public void RunEffects() => ProcessNextCardEffect();

    // 큐를 처리하고 빌 때까지(입력 대기 포함) 기다린다 — 로직만 기다리고 연출(motionQueue)은 기다리지 않는다.
    public IEnumerator ResolveEffects()
    {
        RunEffects();
        while (IsResolvingEffects)
            yield return null;
    }

    // 승패가 로직상 이미 정해졌는지. CombatEnded는 사망 연출이 끝나 기물이 파괴될 때(GameManager) 세팅되므로, 로직만 기다리는
    // 타이밍 진행은 보드 점유로 먼저 판단한다 — GameManager 목록과 같은 기준(소환수 제외). 죽은 기물은 사망 판정 즉시 칸에서 빠진다.
    public bool IsCombatDecided => CombatEnded || !HasNonSummonPiece(0) || !HasNonSummonPiece(1);

    bool HasNonSummonPiece(int teamId)
    {
        for (int x = 0; x < N; x++)
            for (int y = 0; y < M; y++)
            {
                Piece p = GetPieceAt(new Vector2Int(x, y));
                if (p != null && p.teamID == teamId && !p.isSummon) return true;
            }
        return false;
    }

    // 드로우 시 타이밍 훅 — CardCanvas.DrawCardsData가 카드를 한 장 뽑을 때마다 부른다. 지금은 발동하는 효과가 없다.
    // 효과를 추가할 때는 여기서 InsertEffectNext로 넣는다(카드 효과 중 드로우면 그 효과 직후, 턴 시작 드로우면 그 단계의
    // ResolveEffects가 처리한다).
    public void OnCardDrawn(Piece piece, RectTransform card)
    {
    }
}
