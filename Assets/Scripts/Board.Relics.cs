using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 유물(Relic) 시스템. DataManager(세이브 파일)의 ownedRelicNames로부터 이번 전투 동안 유지할
// Relic 데이터를 만들고(RelicDatabase.CreateRelic), MainCanvas.RelicIconContainer에 아이콘을
// 스폰한다(RelicDatabase.SpawnIcon) — 아이콘 자체는 순수 시각 오브젝트일 뿐, Board가 들고 있는
// Relic 데이터가 실제 효과를 담당한다. 훅이 발동하면 관련 유물들의 Effects를 발동 기물을 시전자로 한 사본으로
// 효과 큐(pendingEffects)에 넣는다 — 턴 시작/종료·전투 시작은 타이밍 효과라 큐 뒤에(EnqueueEffect), 카드 사용 시·
// 피격 시·처치 시는 반응 효과라 큐 앞에(InsertEffectNext) 넣어 유발한 효과 직후에 실행되게 한다.
public partial class Board
{
    // 이번 전투 동안 유지되는 유물 목록.
    public List<Relic> ownedRelics = new List<Relic>();

    // InitBoard에서, 그리고 상점 구매 직후에도 호출: 세이브 파일의 소유 유물 이름들로부터
    // ownedRelics와 파티 아이콘 바(MainCanvas.RelicIconContainer)를 처음부터 다시 만든다.
    // 아이콘 바를 먼저 비우고 다시 스폰하므로, 몇 번을 호출해도 중복 아이콘이 생기지 않는다.
    public void LoadOwnedRelics()
    {
        ownedRelics.Clear();
        ClearRelicIcons();

        IReadOnlyList<string> names = DataManager.Instance?.OwnedRelicNames;
        if (names == null || RelicDatabase.instance == null) return;

        foreach (string name in names)
        {
            Relic relic = RelicDatabase.instance.CreateRelic(name);
            if (relic == null) continue;
            ownedRelics.Add(relic);

            if (MainCanvas.instance?.RelicIconContainer != null)
                RelicDatabase.instance.SpawnIcon(MainCanvas.instance.RelicIconContainer, name);
        }
    }

    void ClearRelicIcons()
    {
        RectTransform container = MainCanvas.instance?.RelicIconContainer;
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    // 타이밍 효과: 아군마다 큐 뒤에 넣는다. 처리는 호출부(InitBoard의 전투 시작 처리 / 턴 시작·종료 코루틴)가 한다.
    // 전투 시작은 배치하며 모아둔 스폰 시 효과 다음에 실행된다.
    public void TriggerRelicsOnCombatStart() => EnqueueRelicsForAllies(RelicTiming.CombatStart);
    public void TriggerRelicsOnTurnStart() => EnqueueRelicsForAllies(RelicTiming.TurnStart);
    public void TriggerRelicsOnTurnEnd() => EnqueueRelicsForAllies(RelicTiming.TurnEnd);

    void EnqueueRelicsForAllies(RelicTiming timing)
    {
        List<CardEffect> effects = RelicsFor(timing);
        if (effects.Count == 0) return;
        foreach (Piece ally in GetAllAllyPieces())
            foreach (CardEffect effect in effects)
                EnqueueEffect(effect, ally);
    }

    // casterPos: 지금 카드를 쓰고 있는 기물의 위치, targetPos: 그 카드(의 첫 효과)가 실제로 겨냥한 위치
    // (둘 다 ExecuteEffect가 이미 알고 있는 값을 그대로 넘겨받음). 유물마다 TargetsCardTarget으로
    // 둘 중 어느 칸의 기물을 시전자로 Effects를 적용할지 고른다 — 그 칸에 기물이 없으면 발동하지 않는다.
    // 반응 효과라 카드의 첫 효과 직후, 두 번째 효과보다 먼저 실행된다.
    public void TriggerRelicsOnCardUsed(Vector2Int casterPos, Vector2Int targetPos)
    {
        foreach (Relic relic in ownedRelics.Where(r => r != null && r.Timing == RelicTiming.CardUsed))
        {
            Vector2Int pos = relic.TargetsCardTarget ? targetPos : casterPos;
            Piece piece = pos.x >= 0 && pos.x < N && pos.y >= 0 && pos.y < M ? GetPieceAt(pos) : null;
            if (piece == null) continue;
            foreach (CardEffect effect in relic.Effects)
                InsertEffectNext(effect, piece);
        }
    }

    // 공격 피해를 받은 아군(Piece.GetDamage)이 시전자. 반응 효과라 그 피해를 준 효과 직후에 실행된다.
    public void TriggerRelicsOnHit(Piece target)
    {
        foreach (CardEffect effect in RelicsFor(RelicTiming.OnHit))
            InsertEffectNext(effect, target);
    }

    // caster: 처치를 확정한(마지막 타격을 가한) 기물. 아군일 때만 유물이 발동한다.
    public void TriggerRelicsOnKill(Piece caster)
    {
        if (caster == null || caster.teamID != 0) return;
        foreach (CardEffect effect in RelicsFor(RelicTiming.OnKill))
            InsertEffectNext(effect, caster);
    }

    List<CardEffect> RelicsFor(RelicTiming timing) =>
        ownedRelics.Where(r => r != null && r.Timing == timing)
                   .SelectMany(r => r.Effects)
                   .ToList();
}
