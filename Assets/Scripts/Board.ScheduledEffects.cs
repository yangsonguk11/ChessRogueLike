using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TurnEffect/유물처럼 "지금 실제로 카드를 쓰는 중"이 아닌 시점에 발동하는 CardEffect들을 모아
// 하나의 큐로 순차 처리한다. 실제 카드가 효과를 적용할 때 쓰는 것과 동일한 함수(ApplyCardEffectNow,
// Board.CardEffect.cs)를 그대로 재사용하되, currentActiveCard/pendingEffects/effectApplied 같은
// "진행 중인 카드" 상태는 건드리지 않으므로 다른 카드가 한창 처리되는 도중에 끼어들어도 안전하다.
public partial class Board
{
    readonly Queue<(Vector2 pos, CardEffect effect)> scheduledEffectQueue = new Queue<(Vector2, CardEffect)>();
    bool processingScheduledEffects;

    public void EnqueueScheduledEffect(Vector2 pos, CardEffect effect)
    {
        scheduledEffectQueue.Enqueue((pos, effect));
        if (!processingScheduledEffects)
            StartCoroutine(ProcessScheduledEffectQueue());
    }

    IEnumerator ProcessScheduledEffectQueue()
    {
        processingScheduledEffects = true;
        while (scheduledEffectQueue.Count > 0)
        {
            (Vector2 pos, CardEffect effect) = scheduledEffectQueue.Dequeue();
            // selectedButton 프로퍼티의 setter는 OnSelectBoard(범위 표시, self 즉시실행 등)를 트리거하므로
            // 여기선 ProcessEnemyCardEffect와 동일하게 백킹 필드를 직접 대입해 그 부작용을 피한다.
            _selectedButton = pos;
            ApplyCardEffectNow(effect, pos);
            yield return new WaitUntil(() => !queuecoroutineworking);
        }
        processingScheduledEffects = false;
    }
}
