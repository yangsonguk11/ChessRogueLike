# ChessRogueLike

체스 기물과 덱빌딩 카드 시스템을 결합한 Unity 로그라이크 게임입니다. 체스판 위에서 카드를 사용해 기물을 움직이고, 공격하고, 버프/디버프를 걸어가며 층을 클리어합니다.

<img width="1919" height="1081" alt="image" src="https://github.com/user-attachments/assets/47691209-ec3f-478d-bf60-b5b945c16ff8" />

## 게임 개요

- **보드 전투**: 체스판(그리드) 위에서 아군/적 기물이 카드를 사용해 이동, 공격, 방어막, 힐 등을 수행합니다.
- **덱빌딩**: 기물마다 개인 덱을 가지며, 전투 보상과 상점에서 카드를 획득해 덱을 강화합니다.
- **유물(Relic)**: 상점에서 구매하는 영구 강화 아이템으로 전투 전반에 영향을 줍니다.
- **상태 효과**: 중독, 화상, 재생, 기절, 강화/약화, 가시(반격) 등 다양한 상태 효과 시스템을 제공합니다.
- **맵 진행**: 노드 기반 맵을 따라 전투 / 상점 / 휴식 / 이벤트 노드를 선택하며 진행하고, 마지막 층을 클리어하면 런이 종료됩니다.
- **소환 시스템**: 보스와 일반 적을 `AutoPiece`로 통합한 기물 소환 로직을 제공합니다.

## 기술 스택

- **엔진**: Unity `6000.3.10f1`
- **언어**: C#

## 프로젝트 구조

```
Assets/
  Scripts/
    Board.cs, Board.*.cs   # 보드(전투) 로직을 책임별로 분리한 partial class 모음
                            # (InputHandler, CardEffect, Combat, Animation, TurnControl,
                            #  RangeUI, Relics, ScheduledEffects 등)
    Cards/                 # 카드별 효과 구현 (공격, 이동, 힐, 방어막, 광역기 등)
    Relics/                # 유물 효과 구현
    GameManager.cs         # 씬 전역 상태(아군/적 목록, 레벨 종료/보상 흐름) 관리
    TurnManager.cs         # 턴 진행 관리
    DataManager.cs         # 런 진행 상태(층, 노드, 보유 기물 등) 저장
    *Database.cs / I*Database.cs
                            # 카드/유물/기물/레벨 등 데이터 조회를 담당하는 싱글턴 + 인터페이스
  SO/                      # ScriptableObject 데이터 (기물 정보, 사거리 정보 등)
  Prefab/                  # 기물, UI, 카드 프리팹
  Scenes/                  # TitleScene, MainScene, CardTestScene

```

## 시작하기

1. Unity Hub에서 `6000.3.10f1` 버전의 에디터로 이 프로젝트 폴더를 엽니다.
2. `Assets/Scenes/TitleScene.unity`를 실행합니다.
3. `Assets/Scenes/CardTestScene.unity`는 카드 동작을 개별적으로 테스트하기 위한 씬입니다 (`CardTestBootstrap.cs`, `CardTestHandDealer.cs` 참고).

## 아키텍처 메모

- 보드(`Board`) 클래스는 책임별 partial class로 분리되어 있어, 입력 처리·카드 효과·전투 계산·애니메이션·턴 진행 등을 각각의 파일에서 관리합니다.
- 매니저 싱글턴(`GameManager`, `TurnManager`, `CardDatabase` 등)은 인터페이스(`IGameManager`, `ITurnManager`, `ICardDatabase` 등)를 통해 접근하도록 되어 있습니다(자세한 내용은 아래 "싱글턴 + 인터페이스 구조" 참고).
- 카드 효과(`CardEffect`)는 명명된 필드를 가진 `record`로 정의되어 있어 호출부에서 인자의 의미를 명확히 알 수 있습니다.
- 보드 좌표는 `Vector2Int`로 통일되어 있습니다.

## 카드 사용 처리 흐름

카드 한 장은 `Card.effects`(`List<CardEffect>`)를 가지며, `Board`는 이를 큐(`Queue<CardEffect> pendingEffects`)에 담아 **등록된 순서대로 하나씩** 처리합니다. 관련 코드: [Board.CardEffect.cs](Assets/Scripts/Board.CardEffect.cs).

1. **카드 선택/드롭**: 손패에서 카드를 클릭하거나 드래그해 보드에 드롭하면 `CardCanvas.UseCard(handnum)` → `CommitNowUsingCard()`를 거쳐, 타겟팅이 필요한 카드는 `Board.UseCard(Card card)`가 호출됩니다.
2. **`Board.UseCard`**: 이전 상태(`lockedCaster`, `effectApplied`)를 초기화하고, `card.effects`를 전부 `pendingEffects` 큐에 채운 뒤 `ProcessNextCardEffect()`를 시작합니다.
3. **`ProcessNextCardEffect`**: 큐의 다음 효과(`Peek`)를 보고 진행 방식을 분기합니다.
   - **적 카드(`User.Enemy`)**: `ProcessEnemyCardEffect`가 `TargetLogic`(`NearestEnemy` / `LowestHP` / `self` / `AllXInRange` 등)에 따라 대상을 자동으로 계산해 즉시 실행합니다.
   - **아군 카드(`User.Ally`)**: 효과의 `requiredMode`에 따라
     - `cardSelecting` → 카드 선택 패널(핸드/덱/버림 더미 등에서 카드 고르기)을 띄우고 플레이어의 확정을 기다립니다.
     - `Inspect`(보드 상호작용 불필요) + `pieceSelectCount > 0` → 보드에서 기물을 직접 클릭해 고르게 합니다(`RequestPieceSelection`).
     - `Inspect`(그 외) → 대상 선택 없이 즉시 실행합니다.
     - `command` / `targeting` → 플레이어가 보드를 클릭해 시전자·대상을 고를 때까지 대기합니다(`lockedCaster`로 이전 효과의 시전자를 다음 효과에도 고정 가능).
4. **효과 실행**: 대상이 정해지면 `ExecuteEffect(cardEffect, targetPos)` → `ApplyCardEffectNow`가 실제로 게임 상태를 변경합니다.
5. **다음 효과로 진행**: `ScheduleNextCardEffect()`가 현재 효과의 이동/애니메이션 코루틴(`queuecoroutineworking`)이 끝날 때까지 기다린 뒤 `ProcessNextCardEffect()`를 다시 호출합니다. 즉, **각 `CardEffect`는 이전 효과의 애니메이션까지 완전히 끝난 뒤에야 리스트 순서대로 실행**됩니다.
6. **카드 종료**: `pendingEffects`가 비면 `FinishCardUsage()`가 호출되어 (필요 시) 시전자의 이번 턴 이동을 막고, `CardCanvas.FinishUseCard()`와 `ResetBoardAfterCardUse()`로 보드 상태를 정리합니다.

### `CardEffect` 하나의 처리 순서 (`ExecuteEffect` → `ApplyCardEffectNow`)

1. **시전자 고정**: `lockCasterForNext == true`이고 뒤에 처리할 효과가 남아 있으면, 다음 효과가 참조할 시전자 위치를 고정합니다(`Move` 타입이면 이동 목적지, 그 외에는 현재 위치).
2. **유물(Relic) 훅 — 카드당 단 한 번**: 이 카드의 첫 효과가 실행되는 시점(`effectApplied`가 처음 `true`로 바뀌는 순간)에 `TriggerRelicsOnCardUsed`가 "카드 사용 시" 발동하는 유물 효과를 전부 동기적으로 먼저 처리합니다. 이 지점부터는 카드 취소가 불가능해집니다.
3. **`ApplyCardEffectNow`로 실제 적용**:
   - `targetlogic`이 `AllEnemiesInRange` / `AllAlliesInRange` / `AllPiecesInRange`면 `ExecuteAreaEffect`로 분기해, `effectRange` 오프셋(필요 시 `Directional4` / `Directional8`로 회전)으로 대상 목록을 모은 뒤 타입별 광역 함수(`AreaAttackPiece` / `AreaShieldPiece` / `AreaHealPiece`)를 적용합니다.
   - 그 외에는 `EffectType`(`Move`, `Damage`, `Heal`, `Shield`, `SelfDamage`, `Draw`, `ApplyStatus`, `ApplyTurnEffect`, `ColDamageUp`, `Summon` 등)에 따라 분기해 처리합니다.
   - `Damage` / `Heal` / `Shield`는 각각 `ResolveDamageWithColDamage` / `ResolveShieldWithBonus`로 시전자의 영구 강화 스탯(콜대미지·방어막 보너스)을 더한 뒤 적용하고, 이어서 `statusEffectType`이 설정돼 있으면 `ApplyStatusToTarget`으로 상태 효과(중독·화상·기절 등)를 함께 부여합니다.
4. **처치 시 연쇄 효과(`onKillEffect`)**: `Damage` 효과로 대상이 처치되면 `Board.Combat.cs`가 `cardEffect.onKillEffect`를 시전자 자신에게 즉시 실행합니다(예: `ExecutionerCard`는 처치 시 영구 콜대미지 증가, `LethalChargeCard`는 처치 시 코스트 회복).
5. 적용이 끝나면 다시 상위 큐 루프(`ScheduleNextCardEffect`)로 돌아가 다음 `CardEffect`를 처리합니다.

## 턴 진행 상태 머신

`TurnManager`(`TurnState`: `Player` / `Enemy` / `Processing`)가 전투의 턴 순환을 관장하고, 실제 처리는 `SendMessage`로 `Board`의 각 메서드에 위임합니다. 관련 코드: [TurnManager.cs](Assets/Scripts/TurnManager.cs), [Board.TurnControl.cs](Assets/Scripts/Board.TurnControl.cs).

1. **`StartPlayerTurnCoroutine`**: "전투 시작"(최초 1회) / "플레이어 턴" 안내 후 `currentState = Player`로 두고 `Board.TurnStart()`를 호출합니다 — 유물(`TriggerRelicsOnTurnStart`) → 아군 `TurnPhase.OwnTurnStart` 예약 효과 처리 → 기물별 이동 플래그 초기화 → 적 사거리 표시 순으로 진행됩니다.
2. **플레이어 턴 종료**: `EndTurnButton` → `EndPlayerTurnCoroutine` → `Board.AllyTurnEnd()`(예약 효과 종료 처리 → 유물 `TurnEnd` → 모든 아군 기물의 `OnTurnEnd`/상태 효과 만료 → 사용 중이던 카드 정리 → 임시 코스트 복구) → 진행 중이던 코루틴이 끝나길 대기 → `PlayAutoAllyTurnCoroutine`(자동행동 아군을 한 명씩 `GetNextMove()`로 행동시킴) → 적 턴 시작.
3. **`StartEnemyTurnCoroutine`**: "적 턴" 안내 후 `currentState = Enemy`, `Board.PlayEnemyTurnCoroutine()`이 살아있는 적을 순서대로 `GetNextMove()`(예고된 카드, 기절 상태면 스턴 카드로 대체)로 행동시킵니다 — 각 적의 행동은 `UseCard` 큐(`pendingEffects`)가 완전히 비고 애니메이션이 끝날 때까지 기다린 뒤에야 다음 적으로 넘어갑니다.
4. **턴 순환**: 적 턴이 끝나면 `EndEnemyTurn()` → `Board.EnemyTurnEnd()`(적 팀 예약 효과 종료, `OnTurnEnd`/사망 처리) → 다시 `StartPlayerTurn()`으로 돌아갑니다.
5. `TurnStateProcessing()` / `RollbackStateProcessing()`으로 카드 처리 중처럼 "턴 상태를 잠시 감춰야 하는" 구간을 표시하고 되돌릴 수 있습니다.

이벤트 레벨(상점 · 휴식 · 이벤트 노드)에서는 `Board.IsEventLevel`이 `true`라 `TurnStart` / `AllyTurnEnd`가 아예 호출되지 않아 턴이 흐르지 않습니다.

## 유물 발동 타이밍

유물(`Relic`)은 MonoBehaviour가 아닌 순수 데이터 클래스로 `Name` / `Description` / `Timing`(`RelicTiming`) / `Effects`(`List<CardEffect>`)만 가집니다. `RelicDatabase.CreateRelic`이 세이브 데이터의 유물 이름으로 리플렉션을 통해 `Assets/Scripts/Relics/` 아래의 서브클래스를 생성하고, `Board.ownedRelics`가 전투 동안 이를 들고 있습니다. 관련 코드: [Relic.cs](Assets/Scripts/Relic.cs), [Board.Relics.cs](Assets/Scripts/Board.Relics.cs).

`RelicTiming` 값과 트리거 시점:

| Timing | 트리거 시점 | 효과 대상 |
|---|---|---|
| `CombatStart` | 전투 시작 시(보드 초기화) | 각 아군 기물 자신 |
| `TurnStart` | `Board.TurnStart()`(플레이어 턴 시작) | 각 아군 기물 자신 |
| `TurnEnd` | `Board.AllyTurnEnd()`(플레이어 턴 종료) | 각 아군 기물 자신 |
| `CardUsed` | 카드의 첫 `CardEffect`가 실제로 실행되는 순간(`ExecuteEffect`) | `TargetsCardTarget`에 따라 카드를 낸 기물 또는 그 카드가 겨냥한 대상 |
| `OnHit` | 공격이 적중했을 때 | 맞은 대상 기물 |
| `OnKill` | 아군이 적을 처치했을 때 | 처치를 확정한 기물 |

발동 시점마다 `DrainRelicQueue`가 해당 타이밍의 모든 유물 `Effects`를 큐에 모아, 카드 효과와 동일한 함수(`ApplyCardEffectNow`)로 **동기적으로 순차 처리**합니다 — 즉 유물도 카드와 같은 `CardEffect` 파이프라인을 재사용합니다. 특히 `CardUsed`는 '카드를 손에서 사용했을 때' 실행되는 부분으로, 카드 자신의 효과가 실행되기 전에 반드시 먼저 끝나도록 되어 있습니다.

## 싱글턴 + 인터페이스 구조

매니저/데이터베이스 클래스(`GameManager`, `TurnManager`, `CardDatabase`, `RelicDatabase` 등)는 여전히 `public static X instance` 형태의 싱글턴이지만, 각각 좁은 인터페이스(`IGameManager`, `ITurnManager`, `ICardDatabase`, `IRelicDatabase`, `ILevelDatabase`, `IRangeInfoSODatabase`, `IPieceEffectDatabase`, `IGameDataStore`, `IMap`)를 함께 구현합니다.

- 인터페이스는 소비하는 쪽이 실제로 쓰는 메서드만 노출합니다. 예를 들어 [ICardDatabase.cs](Assets/Scripts/ICardDatabase.cs)는 `SpawnCard` / `SpawnSprite` / `PickRandomDistinct` / `GetAllCardNames` 네 가지만 선언하고, `CardDatabase`가 가진 나머지 내부 구현(카드 프리팹 리스트 등)은 감춥니다.
- 소비하는 코드는 구체 클래스가 아니라 인터페이스 타입의 지역 변수로 받아서 사용합니다:
  ```csharp
  ICardDatabase cardDb = CardDatabase.instance; // ShopCanvas, ShopCardSlot, ResultCanvas 등
  ```

