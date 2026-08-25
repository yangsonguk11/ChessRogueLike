using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 유물(Relic) 시스템. DataManager(세이브 파일)의 ownedRelicNames로부터 이번 전투 동안 유지할
// Relic 데이터를 만들고(RelicDatabase.CreateRelic), MainCanvas.RelicIconContainer에 아이콘을
// 스폰한다(RelicDatabase.SpawnIcon) — 아이콘 자체는 순수 시각 오브젝트일 뿐, Board가 들고 있는
// Relic 데이터가 실제 효과를 담당한다. 실제 카드가 효과를 적용할 때 쓰는 것과 동일한 함수
// (ApplyCardEffectNow)를 재사용하되, 훅이 발동할 때마다 관련 유물들의 Effects를 큐에 담아
// 동기적으로 전부(순차적으로) 처리한다 — 호출부(카드 자신의 효과 등)로 넘어가기 전에 반드시 끝나야 하므로.
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

    public void TriggerRelicsOnCombatStart()
    {
        Vector2Int previousSelected = _selectedButton;
        foreach (Piece ally in GetAllAllyPieces())
            DrainRelicQueue(RelicsFor(RelicTiming.CombatStart), FindPiecePos(ally));
        _selectedButton = previousSelected;
    }

    public void TriggerRelicsOnTurnStart()
    {
        Vector2Int previousSelected = _selectedButton;
        foreach (Piece ally in GetAllAllyPieces())
            DrainRelicQueue(RelicsFor(RelicTiming.TurnStart), FindPiecePos(ally));
        _selectedButton = previousSelected;
    }

    public void TriggerRelicsOnTurnEnd()
    {
        Vector2Int previousSelected = _selectedButton;
        foreach (Piece ally in GetAllAllyPieces())
            DrainRelicQueue(RelicsFor(RelicTiming.TurnEnd), FindPiecePos(ally));
        _selectedButton = previousSelected;
    }

    // casterPos: 지금 카드를 쓰고 있는 기물의 위치, targetPos: 그 카드(의 첫 효과)가 실제로 겨냥한 위치
    // (둘 다 ExecuteEffect가 이미 알고 있는 값을 그대로 넘겨받음). 유물마다 TargetsCardTarget으로
    // 둘 중 어느 위치를 기준으로 Effects를 적용할지 고른다.
    public void TriggerRelicsOnCardUsed(Vector2Int casterPos, Vector2Int targetPos)
    {
        foreach (Relic relic in ownedRelics.Where(r => r != null && r.Timing == RelicTiming.CardUsed))
            DrainRelicQueue(relic.Effects, relic.TargetsCardTarget ? targetPos : casterPos);
    }

    public void TriggerRelicsOnHit(Piece target)
    {
        Vector2Int previousSelected = _selectedButton;
        DrainRelicQueue(RelicsFor(RelicTiming.OnHit), FindPiecePos(target));
        _selectedButton = previousSelected;
    }

    // caster: 처치를 확정한(마지막 타격을 가한) 기물. 아군일 때만 유물이 발동한다.
    public void TriggerRelicsOnKill(Piece caster)
    {
        if (caster == null || caster.teamID != 0) return;
        Vector2Int previousSelected = _selectedButton;
        DrainRelicQueue(RelicsFor(RelicTiming.OnKill), FindPiecePos(caster));
        _selectedButton = previousSelected;
    }

    List<CardEffect> RelicsFor(RelicTiming timing) =>
        ownedRelics.Where(r => r != null && r.Timing == timing)
                   .SelectMany(r => r.Effects)
                   .ToList();

    // 유물 효과 목록을 큐에 담아 casterPos를 대상으로 순차적으로(동기적으로) 전부 적용한다.
    // selectedButton 프로퍼티의 setter는 OnSelectBoard(범위 표시, self 즉시실행 등)를 트리거하므로
    // 백킹 필드를 직접 대입해 그 부작용을 피한다(ProcessEnemyCardEffect와 동일한 방식).
    void DrainRelicQueue(List<CardEffect> effects, Vector2Int casterPos)
    {
        Queue<CardEffect> queue = new Queue<CardEffect>(effects);
        while (queue.Count > 0)
        {
            _selectedButton = casterPos;
            ApplyCardEffectNow(queue.Dequeue(), casterPos);
        }
    }
}
