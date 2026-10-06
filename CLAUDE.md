# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 응답 언어

**이 저장소의 Claude Code 세션에서는 모든 답변을 반드시 한국어로 작성한다.** 코드 식별자·명령어·파일 경로는 원문 그대로 두되, 설명·요약·질문·커밋 메시지 본문은 한국어로 쓴다.

## Jira 반영 (필수)

게임 코드·에셋, 또는 QA 문서·카드 목록 txt를 변경하는 작업을 마치면, 응답을 끝내기 전에 **사용자에게 묻지 않고 바로** Jira에 반영한다(사용자가 코멘트·상태 전환·이슈 생성 모두 자동 진행을 승인함).

- 사이트: `https://agilewhaleshark.atlassian.net` (cloudId `eeb83fff-65ea-4dea-9f35-31bbbff6d1e8`), 프로젝트 키 **`CRL`**
- 상태: `해야 할 일` → `진행 중` → `검토 중` → `완료`
- 기존 이슈 구조:
  - 에픽 `CRL-4` "QA 테스트 케이스" — 하위에 `[QA] N. 섹션명 (케이스 수)` 작업(섹션별, [docs/QA_TestCases.md](docs/QA_TestCases.md)와 1:1)과 `[이슈] ...` 작업(QA 문서 "알려진 이슈 인덱스"와 1:1, 라벨 `known-issue`, `qa`)
  - 에픽 `CRL-39` "직업별 카드 목록" — 하위에 `[카드 목록] 전사/소환사 ...` 작업([전사_소환사_카드목록.txt](전사_소환사_카드목록.txt)와 대응)

반영 절차:
1. JQL(`project = CRL AND text ~ "<클래스명/키워드>"` 등)로 변경과 관련된 이슈를 찾는다.
2. 관련 이슈마다 무엇을 어떻게 바꿨는지(파일, 핵심 변경점)를 한국어 코멘트로 남긴다.
3. 변경으로 문제가 해결된 이슈는 `완료`로 전환한다. 작업이 일부만 끝났으면 `진행 중`으로 둔다.
4. 대응하는 이슈가 없는 새 작업(새 카드, 새로 발견한 버그·설계 미비 등)은 새 이슈를 만든다 — 버그성 이슈는 `[이슈] ` 접두어 + 라벨 `known-issue`, `qa`로 `CRL-4` 아래에, 카드 목록 변경은 `CRL-39` 아래에 둔다. 본문은 기존 이슈 형식(**내용** / **위치** / **관련 테스트 케이스** / **확인할 것**)을 따른다. 이 프로젝트는 새 이슈가 `진행 중` 상태로 생성되므로, 아직 착수하지 않은 작업이면 생성 직후 `해야 할 일`로 전환한다(전환 ID `2`).
5. QA 문서·카드 목록 txt를 고쳤다면 대응하는 `[QA]`·`[카드 목록]` 이슈의 제목(케이스 수)·본문도 맞춰 갱신한다.
6. 마지막 응답에 반영한 이슈 키와 한 일(코멘트/전환/생성)을 짧게 적는다.

질문에 답만 하거나 코드를 읽기만 한 경우에는 반영하지 않는다.

## 프로젝트 개요

체스판 위 기물 + 기물별 개인 덱을 쓰는 덱빌딩 로그라이크. Unity `6000.3.10f1` (URP, Input System, TextMeshPro, DOTween). 코드 주석·UI 텍스트·문서는 모두 한국어다. 게임 개요와 핵심 흐름(카드 처리 순서, 턴 상태 머신, 유물 발동 타이밍, 싱글턴+인터페이스 구조)은 [README.md](README.md)에 상세히 정리돼 있으니 해당 시스템을 건드리기 전에 먼저 읽는다.

## 빌드 / 검증

- **컴파일 확인**: `dotnet build Assembly-CSharp.csproj` (에디터 스크립트는 `Assembly-CSharp-Editor.csproj`). 몇 초면 끝난다.
  - `.csproj`는 Unity가 생성하며 `<Compile Include>`가 명시적으로 나열돼 있다. **새로 추가한 `.cs` 파일은 Unity 에디터가 프로젝트를 다시 읽어 csproj를 재생성하기 전까지 `dotnet build`에 포함되지 않는다.**
  - 대안: `"/c/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath . -logFile compile_check.log` — 에디터가 이 프로젝트를 열고 있으면 실패한다.
- **C# 언어 버전은 9.0** (`LangVersion`). `record` + `init`은 [IsExternalInit.cs](Assets/Scripts/IsExternalInit.cs) 셈으로 동작한다. file-scoped namespace, `record struct`, global using 등 C# 10+ 문법은 쓸 수 없다.
- **자동화 테스트 없음** (test-framework 패키지는 있지만 테스트 어셈블리가 없다). 동작 확인은 에디터에서 수동으로 한다:
  - `Assets/Scenes/TitleScene.unity` → 실제 게임 시작점.
  - `Assets/Scenes/CardTestScene.unity` → 카드 단독 테스트. [CardTestBootstrap.cs](Assets/Scripts/CardTestBootstrap.cs)가 아군 1기·지정 레벨로 강제 진입시키고, 실제 세이브를 백업했다가 씬 종료 시 복원한다.
  - 수동 QA 케이스: [docs/QA_TestCases.md](docs/QA_TestCases.md). 정확한 수치·조건 기반이라 카드/로직을 바꾸면 이 문서와 [전사_소환사_카드목록.txt](전사_소환사_카드목록.txt)도 함께 갱신한다.

## 아키텍처 핵심

### 모든 효과는 `CardEffect` 파이프라인 하나로 흐른다
[Card.cs](Assets/Scripts/Card.cs)의 `CardEffect`(불변 `record`)가 게임의 모든 "효과" 단위다. 플레이어 카드뿐 아니라 **적/자동 아군 AI 행동(`AutoPiece.actionCards`도 `Card` 컴포넌트), 유물(`Relic.Effects`), 턴 효과(`TurnEffect`), 처치 시 효과(`onKillEffect`), 소환 시 효과(`Piece.onSpawnCards`)** 모두 `Board.ApplyCardEffectNow`([Board.CardEffect.cs](Assets/Scripts/Board.CardEffect.cs))를 거친다. 새 동작은 보통 기존 `CardEffect` 필드 조합으로 만들고, 불가능할 때만 `EffectType`을 추가해 `ApplyCardEffectNow`에 분기를 넣는다.

- `CardEffect`는 절대 변경하지 않는다. 시전자 기록 등은 `effect with { caster = piece }` 사본으로 만든다 — `TurnEffect`가 카드의 중첩 `CardEffect` 객체를 공유하기 때문.
- 카드 처리 중이 아닌 시점(턴 효과·유물)의 효과는 `Board.EnqueueScheduledEffect`([Board.ScheduledEffects.cs](Assets/Scripts/Board.ScheduledEffects.cs))로 넣는다. 진행 중인 카드 상태(`pendingEffects`, `effectApplied` 등)를 건드리지 않아 끼어들어도 안전하다.

### 로직과 연출의 분리
게임 상태(점유, HP, 손패 리스트 소속 등)는 **즉시 동기적으로** 확정되고, 애니메이션 코루틴은 `Board.motionQueue`([Board.Animation.cs](Assets/Scripts/Board.Animation.cs))에 쌓여 나중에 순서대로 재생된다. 예: `ApplyMoveOccupancy`가 이동 애니메이션 전에 칸 점유를 옮겨둔다([Board.Occupancy.cs](Assets/Scripts/Board.Occupancy.cs)).
- 상태 판정은 항상 논리 상태로 한다(칸의 `GetPieceScript()`, `CardCanvas.cards` 소속 여부 등). 화면 위치나 `handNumber`는 연출 지연 때문에 신뢰할 수 없다.
- 연출 코루틴은 enqueue 시점 이후에 실행되므로 필요한 대상(기물 등)을 파라미터로 받아야 한다 — 실행 시점에 칸을 다시 조회하면 이미 비어 있을 수 있다.
- 다른 컴포넌트의 연출을 보드 연출 순서에 맞추려면 `Board`의 공개 래퍼로 `motionQueue`에 넣는다.

### Board는 책임별 partial class
`Board.cs` + `Board.*.cs`가 하나의 클래스(`Board.instance`)다. 보드 좌표는 `Vector2Int`, 크기는 `N × M`. `BoardMode`(`Inspect`/`command`/`targeting`/`cardSelecting`)가 입력 해석을 결정한다.

### 기물과 데이터
- `Piece` → `AutoPiece`(적 + 자동행동 아군; `actionCards`를 순환하며 다음 행동을 예고, 기절 시 `StunnedCard`로 대체). `teamID == 0`이 아군, `1`이 적.
- `PieceInfo`(SO, `Assets/SO/`)가 기물 템플릿(스탯, 사거리 `RangeInfoSO`, 기본 덱, 직업 `JobInfo`). `JobInfo`(`Assets/SO/Jobs/`)의 `RewardCardPool`이 전투 보상 카드 풀.
- `LevelData`(SO, `Assets/Prefab/Level/`)가 전투/이벤트 레벨. `placements`의 `name`이 비면 플레이어 스폰 위치, 채워져 있으면 `PieceDatabase`의 프리팹 이름. `LevelDatabase.floorPools`가 층별 랜덤 레벨 풀. 이벤트 레벨(휴식·상점·이벤트)은 `Board.IsEventLevel`이라 턴이 흐르지 않는다.
- 데이터 조회 싱글턴(`CardDatabase`, `PieceDatabase`, `RelicDatabase` 등)은 `Assets/Prefab/Database.prefab`에 붙어 있고, 소비 측은 `I*Database` 인터페이스 타입으로 받는다.

### 세이브는 "이름 문자열"로 참조한다
`DataManager`가 `Application.persistentDataPath/save.json`에 `JsonUtility`로 저장한다(파일 기록은 맵에서 다음 노드를 고를 때만). 저장 데이터는 모두 이름으로 원본을 찾는다:
- 카드 = 카드 **프리팹 이름**(`Card.cardID`, `CardDatabase.SpawnCard`가 채움)
- 기물 = `PieceInfo.PieceName` / 프리팹 이름
- 유물 = **클래스 이름** (`RelicDatabase.CreateRelic`이 `Type.GetType` 리플렉션으로 생성)
- 레벨 = `LevelData` 에셋 이름

따라서 이 이름들을 바꾸면 기존 세이브가 깨진다. 직렬화 필드 이름을 바꿀 때는 `[FormerlySerializedAs]`를 붙인다(예: `AutoPiece.actionCards`).

## 카드 추가 절차

1. `Assets/Scripts/Cards/XxxCard.cs` 작성 — `Card` 상속, `Awake()`에서 `Name`/`Cost`/`type`/`dragDropTarget`/`effects`를 코드로 설정한다(인스펙터 값은 대부분 `Awake()`가 덮어씀). `EffectDescription`을 오버라이드해 카드 설명을 제공한다. 희귀도는 기본 일반이며, 희귀 카드만 `public override CardRarity Rarity => CardRarity.Rare;`를 넣는다(Awake가 아닌 프로퍼티라 상점이 프리팹에서 바로 읽는다 — 가격·희귀 1장 진열 보장에 쓰임). 기존 카드(예: [ChainMoveAttackCard.cs](Assets/Scripts/Cards/ChainMoveAttackCard.cs))를 참고.
2. Unity 메뉴 **Tools > Cards > Card Prefab Generator**([CardPrefabGenerator.cs](Assets/Editor/CardPrefabGenerator.cs))로 프리팹 생성 — `AttackCard.prefab`을 템플릿으로 복제하고, 같은 이름의 스크립트를 연결하고, `Database.prefab`의 `cardPrefabs`에 등록한다. **클래스 이름 = 프리팹 이름 = cardID**여야 한다.
3. 프리팹의 `effectRange` 리스트(`RangeInfoSO`)를 채운다 — 대부분의 카드가 `Awake()`에서 `effectRange[0]`을 읽으므로 비어 있으면 예외가 난다.
4. 획득 경로에 추가: 직업 보상 풀(`Assets/SO/Jobs/*.asset`) 또는 `PieceInfo`의 기본 덱. 프리팹/등록이 없는 카드 스크립트는 게임에서 도달 불가능하다.

적 전용 카드는 `user = User.Enemy`로 두며, 대상 선택은 `TargetLogic`으로 자동 계산된다.

## Unity 에셋을 텍스트로 편집할 때

`.prefab`/`.asset`/`.unity`는 YAML이고 참조는 `.meta`의 `guid`로 연결된다. 새 에셋을 직접 만들면 고유한 32자리 hex guid를 가진 `.meta`도 함께 만들어야 하고, 기존 에셋을 복제할 때 guid를 재사용하면 안 된다. 새 소스 파일은 UTF-8로 저장한다(일부 기존 파일은 CP949 인코딩이 섞여 있다).
