# ChessRogueLike

체스 기물과 덱빌딩 카드 시스템을 결합한 Unity 로그라이크 게임입니다. 체스판 위에서 카드를 사용해 기물을 움직이고, 공격하고, 버프/디버프를 걸어가며 층을 클리어합니다.

<img width="1919" height="1081" alt="image" src="https://github.com/user-attachments/assets/47691209-ec3f-478d-bf60-b5b945c16ff8" />

## 게임 개요

- **보드 전투**: 체스판(그리드) 위에서 아군/적 기물이 카드를 사용해 이동, 공격, 방어막, 힐 등을 수행합니다.
- **덱빌딩**: 기물마다 개인 덱을 가지며, 전투 보상과 상점에서 카드를 획득해 덱을 강화합니다.
- **유물(Relic)**: 상점에서 구매하는 영구 강화 아이템으로 전투 전반에 영향을 줍니다.
- **상태 효과**: 중독, 화상, 재생, 기절, 강화/약화, 가시(반격), 도발 등 다양한 상태 효과 시스템을 제공합니다.
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
                            #  RangeUI, Relics, ScheduledEffects(효과 큐 공용 API) 등)
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

카드 한 장은 `Card.effects`(`List<CardEffect>`)를 가지며, `Board`는 이를 큐(`Queue<CardEffect> pendingEffects`)에 담아 **등록된 순서대로 하나씩** 처리합니다. 관련 코드: [Board.CardEffect.cs](Assets/Scripts/Board.CardEffect.cs). 설계 의도와 코드·커밋 근거는 [docs/CardEffect_Pipeline.md](docs/CardEffect_Pipeline.md)에 정리돼 있습니다.

1. **카드 잡기**: 손패에서 카드를 드래그해 사용 영역에 들이면 `CardCanvas.UseCard(handnum)`이 사용 가능 여부를 검사하고 `Card.PrepareUseEffects`로 이번 사용 효과(`Card.UseEffects`)를 정합니다 — 시전자가 이동 불가면 처음 나오는 이동 효과부터 뒤는 빠지고, 남는 효과가 없으면 거부됩니다. 이어서 `CommitNowUsingCard()`가 `Board.ShowUseEligibilityPreview`로 범위만 미리 보여줍니다(**효과 큐는 아직 비어 있음**). 조준 카드(`Card.NeedsTargeting` — 이번 사용 효과 중 처음 나오는 command/targeting 효과가 있음, 단 Self 카드의 self 효과는 제외)는 NowUsing 자리로 가고 화살표가 뜹니다.
2. **사용 확정(드롭) → `Board.UseCard`**: 드롭(조준 완료) 순간 `CardCanvas`가 `Board.UseCard(Card card)`를 호출합니다. 이전 상태(`effectApplied` 등)를 초기화하고, `card.UseEffects`를 카드 시전자를 기록한 사본(`CardEffect.caster`)으로 `pendingEffects` 큐에 채운 뒤 `ProcessNextCardEffect()`를 시작합니다. 조준 카드는 이어서 시전자 확정(`ConfirmCasterOnDrop`)과 드롭 칸 클릭(`ButtonClicked`)으로 첫 조준 효과까지 실행됩니다. 효과마다 이 시전자의 현재 위치를 시전자 칸으로 쓰므로, 앞 효과로 이동했으면 다음 효과는 새 위치 기준으로 처리됩니다.
3. **`ProcessNextCardEffect`**: 큐의 다음 효과(`Peek`)를 보고 진행 방식을 분기합니다.
   - **적 카드(`User.Enemy`)**: `ProcessEnemyCardEffect`가 `TargetLogic`(`NearestEnemy` / `LowestHP` / `self` / `AllXInRange` 등)에 따라 대상을 자동으로 계산해 즉시 실행합니다.
   - **아군 카드(`User.Ally`)**: 효과의 `requiredMode`에 따라
     - `cardSelecting` → 카드 선택 패널(핸드/덱/버림 더미 등에서 카드 고르기)을 띄우고 플레이어의 확정을 기다립니다.
     - `Inspect`(보드 상호작용 불필요) + `pieceSelectCount > 0` → 보드에서 기물을 직접 클릭해 고르게 합니다(`RequestPieceSelection`).
     - `Inspect`(그 외) → 대상 선택 없이 즉시 실행합니다.
     - `command` / `targeting` → 첫 효과는 드롭으로 시전자가 확정되고 대상을 고를 때까지 대기합니다. 두 번째 이후 효과는 카드를 쓴 기물을 현재 위치에서 다시 선택한 것처럼 처리해, `self`면 바로 실행하고 아니면 그 효과의 사거리를 띄워 클릭을 기다립니다.
4. **효과 실행**: 대상이 정해지면 `ExecuteEffect(cardEffect, targetPos)` → `ApplyCardEffectNow`가 실제로 게임 상태를 변경합니다.
5. **다음 효과로 진행**: `ScheduleNextCardEffect()`가 곧바로 `ProcessNextCardEffect()`를 다시 호출합니다. `ApplyCardEffectNow`가 게임 상태를 동기적으로 확정하고 연출만 `motionQueue`에 넣으므로, 다음 효과의 로직은 앞 효과의 애니메이션을 기다리지 않습니다. 연출은 `motionQueue`에서 등록 순서대로 하나씩 끝까지 재생되므로, **화면에는 각 `CardEffect`의 연출이 리스트 순서대로, 앞 연출이 끝난 뒤에 나옵니다**. 연출이 재생되는 동안에도 다음 카드를 사용할 수 있습니다.
6. **카드 종료**: 카드를 쓰는 중에 `pendingEffects`가 비면 `FinishCardUsage()`가 호출되어 (필요 시) 시전자의 이번 턴 이동을 막고, `CardCanvas.FinishUseCard()`와 `ResetBoardAfterCardUse()`로 보드 상태를 정리한 뒤 `currentActiveCard`를 비웁니다.

### 효과 큐 하나로 모든 효과를 처리한다

카드 효과뿐 아니라 턴 효과(`TurnEffect`), 유물, 상태이상 틱(독·화상·재생), 처치 시 효과(`onKillEffect`), 소환 시 효과(`Piece.onSpawnCards`), 기물 선택 결과까지 **모든 `CardEffect`가 같은 `pendingEffects` 큐**로 실행됩니다. 공용 진입점은 [Board.ScheduledEffects.cs](Assets/Scripts/Board.ScheduledEffects.cs)에 있습니다.

- **타이밍 효과** — 턴 시작/종료·전투 시작처럼 정해진 타이밍에 발동하는 효과는 `EnqueueEffect(effect, caster)`로 큐 **뒤**에 넣고, `ResolveEffects()`로 큐가 빌 때까지 처리한 뒤 다음 타이밍으로 넘어갑니다(아래 "턴 진행 상태 머신").
- **반응 효과** — 처치 시·피격 시·소환 시·카드 사용 시·드로우 시처럼 효과 처리 도중에 유발되는 효과는 `InsertEffectNext(effect, caster)`로 큐 **앞**에 넣습니다. 유발한 효과 직후, 다음 효과보다 먼저(같은 효과가 여러 반응을 유발하면 유발 순서대로) 실행됩니다.
- 카드 밖에서 넣는 효과는 모두 `executeOnCaster`라 입력 모드를 보지 않고 **시전자 칸에서** 바로 실행되며, 카드 사용 확정(에너지 차감·카드 사용 시 유물)을 하지 않습니다. 큐 안의 `!executeOnCaster` 항목은 항상 지금 쓰는 카드의 효과입니다(카드 취소·이동 불가는 이 항목만 지움).
- 시전자가 앞 효과로 죽어 보드에 없으면 그 효과는 건너뜁니다.
- **소환 시 효과는 자기 대상만** 가능합니다 — 소환된 기물 칸에서 실행되며 `TargetLogic`으로 대상을 고르지 않습니다.
- 독·화상 틱은 `SelfDamage`, 재생 틱은 `Heal`(self) 효과로 실행됩니다. 자해(`SelfDamage`)·턴 DoT(`TurnDamageStart/End`)·독·화상·가시 반격·이벤트 피해(대화 선택지 `DamageAllAllies`)는 **비공격 피해**(`GetDamage(isAttack: false)`)라 피격 시(`OnHit`) 유물과 타격음이 없습니다(가시 반격은 반격 효과음만 재생).
- 드로우 시 타이밍은 훅(`Board.OnCardDrawn`)만 있고 아직 발동하는 효과는 없습니다.

### `CardEffect` 하나의 처리 순서 (`ExecuteEffect` → `ApplyCardEffectNow`)

1. **카드 사용 확정 — 카드당 단 한 번**: 카드 자신의 첫 효과가 실행되는 시점(`effectApplied`가 처음 `true`로 바뀌는 순간)에 에너지를 차감하고, `TriggerRelicsOnCardUsed`가 "카드 사용 시" 유물 효과를 반응 효과로 큐 앞에 넣습니다 — 이 첫 효과 직후, 카드의 두 번째 효과보다 먼저 실행됩니다. 이 지점부터는 카드 취소가 불가능해집니다.
2. **`ApplyCardEffectNow`로 실제 적용**:
   - `targetlogic`이 `AllEnemiesInRange` / `AllAlliesInRange` / `AllPiecesInRange`면 `ExecuteAreaEffect`로 분기해, `effectRange` 오프셋(필요 시 `Directional4` / `Directional8`로 회전)으로 대상 목록을 모은 뒤 타입별 광역 함수(`AreaAttackPiece` / `AreaShieldPiece` / `AreaHealPiece`)를 적용합니다.
   - 그 외에는 `EffectType`(`Move`, `Damage`, `Heal`, `Shield`, `SelfDamage`, `Draw`, `ApplyStatus`, `ApplyTurnEffect`, `ColDamageUp`, `Summon` 등)에 따라 분기해 처리합니다.
   - `Damage` / `Heal` / `Shield`는 각각 `ResolveDamageWithColDamage` / `ResolveShieldWithBonus`로 시전자의 영구 강화 스탯(콜대미지·방어막 보너스)을 더한 뒤 적용하고, 이어서 `statusEffectType`이 설정돼 있으면 `ApplyStatusToTarget`으로 상태 효과(중독·화상·기절 등)를 함께 부여합니다.
   - **이동공격**: `Move`로 적 칸에 부딪히는 경우와 `Damage`에 `countsAsMoveAttack`이 켜진 경우(이동공격 판정) 모두 [Board.Combat.cs](Assets/Scripts/Board.Combat.cs)의 `ResolveMoveAttackHit` 하나를 거칩니다. 다음 이동공격 버프(`NextMoveAttackEffect`) 소모, 기물의 이동공격 스플래시(`MoveAttackRangeInfoSO`), 연쇄(`Piece.OnMoveAttackPerformed`), 가시 반격이 똑같이 적용되고 `hitCount`도 반영됩니다. 실제 이동공격만 대상 앞 칸까지 다가가고 처치 시 그 칸으로 전진하며, 이동공격 판정은 제자리에서 대상 바로 앞의 가상 도착 칸을 기준으로 스플래시를 펼칩니다.
3. **처치 시 연쇄 효과(`onKillEffect`)**: `Damage` 효과로 대상이 처치되면 `Board.Combat.cs`가 처치 시 유물과 `cardEffect.onKillEffect`(시전자 자신 대상)를 반응 효과로 큐 앞에 넣어, 이 효과 직후에 실행합니다(예: `ExecutionerCard`는 처치 시 영구 콜대미지 증가, `LethalChargeCard`는 처치 시 코스트 회복).
4. 적용이 끝나면 다시 상위 큐 루프(`ScheduleNextCardEffect`)로 돌아가 다음 `CardEffect`를 처리합니다.

## 턴 진행 상태 머신

`TurnManager`(`TurnState`: `Player` / `Enemy` / `Processing`)가 전투의 턴 순환을 관장하고, 각 턴 경계의 처리는 `Board`의 턴 코루틴에 위임합니다. 관련 코드: [TurnManager.cs](Assets/Scripts/TurnManager.cs), [Board.TurnControl.cs](Assets/Scripts/Board.TurnControl.cs), [Board.TurnEffects.cs](Assets/Scripts/Board.TurnEffects.cs).

각 턴 경계는 **타이밍의 연속**입니다. 타이밍마다 발동할 효과를 효과 큐에 넣고 `ResolveEffects()`로 큐가 빌 때까지 처리한 뒤 다음 타이밍으로 넘어가므로, 앞 타이밍의 결과(사망, 새로 걸린 턴 효과 등)가 다음 타이밍에 반영됩니다. 효과로 승패가 정해지면(`IsCombatDecided` — 보드에 소환수가 아닌 아군 또는 적이 남지 않음) 다음 타이밍으로 넘어가지 않습니다. `ResolveEffects`는 로직만 기다리고, 연출 대기는 `TurnManager`의 `WaitUntil(!queuecoroutineworking)` 지점이 맡습니다.

1. **`StartPlayerTurnCoroutine`**: 입력을 잠그고(`PlayerInputLocked`) "전투 시작"(최초 1회) / "플레이어 턴" 안내 후 `currentState = Player`로 두고 `Board.PlayerTurnStartCoroutine()`을 실행합니다 — 턴 시작 유물 → 아군 `OwnTurnStart` 턴 효과 → 플래그 초기화·전원 5장 드로우(드로우 시 훅)·에너지 회복 → 적 예고 잠금·사거리 표시. 이 처리가 끝나야 입력과 턴 종료가 열립니다.
2. **플레이어 턴 종료**: `EndTurnButton` → `EndPlayerTurnCoroutine` → `Board.AllyTurnEndCoroutine()`(쓰던 카드 정리 — 확정 전이면 손패로 되돌림 → 아군 `OwnTurnEnd` 턴 효과 → 턴 종료 유물 → 아군 상태이상 틱 → 적 방어도 초기화 → 임시 코스트 복구·손패 버림) → 연출 대기 → `PlayAutoAllyTurnCoroutine`(자동행동 아군을 한 명씩 `GetNextMove()`로 행동시킴) → 적 턴 시작.
3. **`StartEnemyTurnCoroutine`**: "적 턴" 안내 후 `currentState = Enemy`, `Board.PlayEnemyTurnCoroutine()`이 적 `OwnTurnStart` 턴 효과를 처리한 뒤 살아있는 적을 순서대로 `GetNextMove()`(예고된 카드, 기절 상태면 스턴 카드로 대체)로 행동시킵니다 — 각 적의 행동은 효과 큐가 완전히 비고(`!IsResolvingEffects`) 애니메이션이 끝날 때까지 기다린 뒤에야 다음 적으로 넘어갑니다.
4. **턴 순환**: 적이 모두 행동하면 `Board.EnemyTurnEndCoroutine()`(적 `OwnTurnEnd` 턴 효과 → 적 상태이상 틱 → 아군 방어도 초기화) → 다시 `StartPlayerTurn()`으로 돌아갑니다.
5. `TurnStateProcessing()` / `RollbackStateProcessing()`으로 카드 처리 중처럼 "턴 상태를 잠시 감춰야 하는" 구간을 표시하고 되돌릴 수 있습니다.

이벤트 레벨(상점 · 휴식 · 이벤트 노드)에서는 `Board.IsEventLevel`이 `true`라 턴 코루틴이 아예 호출되지 않아 턴이 흐르지 않습니다(소환 시 효과도 발동하지 않음).

## 유물 발동 타이밍

유물(`Relic`)은 MonoBehaviour가 아닌 순수 데이터 클래스로 `Name` / `Description` / `Timing`(`RelicTiming`) / `Effects`(`List<CardEffect>`)만 가집니다. `RelicDatabase.CreateRelic`이 세이브 데이터의 유물 이름으로 리플렉션을 통해 `Assets/Scripts/Relics/` 아래의 서브클래스를 생성하고, `Board.ownedRelics`가 전투 동안 이를 들고 있습니다. 관련 코드: [Relic.cs](Assets/Scripts/Relic.cs), [Board.Relics.cs](Assets/Scripts/Board.Relics.cs).

`RelicTiming` 값과 트리거 시점:

| Timing | 트리거 시점 | 효과 대상 |
|---|---|---|
| `CombatStart` | 전투 시작 시(보드 초기화, 소환 시 효과 다음) — 타이밍 효과 | 각 아군 기물 자신 |
| `TurnStart` | 플레이어 턴 시작의 첫 타이밍 — 타이밍 효과 | 각 아군 기물 자신 |
| `TurnEnd` | 플레이어 턴 종료, 아군 턴 효과 다음 — 타이밍 효과 | 각 아군 기물 자신 |
| `CardUsed` | 카드의 첫 `CardEffect`가 실제로 실행된 직후 — 반응 효과 | `TargetsCardTarget`에 따라 카드를 낸 기물 또는 그 카드가 겨냥한 칸의 기물(칸이 비면 발동 안 함) |
| `OnHit` | 아군이 공격 피해를 받은 효과 직후(독·화상·자해·턴 DoT·가시 반격·이벤트 피해 등 비공격 피해 제외) — 반응 효과 | 맞은 대상 기물 |
| `OnKill` | 아군이 적을 처치한 효과 직후 — 반응 효과 | 처치를 확정한 기물 |

발동 시점마다 해당 타이밍의 모든 유물 `Effects`를 발동 기물을 시전자로 한 사본으로 효과 큐(`pendingEffects`)에 넣습니다 — 타이밍 효과는 큐 뒤(`EnqueueEffect`), 반응 효과는 큐 앞(`InsertEffectNext`). 즉 유물도 카드와 같은 `CardEffect` 파이프라인과 같은 큐를 씁니다(위 "효과 큐 하나로 모든 효과를 처리한다").

## 싱글턴 + 인터페이스 구조

매니저/데이터베이스 클래스(`GameManager`, `TurnManager`, `CardDatabase`, `RelicDatabase` 등)는 여전히 `public static X instance` 형태의 싱글턴이지만, 각각 좁은 인터페이스(`IGameManager`, `ITurnManager`, `ICardDatabase`, `IRelicDatabase`, `ILevelDatabase`, `IRangeInfoSODatabase`, `IPieceEffectDatabase`, `IGameDataStore`, `IMap`)를 함께 구현합니다.

- 인터페이스는 소비하는 쪽이 실제로 쓰는 메서드만 노출합니다. 예를 들어 [ICardDatabase.cs](Assets/Scripts/ICardDatabase.cs)는 `SpawnCard` / `SpawnSprite` / `PickRandomDistinct` / `GetAllCardNames` 네 가지만 선언하고, `CardDatabase`가 가진 나머지 내부 구현(카드 프리팹 리스트 등)은 감춥니다.
- 소비하는 코드는 구체 클래스가 아니라 인터페이스 타입의 지역 변수로 받아서 사용합니다:
  ```csharp
  ICardDatabase cardDb = CardDatabase.instance; // ShopCanvas, ShopCardSlot, ResultCanvas 등
  ```

