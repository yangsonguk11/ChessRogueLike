# ChessRogueLike QA 테스트 케이스

이 문서는 `Assets/Scripts/` 전체(Board 16개 partial class, Piece/Card 핵심 엔진, `Cards/` 폴더 카드 스크립트 94종, 매니저/UI/맵/상점/다이얼로그/유물 시스템)와 `Assets/LevelData.cs`, `Assets/CardsPanel.cs`, `Assets/SO/Jobs/*.asset`, Confluence "카드 목록" 페이지를 근거로 작성된 QA 테스트 케이스 모음이다.

## 사용법

- **ID 규칙**: `TC-<섹션약어>-NNN` (예: `TC-UI-014`). 카드 효과의 공통 규칙은 Confluence [카드 효과 기본 TC](https://agilewhaleshark.atlassian.net/wiki/spaces/CRL/pages/98477)의 `TC-FX-<영역>-NNN`이고, 6장 카드 행이 이를 참조한다. 6장(개별 카드 기능)은 카드 1장에 `TC-CARD-NNN` 하나를 두고, 그 카드의 확인 항목을 `TC-CARD-NNN-태그`로 나눈다(예: `TC-CARD-001-COST` = AttackCard 사용 후 코스트 차감, `TC-CARD-001-TARGET` = 대상 선택). 태그는 영문 대문자 한 단어로, 기본 태그는 COST·TARGET·RANGE·DMG·HIT·KILL·MOVE·SHIELD·HEAL·STATUS·TURN·DRAW·DISCARD·ADDCARD·SUMMON·SPAWN·GRAVE·EXILE·PERSIST·DESC·AI이고, 같은 측면이 둘 이상이면 의미가 드러나는 태그를 새로 만든다(예: `-HIT`/`-OVERKILL`).
- **표 컬럼**: `ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고`(6장은 `ID` 뒤에 `기본 TC` 열이 있다)
- **비고란**: 코드 분석 중 발견된 버그성 이슈/설계 미비 사항은 관련 케이스의 비고란에 `⚠` 표시로 남긴다. 정상 동작 확인이 목적인 일반 케이스는 비고란을 비워둔다.
- 모든 케이스는 실제 스크립트의 정확한 수치·조건을 근거로 하며, 코드가 변경되면 함께 갱신되어야 한다.
- **용어**: 게임 화면의 "힘"은 기물 스탯 `colDamage`(이동공격 피해의 기준, 이전 표기 "이동공격력")를 말한다. 기물 정보창 라벨, 카드 설명, 상태 텍스트("힘 +N") 모두 이 용어를 쓴다.

## 목차

1. [세이브/로드 & 신규 게임](#1-세이브로드--신규-게임)
2. [보드 초기화 & 레벨 진입](#2-보드-초기화--레벨-진입)
3. [이동 & 이동공격](#3-이동--이동공격) — Confluence로 이관
4. [피해/회복/보호막 처리](#4-피해회복보호막-처리) — Confluence로 이관
5. [카드 엔진 공통 로직](#5-카드-엔진-공통-로직) — Confluence로 이관
6. [개별 카드 기능](#6-개별-카드-기능) — [6.1 기본 덱](#61-기본-덱) · [6.2 전사](#62-전사) · [6.3 소환사](#63-소환사) · [6.4 적](#64-적)
7. [상태이상/버프 시스템](#7-상태이상버프-시스템) — Confluence로 이관
8. [턴 진행 & 적 AI](#8-턴-진행--적-ai)
9. [유물 시스템](#9-유물-시스템)
10. [맵 생성 & 진행](#10-맵-생성--진행)
11. [상점](#11-상점)
12. [보상/결과 화면](#12-보상결과-화면)
13. [다이얼로그/이벤트 오브젝트](#13-다이얼로그이벤트-오브젝트)
14. [UI/입력/애니메이션 타이밍](#14-ui입력애니메이션-타이밍)
15. [승리/패배 조건](#15-승리패배-조건)
16. [직업 시스템](#16-직업-시스템)
17. [교차 시스템 회귀 테스트](#17-교차-시스템-회귀-테스트)
18. [무덤(Grave) 시스템](#18-무덤grave-시스템)
19. [카드 정보창(호버 툴팁)](#19-카드-정보창호버-툴팁)
20. [보드 칸 정보창](#20-보드-칸-정보창)

## 알려진 이슈 인덱스

코드 분석 중 발견되었으며 아래 섹션의 개별 케이스 비고란에 표시된 항목들의 요약:

- 프리팹이 없어 정상 플레이로는 도달 불가능한 orphan 카드: `AreaAttackCard`, `AreaShieldCard`, `RecoverShieldCard`, `ThornCard`, `ZoneHealCard`, `ZoneShieldCard` (섹션 6 미사용 카드)
- `ThornCard` 툴팁 텍스트가 `statusDuration - 1`을 표시해 실제 지속시간(2턴)과 1턴 차이 남 (섹션 6 미사용 카드)
- `StatusEffects.cs`의 `MovementDisabledEffect` 주석은 "게임플레이 미적용"이라 하지만 실제로는 Move 카드 차단 로직에 적용되어 있음 (카드 효과 기본 TC, TC-FX-MOVE-020)
- `GameManager.TriggerDefeat`가 로그 출력만 하는 플레이스홀더로, 실제 패배 화면/흐름 없음 (섹션 15)
- 동시 전멸(마지막 아군·마지막 적 같은 틱에 사망) 시 승리/패배 판정 경쟁 상태 가능성 (섹션 15)
- `RangeInfoSODatabase.GetRangeInfoSO`는 조회 실패 시 에러 로그 없이 `null` 반환 (다른 Database류와 불일치) (카드 효과 기본 TC)
- 상점에 팔 유물이 없음. 유물이 `ShieldRelic`, `VampiricFangRelic` 2종뿐이고 둘 다 시작 유물이라, 보유 유물을 빼면 유물 칸이 비어 나옴 (섹션 11)
- `ShopObject.prefab`이 휴식용 `RestPieceInfo`(이름 "모닥불")와 모닥불 메시를 그대로 써서 상점이 모닥불로 보임 (섹션 11)
- 상점에서 카드 제거를 누르면 카드 선택 창(`CardCanvas/CardSelectionPanel`, CardCanvas Sort Order 1)이 상점 패널(`MainCanvas/ShopPanel`, Sort Order 2) 뒤에 가려짐. 선택 창에 Override Sorting Canvas(Sort Order 3)와 Graphic Raycaster가 필요. 카드 획득 연출은 `CardCanvas.cardFxLayer`(Sort Order 4)로 해결됨 (섹션 11)
- 다이얼로그 보상 선택(카드 받을 기물 선택, 카드 제거)에는 취소 버튼이 없어 되돌릴 수 없음. 상점 흐름은 취소 가능 (섹션 13)
- `Map.cs` 노드 생성 시 다음 층 고아 노드 방지 안전망이 있으나 반복 생성으로 회귀 검증 필요 (섹션 10)
- `SummonerPieceInfo`의 기물 이름이 `Summoner`에서 `SummonerAlly`로 바뀌어, 변경 전에 만든 세이브의 소환사 기물은 `PieceDatabase`에서 찾지 못해 스폰에 실패할 수 있음 (섹션 1)
- 적이 죽으면 다른 적 전원에게도 "무덤 +1" 텍스트가 뜸. 적은 무덤을 쓰는 카드가 없어 의미 없는 연출 (섹션 18)
- 도발 소환 카드(`TauntSummonCard`) 설명에 소환수 `tauntAutoAlly`의 스폰 시 효과(방어도 2)가 표시되지 않음. `autoally`는 스폰 시 효과가 없음 (TC-CARD-038-SPAWN, CRL-27)
- `autoally.prefab`이 `onSpawnCards`의 옛 이름(`onSummonCards`)으로 저장되어 있어 `FormerlySerializedAs`에 의존함 (섹션 17)
- 손패 부채꼴 위치(`CardCanvas.heightOffset`)가 화면 중앙 기준이라, CanvasScaler(너비 기준)에서 16:9가 아닌 비율이면 손패가 화면 아래로 잘리거나(21:9) 위로 떠 보임(16:10·4:3) (섹션 14)
- 전투 후 보상 카드(그리고 상점·저장 덱 보기 카드)의 피해·방어도 수치에 활성 기물의 일시 버프가 반영돼 표시됨 — `Card.PreviewCaster`로 수정, 에디터 확인 대기. 전투 덱/버림/소멸 보기는 원본 카드 얼굴(`ShowFaceOf`)이라 손패처럼 버프가 반영되는 게 정상 (섹션 12)
- `CardsPanel` 전투 덱/버림 보기가 표시 이름으로 카드를 다시 띄워 바뀐 코스트를 반영하지 않고, 덱 순서(=드로우 순서)를 노출하며, 소멸 더미 보기가 없었음 — 코드 수정 완료. 씬에 소멸 보기 버튼(OnClick → `CardsPanel.ShowExile`) 추가와 에디터 확인 대기 (섹션 14)

---

## 1. 세이브/로드 & 신규 게임

**관련 스크립트**: `DataManager.cs`, `MainMenuCanvas.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-SAVE-001 | 세이브 파일 없음(최초 실행) | 메인메뉴에서 "기본" 기물로 새 게임 시작 | `basicPieceinfo` 기준 로스터 1기, 기본 덱 9장(AttackCard×3, DefenseCard×2, SummonCard×1, MoveCard×2, StunCard×1) 생성, 골드 100(`DataManager.startingGold`), `cardRemoveCount` 0 | |
| TC-SAVE-002 | 세이브 파일 없음 | "소환사" 기물로 새 게임 시작(`StartGameWithPiece(1)`) | `summonerPieceinfo` 기준 로스터 생성 | |
| TC-SAVE-003 | `ResolveStartingPieceInfo`에 범위 밖 인덱스(예: 5, -1) 전달 | 신규 게임 시작 | `basicPieceinfo`로 폴백되어 정상 시작(크래시 없음) | |
| TC-SAVE-004 | 신규 게임 시작 직후 | 보유 유물 확인 | `ShieldRelic`, `VampiricFangRelic` 두 개가 항상 시작 유물로 존재 | |
| TC-SAVE-005 | 골드 0 상태 | `SpendGold(음수값)` 유발 상황(비정상 입력 경로) | 차감 실패(`false`) 반환, 골드 변화 없음 | |
| TC-SAVE-006 | 골드 10 보유 | `SpendGold(0)` 호출 | 항상 성공(`true`), 골드 변화 없음 | |
| TC-SAVE-007 | 골드 5 보유 | `SpendGold(10)` 호출(부족) | 실패(`false`), 골드 차감 없음 | |
| TC-SAVE-008 | 골드 보유 중 | `AddGold(0)` 또는 음수 호출 | 골드 변화 없음(무시) | |
| TC-SAVE-009 | 진행 중인 런 존재 | 맵에서 노드 선택 전 애플리케이션 강제 종료 후 재실행 | 마지막으로 `SetNextLevel`이 호출된(=마지막 노드 선택) 시점까지만 복구되고, 그 이후 진행(카드 획득/스탯 변화 등)은 유실 | ⚠ `SaveToFile()`이 노드 선택 시에만 호출되므로 설계상 의도된 동작인지 확인 필요 |
| TC-SAVE-010 | 구버전 세이브 파일(팀 공유 `deckCardIDs` 필드 사용) 로드 | `LoadFromFile()` 실행 | `MigrateLegacyDeckIfNeeded()`가 값을 `pieceData[0].deckCardIDs`로 이전, 크래시 없음 | |
| TC-SAVE-011 | `DataManager.Instance`가 아직 생성되지 않은 타이틀 씬 | `MainMenuCanvas.StartGameWithPiece`로 새 게임 시작 | 세이브 파일 직접 삭제 + `PlayerPrefs`에 pending index 저장 후, 다음 씬에서 `LoadFromFile()`이 이를 소비하고 키 삭제 | |
| TC-SAVE-012 | TC-SAVE-011 수행 후 `LoadFromFile()`을 두 번 호출 | 재호출 | 두 번째 호출 시 pending index가 이미 삭제되어 중복 적용되지 않음 | |
| TC-SAVE-013 | 기물 여러 마리 보유 | `AddCardToPieceDeck(pieceIndex, cardName)` 호출 | 해당 인덱스 기물의 덱에만 카드 추가, 다른 기물 덱은 불변 | |
| TC-SAVE-014 | 특정 기물 덱에 카드 3장 | `RemoveCardFromDeck(pieceIndex, 잘못된 index)` 호출(범위 밖) | `false` 반환, 덱 변화 없음, 크래시 없음 | |
| TC-SAVE-015 | 게임 진행 중 | "세이브 초기화" 실행 | 맵 진행/로스터/유물 모두 초기화되고 새 게임과 동일한 시작 상태로 복귀 | |
| TC-SAVE-016 | `SummonerPieceInfo` 이름 변경(`Summoner` → `SummonerAlly`) 전에 소환사로 시작한 세이브 | 이어하기로 전투 레벨 진입 후 결과 화면까지 진행 | 소환사 기물이 정상 스폰되고, 보상 카드가 소환사 보상 풀에서 제시됨 | ⚠ 세이브의 `pieceName`이 `Summoner`로 남아 있으면 `PieceDatabase.GetPiece`가 프리팹을 찾지 못해 스폰 실패(에러 로그), 보상도 전체 카드 풀로 폴백됨. 마이그레이션 또는 세이브 초기화 안내 필요 |
| TC-SAVE-017 | `cardRemoveCount` 필드가 없는 이전 세이브 | 이어하기 후 상점 진입 | `cardRemoveCount`가 0으로 읽혀 제거 가격 50G, 크래시 없음 | |
| TC-SAVE-018 | 소환사로 새 게임 시작(세이브의 `pieceName`이 `SummonerAlly`) | 첫 전투 레벨 진입 | 소환사 기물이 플레이어 스폰 위치에 정상 스폰, "플레이어 기물 스폰 실패" 에러 로그 없음 | 플레이어 기물은 세이브의 `pieceName`으로 프리팹을 찾으며 `PieceDatabase.GetPiece`는 대소문자를 구분한다. 프리팹 파일 이름이 `PieceInfo.PieceName`과 대소문자까지 같아야 함. 2026-10-07 `summonerAlly.prefab` → `SummonerAlly.prefab`으로 수정 |

---

## 2. 보드 초기화 & 레벨 진입

**관련 스크립트**: `Board.cs`, `Assets/LevelData.cs`, `Board.Event.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-BOARD-001 | `DataManager.Instance.Pieces`가 빈 상태 | 전투 씬 진입 | `Board.Start()`가 에러를 로그하고 강제 종료(에디터) 또는 `Application.Quit()` | |
| TC-BOARD-002 | `LevelData.placements`에 빈 이름(플레이어 스폰 마커) 2개, 로스터 인원 3명 | 레벨 진입 | 마커 부족분(3번째 기물)은 기본 좌표 `(2,2)`에 스폰되어 다른 기물과 겹칠 수 있음 | ⚠ 스폰 겹침 가능성, 레벨 데이터 설계 시 마커 수 ≥ 로스터 수 보장 필요 |
| TC-BOARD-003 | `LevelData.placements`에 이름이 채워진 적 배치 다수 | 레벨 진입 | `PieceDatabase.GetPiece`로 정확히 매칭되는 프리팹이 스폰, teamID==1 자동 등록 | |
| TC-BOARD-004 | 존재하지 않는 프리팹 이름을 가진 `placements` 항목 | 레벨 진입 | 해당 위치 스폰 실패, `Debug.LogError` 출력, 나머지 스폰은 정상 진행 | |
| TC-BOARD-005 | 다이얼로그에서 `triggerCombat`으로 특정 `LevelData` 지정 | 선택지 클릭 | `Board.pendingLevel`에 지정 레벨이 저장되고 씬 재로드 후 해당 레벨로 진입(랜덤 레벨 무시) | |
| TC-BOARD-006 | 저장된 `NextLevelName` 존재, `pendingLevel` 없음 | 일반 진행으로 레벨 진입 | `ResolveLevelData()`가 저장된 이름 우선 사용 | |
| TC-BOARD-007 | `pendingLevel`/`NextLevelName` 모두 없음, 0층 | 레벨 진입 | 0층 랜덤 레벨 선택, 없으면 인스펙터 폴백(`leveldata`) 사용 | |
| TC-BOARD-008 | `LevelData.levelType == Event`, `eventType == Rest` | 레벨 진입 | 전투 시작 유물 트리거(`TriggerRelicsOnCombatStart`)가 발동하지 않음(이벤트 레벨 스킵) | |
| TC-BOARD-009 | 전투 레벨 진입 직후 | 유물 보유 상태 확인 | `TriggerRelicsOnCombatStart` 발동(예: `ShieldRelic` +3 보호막) | |
| TC-BOARD-010 | 승리 후 생존 아군(소환수 제외) | `SavePlayerPiecesToDataManager()` 호출 경로(레벨 클리어) | 생존한 비-소환 기물만 `DataManager`에 재저장, `pieceDataIndex`가 새 리스트 위치로 재인덱싱 | |
| TC-BOARD-011 | 전투 중 소환수(팀0 AutoAlly) 다수 생성 | 레벨 클리어 | 소환수는 `SavePlayerPiecesToDataManager`에서 제외되어 다음 레벨로 이어지지 않음 | |
| TC-BOARD-012 | 다이얼로그에서 `SpawnPiece` 호출, 빈 칸 없음(보드 가득 참) | 기물 즉시 스폰 시도 | 실패 반환(`false`), `Debug.LogError` 출력, 크래시 없음 | |
| TC-BOARD-013 | 기물 기본 정보 PieceInfo 통일 — 적·소환수의 프리팹 `hp`/`colDamage` 값을 PieceInfo와 다르게 바꿔 둠 | `L3-Bastion`·`L3-Colossus`·`L2` 진입, `GreaterSummonCard` 사용 | 프리팹 값은 무시되고 PieceInfo 값으로 시작: Boss-Bastion 50, Boss-Colossus 120, Enemy-GrowingRook 힘 4, GreaterAutoAlly 15/8. 아군은 세이브 값(현재 체력·영구 강화) 유지 | `Piece.Awake`가 PieceInfo에서 채움 |
| TC-BOARD-014 | 기물 이름 = `PieceInfo.PieceName` | 적·아군 칸 정보창, 기물 게이지 목록, 보상 화면 라벨 확인 | 모두 PieceInfo 이름(예: `Enemy-StrikerKnight`, `Warrior`)으로 표시. 이전에 비어 있거나 잘못된 표시 이름(summoned의 "Thorn Knight")이 없어짐 | |
| TC-BOARD-015 | `PieceDatabase.GetPiece`가 PieceInfo 이름으로 조회 | ① 레벨 진입 ② 소환 카드 ③ 대화 합류(`Dialogue_Floor6CardChoice`의 sin2) ④ 전투 후 다음 노드에서 아군 재배치 | 모두 정상 스폰. `Database.prefab`을 열었을 때 이름 중복·누락 경고 없음 | sin2 프리팹은 `sin2info`로 연결(이전엔 sin과 같은 PieceInfo를 공유) |

---

## 3. 이동 & 이동공격

이 장의 TC는 Confluence [카드 효과 기본 TC](https://agilewhaleshark.atlassian.net/wiki/spaces/CRL/pages/98477) 페이지로 옮겼다(2026-10-18). 카드 효과 기본 기능 TC(`TC-FX-<영역>-NNN`)가 되어 6장 카드 TC의 `기본 TC` 열에서 참조해 확인한다. 옛 ID → 새 ID 대응표는 그 페이지 맨 아래에 있다.

카드와 무관한 TC는 직접 확인하도록 다른 장으로 옮겼다: TC-MOVE-020 → 14장 TC-UI-048.

---

## 4. 피해/회복/보호막 처리

이 장의 TC는 Confluence [카드 효과 기본 TC](https://agilewhaleshark.atlassian.net/wiki/spaces/CRL/pages/98477) 페이지로 옮겼다(2026-10-18). 카드 효과 기본 기능 TC(`TC-FX-<영역>-NNN`)가 되어 6장 카드 TC의 `기본 TC` 열에서 참조해 확인한다. 옛 ID → 새 ID 대응표는 그 페이지 맨 아래에 있다.

카드와 무관한 TC는 직접 확인하도록 다른 장으로 옮겼다: TC-DMG-005~007(On-Hit 유물) → 9장 TC-RELIC-009~011, TC-DMG-009·011 → 17장 TC-REG-022·023.

---

## 5. 카드 엔진 공통 로직

이 장의 TC는 Confluence [카드 효과 기본 TC](https://agilewhaleshark.atlassian.net/wiki/spaces/CRL/pages/98477) 페이지로 옮겼다(2026-10-18). 카드 효과 기본 기능 TC(`TC-FX-<영역>-NNN`)가 되어 6장 카드 TC의 `기본 TC` 열에서 참조해 확인한다. 옛 ID → 새 ID 대응표는 그 페이지 맨 아래에 있다.

카드와 무관한 TC는 직접 확인하도록 다른 장으로 옮겼다: TC-ENGINE-009·010·012 → 8장 TC-TURN-018~020, TC-ENGINE-014·022·023·024 → 14장 TC-UI-049~052, TC-ENGINE-021 → 17장 TC-REG-021.

---

## 6. 개별 카드 기능

**관련 스크립트**: `Assets/Scripts/Cards/*.cs`, `Assets/Prefab/Cards/*.prefab`, `Assets/Prefab/Database.prefab`(`cardPrefabs`), 적 프리팹의 `actionCards`·`onSpawnCards`

카드 1장마다 번호 하나(`TC-CARD-NNN`)를 두고, 그 카드에서 확인할 항목을 `TC-CARD-NNN-태그`로 나눈다. 카드 기능만 다룬다 — 소환 시 효과 실행 순서 같은 공용 메커니즘은 [17장](#17-교차-시스템-회귀-테스트), 수호자 격노·결속·보스 패턴 순환 같은 적 기물 동작은 [8장](#8-턴-진행--적-ai)에 있다. 카드 수치·설명은 Confluence "카드 목록"·"적 목록" 페이지와 같아야 한다.

| 절 | 카드 | 번호 |
| --- | --- | --- |
| [6.1 기본 덱](#61-기본-덱) | 보상 풀에 없고 기본 덱으로만 얻는 카드 3장 | TC-CARD-001 ~ 003 |
| [6.2 전사](#62-전사) | 전사 보상 풀 30장(Confluence 카드 목록 순서) + 획득 경로 없는 전사 카드 2장 | TC-CARD-004 ~ 035 |
| [6.3 소환사](#63-소환사) | 소환사 보상 풀 24장(Confluence 카드 목록 순서) + 획득 경로 없는 소환사 카드 2장 | TC-CARD-036 ~ 061 |
| [6.4 적](#64-적) | Confluence 적 목록에 나오는 적 행동 카드 + 적이 넣는 WoundCard | TC-CARD-062 ~ 084 |

### 기본 TC 참조

코스트·드롭 대상·사거리·피해·이동공격·상태이상·설명 표시·적 카드 예고 같은 카드 효과의 공통 규칙은 Confluence [카드 효과 기본 TC](https://agilewhaleshark.atlassian.net/wiki/spaces/CRL/pages/98477)에 `TC-FX-<영역>-NNN`으로 있다(범위 이름의 모양도 그 페이지의 "범위 표기"에 있다). 카드 표의 `기본 TC` 열이 그 행이 가져다 쓰는 기본 TC다.

- **참조 행**(`기본 TC` 있음): 기본 TC의 절차·기대 결과를 이 카드의 값으로 확인한다. 사전조건·기대 결과 열에는 이 카드의 값만 적는다. 기본 TC가 여러 개면 모두 확인한 뒤 체크한다.
- **고유 행**(`기본 TC` 비어 있음): 그 카드에만 있는 동작이다.
- 플레이어 카드의 `-COST`·`-RANGE`·`-DESC` 행은 각각 TC-FX-USE-001·TC-FX-AIM-001·TC-FX-DESC-001, 적 카드의 `-AI` 행은 TC-FX-AI-001이 기본이다.

### 미사용 카드 (번호 없음)

스크립트는 있지만 `Database.prefab`에 등록된 프리팹이 없거나 어떤 적·레벨에도 쓰이지 않아 정상 플레이로 나오지 않는 카드. 획득·등장하지 않는지만 확인한다.

| 카드 | 상태 | 비고 |
| --- | --- | --- |
| `AreaAttackCard` | 프리팹 없음 | ⚠ orphan/내부 전용 여부 기획 확인 |
| `AreaShieldCard` | 프리팹 없음 | ⚠ orphan |
| `RecoverShieldCard` | 프리팹 없음 | ⚠ orphan |
| `ZoneHealCard` | 프리팹 없음 | ⚠ orphan |
| `ZoneShieldCard` | 프리팹 없음 | ⚠ orphan |
| `ThornCard` | 프리팹 없음 | ⚠ orphan. 설명이 `statusDuration - 1`(1턴)을 표시해 실제 지속 2턴과 다름 |
| `EnemyMoveAndColDamageUp5Card` | 어떤 적에도 연결 안 됨 | ⚠ 미사용 변형 |
| `EnemyWideChargedAttackCard` | 어떤 적에도 연결 안 됨 | 오른 힘만큼 커지는 광역 10 피해 |
| `EnemyChargeCard` | 테스트 레벨(`TelegraphTestLevel`)의 ChargeKnight만 사용 | 피해 없는 예고 포즈 |

`TauntCard`는 tauntAutoAlly의 스폰 시 효과로 쓰여 TC-CARD-038-SPAWN에서 다룬다.

### 6.1 기본 덱

#### TC-CARD-001 AttackCard
코스트 1 · 공격 · 기본 덱(전사 3장) · 적 기물에 드롭 · 사거리 `AttackRangeInfo` · 설명 "적에게 4 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-001-COST | TC-FX-USE-001, TC-FX-USE-002 | 에너지 3 | 적에게 사용 | 에너지 1 차감, 카드는 버린 카드 더미로 |  |
| TC-CARD-001-TARGET | TC-FX-DROP-001 | 사거리 안에 적 2기(체력 다름) | 체력이 **높은** 적에게 드롭 / 아군에게 드롭 | 드롭한 적에게 4 피해(체력이 낮은 적으로 바뀌지 않음) / "적 기물에 사용해야 합니다" | `TargetLogic.LowestHP`는 적 AI에서만 쓰임 |
| TC-CARD-001-RANGE | TC-FX-AIM-001 | 사거리 밖에만 적 | 그 적에게 드롭 | "사거리 밖입니다", 변화 없음 |  |
| TC-CARD-001-DMG | TC-FX-DMG-001, TC-FX-VULN-001 | 시전자 힘 +2(예: TC-CARD-005 사용) / 대상 취약(+1) | 적에게 사용 | 6 피해 / 5 피해 |  |
| TC-CARD-001-DESC | TC-FX-DESC-001 | 시전자 힘 +2 | 손패의 카드 확인 | "적에게 6 피해를 줍니다.", 6이 색으로 표시 |  |

#### TC-CARD-002 DefenseCard
코스트 1 · 스킬 · 기본 덱(전사 3장, 소환사 2장) · 자신 대상 · 설명 "자신에게 방어도 3를 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-002-COST | TC-FX-USE-001 | 에너지 1 이상 | 사용 | 에너지 1 차감 |  |
| TC-CARD-002-TARGET | TC-FX-DROP-004 |  | 보드의 아무 칸(적 칸 포함)에 드롭 | 카드를 낸 기물에게 적용 |  |
| TC-CARD-002-SHIELD | TC-FX-DEF-001, TC-FX-DEF-002 | 방어막 보너스 0 / 1 | 사용 | 방어도 +3 / +4 |  |
| TC-CARD-002-RESET | TC-FX-DEF-006 | 플레이어 턴에 사용(방어도 3) | 턴 종료 → 적 턴 → 적 턴 종료 | 적 턴 동안 방어도 3이 피해를 먼저 막고, 적 턴이 끝나면 남은 방어도가 0 |  |
| TC-CARD-002-DESC | TC-FX-DESC-001 | 방어막 보너스 1 | 손패 확인 | "자신에게 방어도 4를 부여합니다." |  |

#### TC-CARD-003 MoveCard
코스트 2 · 이동 · 기본 덱(전사 2장, 소환사 2장) · 아무 칸에 드롭 · 범위는 기물 이동 범위 · 설명 "이동합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-003-COST | TC-FX-USE-001 |  | 빈 칸으로 이동 | 에너지 2 차감 |  |
| TC-CARD-003-MOVE | TC-FX-MOVE-001~003, TC-FX-MOVE-005~007, TC-FX-MOVE-012, TC-FX-MOVE-019 | 이동 범위 안에 빈 칸과 적 | ① 빈 칸에 드롭 ② 적 칸에 드롭 | ① 그 칸으로 이동 ② 이동공격(힘만큼 피해, 처치 시 그 칸으로 전진, 아니면 바로 앞 칸) | 이동공격 세부는 기본 TC |
| TC-CARD-003-RANGE | TC-FX-AIM-001 |  | 이동 범위 밖 칸에 드롭 | "사거리 밖입니다" |  |
| TC-CARD-003-IMMOBILE | TC-FX-MOVE-020 | 시전자 이동 불가 | 카드 잡기 | "이동 불가 상태입니다", 손패로 복귀 |  |
| TC-CARD-003-DESC | TC-FX-DESC-001 |  | 손패 확인 | "이동합니다." |  |

### 6.2 전사

#### TC-CARD-004 ColDamageUpCard
코스트 1 · 스킬 · 소멸 · 전사 풀 · 자신 대상 · 설명 "힘을 2 올립니다. (소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-004-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-004-STATUS | TC-FX-DEF-004 | 시전자 힘 5 | 사용 | 힘 7, "힘 +2" 텍스트, 이후 이동공격·공격 카드 피해 +2 | 힘 증가량 1 → 2 |
| TC-CARD-004-PERSIST | TC-FX-DEF-004 |  | 사용 후 전투 승리 → 다음 전투 | 다음 전투에서는 원래 힘(영구 강화 아님) |  |
| TC-CARD-004-EXILE | TC-FX-USE-009 |  | 사용 | 버린 카드 더미가 아니라 소멸 더미로. 같은 전투에서 다시 뽑히지 않음 | 소멸 추가 |
| TC-CARD-004-DESC | TC-FX-DESC-003 |  | 손패 확인 | "힘을 2 올립니다. (소멸)", 끝에 "(소멸)" 한 번 |  |

#### TC-CARD-005 TempColDamageUpCard
코스트 0 · 스킬 · 전사 풀 · 자신 대상 · 설명 "이번 턴 동안 힘을 2 올립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-005-COST | TC-FX-USE-001 | 에너지 0 | 사용 | 사용됨, 에너지 0 그대로 |  |
| TC-CARD-005-STATUS | TC-FX-STATUS-001, TC-FX-STATUS-006 |  | 사용 → 정보창 → 턴 종료 | `강화 (+2)` 상태(1턴 남음)로 등록, 힘 +2. 턴 종료 시 "강화 (+2) 해제"와 함께 원래 힘 |  |
| TC-CARD-005-DESC | TC-FX-DESC-001 |  | 손패 확인 | "이번 턴 동안 힘을 2 올립니다." |  |

#### TC-CARD-006 WarmUpDamageCard
코스트 3(사용할 때마다 1 감소) · 스킬 · 전사 풀 · 자신 대상 · 설명 "힘을 3 올립니다. 코스트가 1 감소합니다. (코스트가 0이 되면 소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-006-COST | TC-FX-USE-007 | 에너지 충분 | 같은 카드를 전투 중 3번 사용 | 차감 3 → 2 → 1. 차감은 사용 전 코스트 기준(이번 감소가 소급되지 않음). 사용 후 카드의 코스트 표시가 2 → 1 → 0 | 회귀는 TC-REG-002 |
| TC-CARD-006-STATUS | TC-FX-DEF-004 |  | 사용 | 힘 +3(이번 전투 한정) |  |
| TC-CARD-006-EXILE | TC-FX-USE-008 | 코스트 1인 상태 | 사용 | 사용 직후 코스트가 0이 되며 버린 카드 더미가 아니라 소멸 더미로 | 감소는 사용 중에 일어나고 소멸 판정은 사용 끝에 함 |
| TC-CARD-006-PERSIST |  | 코스트 1까지 줄인 채 전투 승리 | 다음 전투 | 코스트 3으로 다시 시작 |  |
| TC-CARD-006-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 고정, 코스트 숫자만 바뀜 |  |

#### TC-CARD-007 ExecutionerCard
코스트 1 · 공격 · 전사 풀 · 적에게 드롭 · 사거리 `BasicRangeInfoSO` · 설명 "적에게 3 피해를 줍니다. 처치 시 힘이 영구적으로 1 오릅니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-007-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-007-RANGE | TC-FX-AIM-001 |  | 마름모 2 밖 적에게 드롭 | "사거리 밖입니다" |  |
| TC-CARD-007-KILL | TC-FX-DMG-008, TC-FX-DEF-005 | 체력 3 이하 적 | 사용 | 처치 → `colDamageBonus` 영구 +1("힘 +1"), 다음 전투에도 유지 |  |
| TC-CARD-007-NOKILL | TC-FX-DMG-008 | 체력 충분한 적 | 사용 | 3 피해, 힘 변화 없음 |  |
| TC-CARD-007-STACK | TC-FX-DEF-005 |  | 3번 처치 | 영구 보너스 +3 누적 |  |
| TC-CARD-007-DESC | TC-FX-DESC-001 | 시전자 힘 +1(영구) | 손패 확인 | "적에게 4 피해를 줍니다. …" |  |

#### TC-CARD-008 HeavyAttackCard
코스트 2 · 공격 · 전사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "적에게 6 피해를 주고, 자신도 2 피해를 받습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-008-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-008-DMG | TC-FX-DMG-001 | 시전자 방어도 0 | 적에게 사용 | 적 6 피해 + 시전자 2 자해 |  |
| TC-CARD-008-SELF | TC-FX-DMG-009 | 시전자 방어도 2 | 사용 | 자해 2가 방어도로 먼저 막힘 |  |
| TC-CARD-008-NONATK | TC-FX-DMG-009 | (On-Hit 유물이 생기면) 시전자가 On-Hit 유물 보유 | 사용 | 자해 2는 비공격 피해 — 적 타격음은 나지만 자해에는 타격음이 없고, On-Hit 유물이 발동하지 않음 | 효과 큐 단일화로 자해가 비공격 피해로 바뀜. TC-RELIC-010 |
| TC-CARD-008-NOTARGET | TC-FX-DMG-005 | 첫 타격으로 대상이 사라지는 등 적 피해가 헛스윙 | 사용 | 적 피해 없음, 자해 2는 따로 발생하는지 확인 | ⚠ 두 효과가 독립 처리돼 실패 조건이 연동되지 않음 |
| TC-CARD-008-DESC | TC-FX-DESC-001 | 시전자 힘 +2 | 손패 확인 | 적 피해만 8로 바뀌고 자해 2는 그대로 |  |

#### TC-CARD-009 DirectionalAttackCard
코스트 2 · 공격 · 전사 풀 · 아무 칸에 드롭 · 범위 `DirectionAttackRangeInfo`(4방향) · 설명 "방향을 선택해 범위 내 적에게 5 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-009-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-009-RANGE | TC-FX-AIM-004, TC-FX-AIM-005 | 네 방향 중 한쪽에 적 | 마우스로 방향을 바꾸며 겨눔 → 확정 | 범위가 마우스 방향으로 회전하고, 확정한 방향의 6칸 안 적에게만 5 피해 |  |
| TC-CARD-009-TARGET | TC-FX-AIM-007 | 범위 안에 아군과 적 | 사용 | 적만 맞고 아군은 맞지 않음 |  |
| TC-CARD-009-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-010 DoubleAttackCard
코스트 3 · 공격 · 전사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "4의 피해를 2번 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-010-COST | TC-FX-USE-001 |  | 사용 | 에너지 3 차감 |  |
| TC-CARD-010-HIT | TC-FX-DMG-006 | 체력 20 적 | 사용 | 같은 대상에 4 피해 2회(총 8). 공격 애니메이션 1회, 타격 시점에 피해 텍스트·타격음 2번이 연달아 |  |
| TC-CARD-010-OVERKILL | TC-FX-DMG-006 | 체력 4 이하 적 | 사용 | 1타에 처치, 2타 없음(헛스윙 연출·에러 없음). 피격 반응 1번 칸에 Die |  |
| TC-CARD-010-DMG | TC-FX-DMG-001 | 시전자 힘 +1 | 사용 | 타격마다 5(총 10) |  |
| TC-CARD-010-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-011 FinalAttackCard
코스트 2 · 공격 · 희귀 · 전사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "손의 카드를 모두 버리고, 버린 카드 1장당 4의 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-011-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감, 카드 자신은 버린 카드 더미로 |  |
| TC-CARD-011-HIT | TC-FX-AIM-014 | 손패에 다른 카드 3장, 체력 충분한 적 | 적에게 드롭 | 손패 3장이 버림 더미로 간 뒤 같은 대상에 4 피해 3회(총 12). 피해는 다시 클릭하지 않아도 같은 대상(`useLastTarget`) | 사용한 카드는 버린 수에 포함 안 됨 → 버림 더미 총 +4장 |
| TC-CARD-011-DMG | TC-FX-DMG-001 | 시전자 힘 +2, 손패 다른 카드 2장 | 사용 | 타격마다 6(총 12), 설명 수치도 6 |  |
| TC-CARD-011-NODISCARD |  | 이 카드가 손패의 유일한 카드 | 사용 | 사용되고 에너지 2 차감, 타격 없음(헛스윙 연출 없음), 이어서 다른 카드 사용 가능 |  |
| TC-CARD-011-CANCEL | TC-FX-USE-003, TC-FX-USE-004 |  | 카드를 든 채 사거리 확인 → 취소 | 사거리 표시, 취소하면 손패가 하나도 버려지지 않음 | 버리기는 대상을 찍는 순간 실행 |
| TC-CARD-011-OVERKILL | TC-FX-DMG-006 | 손패 다른 카드 3장, 체력 8 적 | 사용 | 2타에 처치, 3타 없음. 피격 반응 2번 칸에 Die, 3번 칸 비어 있음 |  |
| TC-CARD-011-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-012 MoveAndAttackCard
코스트 3 · 공격 · 전사 풀 · 아무 칸에 드롭(이동) → 적 클릭(공격) · 공격 사거리 `AttackRangeInfo` · 설명 "이동한 후 적에게 3 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-012-COST | TC-FX-USE-001 |  | 이동 → 공격 | 에너지 3 차감 |  |
| TC-CARD-012-MOVE | TC-FX-AIM-008 |  | 이동 범위 안 빈 칸에 드롭 | 이동 후 새 위치 기준 공격 사거리가 뜸 |  |
| TC-CARD-012-DMG | TC-FX-DMG-001 | 이동 후 사거리 안에 적 | 적 클릭 | 3 피해 |  |
| TC-CARD-012-MOVEATTACK | TC-FX-AIM-010 | 이동 칸에 적(이동공격으로 처치) | 사용 | 이동공격 처리 후, 이어지는 공격은 새 위치 기준 사거리에서 다시 대상을 고름(자동 스킵되지 않음) |  |
| TC-CARD-012-DEATH | TC-FX-AIM-011 | 이동 칸에 가시 적, 시전자 체력이 반격 피해 이하 | 그 적 칸에 드롭(이동공격) | 반격으로 시전자 사망, 이어지는 공격은 건너뛰고 카드가 정상 종료(에러·교착 없음) |  |
| TC-CARD-012-DESC | TC-FX-DESC-001 | 시전자 힘 +2 | 손패 확인 | "이동한 후 적에게 5 피해를 줍니다." | 스크립트 파일명은 `MoveandAttackCard.cs`, 클래스·프리팹은 `MoveAndAttackCard` |

#### TC-CARD-013 ChargeCard
코스트 2 · 이동 · 전사 풀 · 아무 칸에 드롭 · 설명 "이동합니다. 이동공격 시 방어도 3를 획득합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-013-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-013-SHIELD | TC-FX-MOVE-009 | 이동 범위 안에 적 | 적 칸에 드롭(이동공격) | 이동공격 후 시전자 방어도 +3 |  |
| TC-CARD-013-NOATTACK | TC-FX-MOVE-009 |  | 빈 칸으로 이동 | 방어도 변화 없음 |  |
| TC-CARD-013-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-014 BloodChargeCard
코스트 2 · 이동 · 전사 풀 · 아무 칸에 드롭 · 설명 "이동합니다. 이동공격 시 입힌 피해만큼 체력을 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-014-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-014-HEAL | TC-FX-DMG-007 | 부상 상태, 이동 범위 안에 적 1기 | 이동공격 | 회복량 = 입힌 피해(스플래시 없음) | 취약 대상은 TC-FX-VULN-010 |
| TC-CARD-014-SPLASH | TC-FX-MOVE-011 | 스플래시 이동공격 범위를 가진 기물, 부상, 스플래시로 적 2기 적중 | 이동공격 | 회복량 = 주 대상 + 스플래시 대상 피해의 합 |  |
| TC-CARD-014-NOATTACK | TC-FX-MOVE-001 |  | 빈 칸으로 이동 | 회복 없음 |  |
| TC-CARD-014-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-015 LethalChargeCard
코스트 3 · 이동 · 전사 풀 · 아무 칸에 드롭 · 설명 "이동합니다. 이동공격으로 적을 처치하면 코스트를 2 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-015-COST | TC-FX-USE-001 |  | 사용 | 에너지 3 차감 |  |
| TC-CARD-015-KILL | TC-FX-DMG-008 | 힘 이하 체력의 적 | 이동공격으로 처치 | 에너지 +2(최대치 클램프) |  |
| TC-CARD-015-NOKILL | TC-FX-DMG-008 | 체력 충분한 적 | 이동공격 | 에너지 회복 없음 |  |
| TC-CARD-015-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-016 ColDamageAttackCard
코스트 1 · 공격 · 전사 풀 · 적에게 드롭 · 사거리는 시전자 이동 범위 · 설명 "이동 범위 내 적에게 힘만큼 피해를 줍니다. (이동공격 판정)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-016-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-016-DMG | TC-FX-MOVE-013 | 시전자 힘 7 | 이동 범위 안 적에게 사용 | 피해 7(카드 자체 dmg 무시, `useColDamageAsDmg`), 시전자는 제자리 |  |
| TC-CARD-016-MOVEATTACK | TC-FX-MOVE-014~018, TC-FX-CHAIN-003 | 다음 이동공격 버프·가시 적 | 사용 | 이동공격으로 취급되어 버프 소모·가시 반격·스플래시 적용, 처치해도 전진하지 않음 | 판정 공격 세부는 기본 TC |
| TC-CARD-016-DESC | TC-FX-DESC-001 |  | 손패 확인 | 피해 수치가 현재 힘, 끝에 "(이동공격 판정)" |  |

#### TC-CARD-017 LifeDrainCard
코스트 2 · 공격 · 전사 풀 · 적에게 드롭 · 범위 `BasicRangeInfoSO` 안 적 전원 · 설명 "범위 내 적에게 2 피해를 주고, 적중한 적마다 입힌 피해만큼 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-017-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-017-DMG | TC-FX-AIM-003, TC-FX-AIM-007 | 범위 안 적 3기 | 사용 | 각 적 2 피해 |  |
| TC-CARD-017-HEAL | TC-FX-DMG-007 | 부상 상태, 범위 안 적 3기(취약 없음) | 사용 | 회복량 = 실제 입힌 피해의 합(6), 한 번에 반영 | 취약 포함은 TC-FX-VULN-010 |
| TC-CARD-017-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-018 FlameThrowingCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 턴 효과 범위 `BasicRangeInfoSO` · 설명 "턴 종료 시 주변에 2 피해를 줍니다. (3턴 지속)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-018-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-018-TURN | TC-FX-TURN-001 | 시전자 주변(마름모 2)에 적 | 사용 → 아군 턴 종료 3회 | 턴 종료마다 범위 안 적에게 2 피해, 정확히 3회 후 만료 |  |
| TC-CARD-018-DEATH | TC-FX-TURN-003 | 사용 후 시전자 사망 | 턴 진행 | 죽은 기물의 턴 효과가 더 발동하지 않음 |  |
| TC-CARD-018-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-019 ChainMoveAttackCard
코스트 3 · 스킬 · 희귀 · 소멸 · 전사 풀 · 자신 대상 · 설명 "이동 공격 시 이동 범위 내 다른 적에게 한 번 더 같은 피해를 줍니다. 다른 적이 없으면 공격한 적에게 줍니다. (버프) (소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-019-COST | TC-FX-USE-001, TC-FX-USE-009 |  | 사용 | 에너지 3 차감, 카드는 소멸 더미로 |  |
| TC-CARD-019-STATUS | TC-FX-CHAIN-007 |  | 사용 → 정보창 | 시전자에 `ChainMoveAttackBuff` 부착, 정보창에 "연쇄 이동공격" |  |
| TC-CARD-019-STACK |  | 이미 버프 보유 | 다른 장으로 재사용 | 중복 부착되지 않음(효과 1회분 그대로) |  |
| TC-CARD-019-CHAIN | TC-FX-CHAIN-001~002, TC-FX-CHAIN-004~006 | 버프 보유, 이동 범위 안에 적 2기 이상 | 이동공격 | 이동 범위 안 다른 적 중 체력이 가장 낮은 적(도발 우선)에게 같은 피해 1회. 다른 적이 없으면 공격한 적에게. 공격한 적이 이미 처치됐고 다른 적이 없으면 추가 타격 없음 | 연쇄 규칙 세부는 CRL-52 |
| TC-CARD-019-DESC | TC-FX-DESC-003 |  | 손패 확인 | "(버프) (소멸)"로 "(소멸)"이 한 번만 | CRL-82 |

#### TC-CARD-020 EmpowerMoveAttackCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 설명 "다음 이동공격의 피해가 4 증가합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-020-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-020-STATUS | TC-FX-NEXT-002 | 시전자 힘 5 | 사용 → 정보창 → 적에게 이동공격 | 사용 시 "다음 이동공격 +4" 텍스트·버프 파티클, 정보창에 턴 수 없이 `다음 이동공격 +4`. 이동공격 피해 9. 다가가는 애니메이션 직전(이동이 없으면 공격 애니메이션 직전)에 "다음 이동공격 +4 소모" 텍스트만 뜨고 버프 제거 | 중첩·배율 규칙은 TC-CARD-020-NEXT |
| TC-CARD-020-DESC | TC-FX-DESC-001 | 버프 보유, 손패에 ColDamageAttackCard | 손패 확인 → ColDamageAttackCard 사용 | ColDamageAttackCard 설명 수치가 힘 + 4로 색 표시되고, 사용하면 그 피해 + 버프 소모 |  |
| TC-CARD-020-NEXT | TC-FX-NEXT-001~010 | 다음 이동공격 버프 카드 조합(EmpowerMoveAttackCard·MightyChargeCard·StunningChargeCard, 연쇄·취약 포함) | 기본 TC 절차 | 중첩·배율·소모·유지·디스펠 규칙이 기본 TC대로 |  |

#### TC-CARD-021 MightyChargeCard
코스트 2 · 스킬 · 전사 풀 · 자신 대상 · 설명 "이번 턴 동안 다음 이동공격의 피해가 2배가 됩니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-021-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-021-STATUS | TC-FX-NEXT-003 | 시전자 힘 5 | 사용 → 이동공격 | 피해 10, 버프 소모 |  |
| TC-CARD-021-EXPIRE | TC-FX-STATUS-001 |  | 사용 → 이동공격 없이 턴 종료 | 정보창 `다음 이동공격 ×2  1턴` → 턴 종료 시 "다음 이동공격 ×2 해제" |  |
| TC-CARD-021-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-022 StunningChargeCard
코스트 2 · 스킬 · 전사 풀 · 자신 대상 · 설명 "다음 이동공격에 적중한 적에게 기절을 1턴 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-022-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-022-STATUS | TC-FX-STATUS-009, TC-FX-MOVE-010, TC-FX-NEXT-009 | 스플래시 이동공격 범위를 가진 기물 | 사용 → 적 2기를 맞히는 이동공격 | 주 대상·스플래시 대상 모두 기절(텍스트, 예고가 기절로 바뀜), 다음 적 턴에 둘 다 행동하지 않음 |  |
| TC-CARD-022-KEEP | TC-FX-NEXT-006 |  | 사용 → 이동공격 없이 턴 종료 | 버프가 다음 턴에도 남음(이동공격할 때까지 유지). 기물 게이지 목록·보드 칸 정보창에 "다음 이동공격 적중 시 1턴 기절"로 표시 | "1턴"은 걸어 줄 기절의 지속이라 이름 앞에 둠(`NextMoveAttackStatusEffect.InflictLabel`, 이름은 실제 상태의 `DisplayName`). 버프 자신의 남은 턴이 있으면 뒤에 회색으로 붙음 |
| TC-CARD-022-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-023 PersistentShieldCard
코스트 2 · 스킬 · 전사 풀 · 자신 대상 · 설명 "방어도 4를 부여하고, 다음 턴 시작 시 방어도 4를 추가로 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-023-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-023-SHIELD | TC-FX-DEF-001 | 방어막 보너스 0 | 사용 | 즉시 방어도 +4 |  |
| TC-CARD-023-TURN | TC-FX-TURN-002 |  | 사용 → 다음 자기 턴 시작 | 다음 턴 시작 시 방어도 +4 1회, 이후 만료 | 회귀 TC-REG-007 |
| TC-CARD-023-DESC | TC-FX-DESC-001 | 방어막 보너스 1 | 손패 확인 | 두 수치 모두 5 |  |

#### TC-CARD-024 ShieldCycleCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 설명 "방어도 3를 부여하고, 손의 카드 1장을 무작위로 덱 맨 위로 되돌립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-024-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-024-SHIELD | TC-FX-DEF-001 |  | 사용 | 방어도 +3 |  |
| TC-CARD-024-DECKTOP |  | 손패에 다른 카드 여러 장 | 사용 → 다음 드로우 | 무작위 1장이 덱 맨 위로 가고 다음 드로우에 그 카드가 나옴 |  |
| TC-CARD-024-EMPTYHAND |  | 이 카드가 손패의 유일한 카드 | 사용 | 방어도만 얻고 에러 없이 종료 |  |
| TC-CARD-024-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-025 SacrificeShieldCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 설명 "손의 카드 1장을 버린 후 방어도 6를 얻습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-025-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-025-DISCARD | TC-FX-PILE-003 | 손패에 다른 카드 1장 이상 | 사용 → 선택 패널에서 1장 선택 | 선택한 카드가 버린 카드 더미로, 방어도 +6 |  |
| TC-CARD-025-EMPTYHAND | TC-FX-PILE-004 | 이 카드가 손패의 유일한 카드 | 사용 | 버릴 카드 0장 패널이 즉시 확정되거나 예외 없이 넘어가는지 확인 | ⚠ 빈 손패 경계 |
| TC-CARD-025-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-026 DefensiveStanceCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 설명 "방어도 5를 부여합니다. (이동 전 사용 가능, 사용 후 이동 불가)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-026-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-026-STATUS | TC-FX-USE-011 | 이번 턴에 이동하지 않음 | 사용 | 방어도 +5, 자신에게 이동 불가 1턴, 이번 턴 이동 카드 사용 불가. 턴 종료 시 "이동 불가 해제", 다음 턴에는 이동 가능 | 지속 2 → 1 |
| TC-CARD-026-MOVED | TC-FX-USE-010 | 이번 턴에 이미 이동 | 사용 시도 | "이동 전에만 사용할 수 있습니다", 변화 없음 |  |
| TC-CARD-026-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-027 ShieldBonusUpCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 설명 "방어막 보너스를 1 올립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-027-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-027-STATUS | TC-FX-DEF-001, TC-FX-DEF-004 |  | 사용 → DefenseCard 사용 | 방어막 보너스 +1, DefenseCard 방어도 4 |  |
| TC-CARD-027-PERSIST | TC-FX-DEF-004 |  | 사용 후 전투 승리 → 다음 전투 | 다음 전투에는 유지되지 않음 |  |
| TC-CARD-027-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-028 HeavyShieldCard
코스트 2 · 스킬 · 소멸 · 전사 풀 · 자신 대상 · 설명 "자신에게 방어도 8를 부여합니다. (소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-028-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-028-SHIELD | TC-FX-DEF-001 | 방어막 보너스 0 | 사용 | 방어도 +8 |  |
| TC-CARD-028-EXILE | TC-FX-USE-009 |  | 사용 | 버린 카드 더미가 아니라 소멸 더미로 |  |
| TC-CARD-028-DESC | TC-FX-DESC-003 |  | 손패 확인 | 끝에 "(소멸)" 한 번 |  |

#### TC-CARD-029 DrawCard
코스트 1 · 스킬 · 전사 풀 · 보드 아무 칸에 드롭 · 설명 "카드를 2장 드로우합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-029-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-029-DRAW | TC-FX-PILE-001, TC-FX-DROP-005 | 덱에 카드 2장 이상 | 사용 | 2장 드로우(Draw 효과 2개가 1장씩) |  |
| TC-CARD-029-EMPTYDECK | TC-FX-PILE-002 | ① 덱 0장, 버린 카드 더미에 카드 ② 덱·버린 카드 더미 모두 0장 | 사용 | ① 버린 카드 더미를 섞어 덱으로 옮긴 뒤 2장 드로우 ② 드로우 없이 에러 없이 종료 | `CardCanvas.DrawCardsData` |
| TC-CARD-029-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-030 StunCard
코스트 1 · 스킬 · 전사 풀 · 기물이 있는 칸에 드롭 · 사거리 `AttackRangeInfo` · 설명 "대상에게 기절을 1턴 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-030-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-030-STATUS | TC-FX-STATUS-009, TC-FX-AI-003 | 사거리 안 적 | 사용 → 적 턴 | 기절 텍스트, 예고가 기절로 바뀌고 다음 적 턴에 행동하지 않음(`StunnedCard`로 대체) |  |
| TC-CARD-030-TARGET | TC-FX-DROP-003 | 빈 칸 | 드롭 | "기물이 있는 칸에 사용해야 합니다" | 드롭 대상이 `AnyPiece`라 아군에게도 쓸 수 있음 |
| TC-CARD-030-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-031 DispelCard
코스트 1 · 스킬 · 전사 풀 · 적에게 드롭 · 사거리 제한 없음 · 설명 "적을 선택해 걸려 있는 버프를 모두 제거합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-031-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-031-STATUS | TC-FX-STATUS-012, TC-FX-AIM-002 | 적이 버프(강화·가시·결속·상처 입히기 등)와 디버프를 함께 보유 | 멀리 있는 그 적에게 사용 | 버프만 모두 제거, 디버프는 남음 |  |
| TC-CARD-031-CHAIN | TC-FX-STATUS-012 | `ChainMoveAttackBuff`를 가진 적 | 사용 | 제거되지 않음(`StatusEffect`가 아님) | ⚠ 디스펠 불가 버프 |
| TC-CARD-031-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-032 HealCard
코스트 2 · 스킬 · 전사 풀 · 자신 대상 · 설명 "자신을 2 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-032-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-032-HEAL | TC-FX-DEF-003 | 부상 상태 / 체력 최대 | 사용 | 2 회복 / 최대 체력을 넘지 않음 |  |
| TC-CARD-032-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-033 CleanseCard
코스트 1 · 스킬 · 전사 풀 · 자신 대상 · 설명 "자신에게 걸린 디버프를 모두 제거합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-033-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-033-TARGET | TC-FX-DROP-004 | 자신에게 디버프 보유 | 보드의 아무 칸(적 칸 포함)에 드롭 | 추가 클릭 없이 드롭 즉시 카드를 낸 기물의 디버프가 제거되고 카드 사용 종료 | 회귀: `requiredMode = command`일 땐 시전자만 정해지고 범위 칸을 한 번 더 클릭해야 실행됐음 |
| TC-CARD-033-STATUS | TC-FX-STATUS-011 | 자신이 독 + 약화 + 강화 보유 | 사용 | 독·약화 제거, 강화는 남음 |  |
| TC-CARD-033-IMMOBILE | TC-FX-STATUS-011, TC-FX-CURSE-006 | 자신이 이동 불가·소멸 저주 | 사용 | 두 디버프 모두 제거, 이후 이동 카드 사용 가능 |  |
| TC-CARD-033-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-034 ShieldMoveCard
코스트 2 · 이동 · 획득 경로 없음(카드 목록 전사 페이지의 시작 덱 전용 행) · 자신 대상 → 이동 칸 지정 · 설명 "방어도 3를 얻은 후 이동합니다."

세이브의 덱에 직접 넣어 테스트한다. 새 게임의 전사 기본 덱 8장(AttackCard×3, DefenseCard×3, MoveCard×2)에는 없다.

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-034-COST | TC-FX-USE-001 |  | 빈 칸에 드롭 | 에너지 2 차감, NowUsing → UsedCardZone → 버린 카드 더미 |  |
| TC-CARD-034-MOVE | TC-FX-AIM-001, TC-FX-MOVE-001 | 방어막 보너스 0 | ① 잡기 → 손패로 되돌리기 ② 잡기 → 이동 범위 안 빈 칸에 드롭 ③ 범위 밖에 드롭 | 잡으면 화살표·이동 범위가 뜨고 방어도는 아직 없음. ① 변화 없음 ② 드롭 순간 방어도 +3 → 그 칸으로 이동 ③ "사거리 밖입니다" 후 취소 | 방어도는 self라 조준에서 빠지고 이동이 첫 조준 효과(`Card.AimEffectIndex`=1) |
| TC-CARD-034-IMMOBILE | TC-FX-MOVE-021 | 시전자 이동 불가 | 잡기 → 보드에 드롭 | 이동 효과가 빠져 화살표·이동 범위 없이 마우스를 따라감. 드롭하면 방어도 +3만, 에너지 2 차감 |  |
| TC-CARD-034-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-035 SquadTrainingCard
코스트 2 · 스킬 · 획득 경로 없음(예전 전사 풀) · 보드에 드롭 후 기물 선택 · 설명 "아군 기물 2체를 선택해 힘을 영구적으로 1 올립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-035-COST | TC-FX-USE-001, TC-FX-AIM-013 |  | 선택 확정 | 에너지 2 차감(놓는 순간이 아니라 선택 확정 시) |  |
| TC-CARD-035-TARGET | TC-FX-AIM-013, TC-FX-DEF-005 | 아군 2명 이상 | 보드에 놓기 → 시전자 포함 2명 선택 | 놓는 순간 선택 모드, 확정 시 선택한 2명 모두 `colDamageBonus` 영구 +1 |  |
| TC-CARD-035-ALONE | TC-FX-AIM-013 | 아군 1명뿐 | 사용 | 선택 가능 인원이 1명으로 클램프되어 그 1명만 적용 |  |
| TC-CARD-035-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

### 6.3 소환사

소환사 기본 덱의 DefenseCard·MoveCard는 6.1(TC-CARD-002·003)에 있다.

#### TC-CARD-036 SummonCard
코스트 2 · 스킬 · 소환사 풀·기본 덱(3장) · 빈 칸에 드롭 · 소환 범위 `SurroundingRangeInfo` · 설명 "autoally을(를) 소환합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-036-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-036-RANGE | TC-FX-AIM-001 |  | 주변 8칸 밖 칸에 드롭 | "사거리 밖입니다" |  |
| TC-CARD-036-SUMMON | TC-FX-SUMMON-001 | 주변 8칸에 빈 칸 | 빈 칸에 드롭 | 그 칸(막혀 있으면 가까운 빈 칸, 직교 우선)에 autoally 소환 — 팀 0, `isSummon`, 체력 5/5, 힘 3. 스폰 시 효과 없음 | autoally `onSpawnCards` 비어 있음(CRL-27) |
| TC-CARD-036-NOSPACE | TC-FX-SUMMON-002 | 주변 8칸이 모두 점유 | 사용 | 소환 없이 카드만 소모(에너지 2 차감, 버린 카드 더미로) |  |
| TC-CARD-036-BONUS | TC-FX-SUMMON-001 | 시전자의 다음 소환 보너스 있음(TC-CARD-040·042) | 소환 | 보너스가 소환수에 더해지고 0으로 리셋 |  |
| TC-CARD-036-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-037 GreaterSummonCard
코스트 4 · 스킬 · 소환사 풀 · 빈 칸에 드롭 · 소환 범위 `AttackRangeInfo` · 설명 "강력한 GreaterAutoAlly을(를) 소환합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-037-COST | TC-FX-USE-001 |  | 사용 | 에너지 4 차감 |  |
| TC-CARD-037-SUMMON | TC-FX-SUMMON-001 |  | 빈 칸에 사용 | GreaterAutoAlly 소환, 체력 15/15·힘 8 | `GreaterAutoAllyPieceInfo` |
| TC-CARD-037-RANGE | TC-FX-AIM-001 |  | 사거리 밖 칸에 드롭 | "사거리 밖입니다" |  |
| TC-CARD-037-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-038 TauntSummonCard
코스트 2 · 스킬 · 소멸 · 소환사 풀 · 빈 칸에 드롭 · 소환 범위 `AttackRangeInfo` · 설명 "도발 상태의 tauntAutoAlly을(를) 소환합니다. (소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-038-COST | TC-FX-USE-001, TC-FX-USE-009 |  | 사용 | 에너지 2 차감, 카드는 소멸 더미로 |  |
| TC-CARD-038-SUMMON | TC-FX-SUMMON-001 |  | 빈 칸에 사용 | tauntAutoAlly(비숍) 소환, 체력 5·힘 5 |  |
| TC-CARD-038-SPAWN | TC-FX-TAUNT-001 |  | 소환 직후 | `onSpawnCards` 순서대로 방어도 3(`DefenseCard`) → 영구 도발(`TauntCard`) 적용, 정보창에 `도발` | ⚠ 방어도 3이 카드 설명에 없음(CRL-27) |
| TC-CARD-038-TAUNT | TC-FX-TAUNT-002~013 | tauntAutoAlly(도발) 소환 상태 | 기본 TC의 배치로 적 턴 진행 | 적 AI의 대상 선택이 기본 TC대로 도발 기물 우선 |  |
| TC-CARD-038-DESC | TC-FX-DESC-003 |  | 손패 확인 | 끝에 "(소멸)" 한 번 |  |

#### TC-CARD-039 EmpowerAllyCard
코스트 2 · 스킬 · 소환사 풀 · 보드에 드롭 후 아군 1명 선택(시전자 제외) · 설명 "자신을 제외한 아군 기물 1체를 선택해 힘을 영구적으로 2 올립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-039-COST | TC-FX-USE-001, TC-FX-AIM-013 |  | 선택 확정 | 에너지 2 차감(선택 확정 시) | 예전엔 기물 선택 카드가 에너지를 차감하지 않았음 |
| TC-CARD-039-TARGET | TC-FX-AIM-013, TC-FX-USE-003, TC-FX-DEF-005 | 아군 2명 이상 | 보드에 놓기 → 다른 아군 클릭 | 놓는 순간 선택 모드(잡기만 한 동안은 아님, TC-FX-USE-003), 확정 시 그 아군 `colDamageBonus` 영구 +2(Buff 애니메이션에 맞춰 "힘 +2"), 시전자는 선택 불가 |  |
| TC-CARD-039-ALONE | TC-FX-AIM-012 | 아군이 시전자 1명뿐 | 사용 | 선택 대상 0명, 카드는 소모되고 에너지 2 차감, 효과 없음 | ⚠ 경계 케이스 |
| TC-CARD-039-PERSIST | TC-FX-DEF-005 |  | 사용 후 전투 승리 → 다음 전투 | 선택한 아군의 힘 증가 유지 |  |
| TC-CARD-039-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-040 SummonMasteryCard
코스트 1 · 스킬 · 소환사 풀 · 자신 대상 · 설명 "다음에 소환할 기물의 힘, 체력을 2씩 올립니다. (소환 시 소모됨)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-040-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-040-SUMMON | TC-FX-SUMMON-001 |  | 사용 → 같은 기물이 SummonCard 사용 | 소환수의 힘·최대 체력·현재 체력 각 +2, 보너스 0으로 리셋 |  |
| TC-CARD-040-STACK |  |  | 2번 사용 → 소환 | 각 +4 누적 적용, 상한 없음 |  |
| TC-CARD-040-EXPIRE |  |  | 사용 후 전투가 끝날 때까지 소환하지 않음 | 보너스가 만료 없이 기물에 남음(시각 표시 없음) | ⚠ `StatusEffect`가 아니라 만료 표시 없이 잔존 |
| TC-CARD-040-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-041 AreaHealCard
코스트 2 · 스킬 · 소환사 풀 · 아무 칸에 드롭 · 범위 `SurroundingRangeInfo`(시전자 주변 8칸) · 설명 "범위 내 아군을 5 회복합니다. (자신 제외)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-041-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-041-TARGET | TC-FX-AIM-007, TC-FX-DEF-003 | 주변 8칸에 부상 아군 여러 명, 시전자도 부상 | 사용 | 주변 아군 전원 5 회복, 시전자는 회복되지 않음, 적은 영향 없음 |  |
| TC-CARD-041-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-042 SummonGrowthCard
코스트 3 · 스킬 · 희귀 · 소멸 · 소환사 풀 · 자신 대상 · 설명 "매 턴 종료 시 다음에 소환할 기물의 힘을 1, 체력을 3 올립니다. (소환 시 소모됨) (소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-042-COST | TC-FX-USE-001, TC-FX-USE-009 |  | 사용 | 에너지 3 차감, 카드는 소멸 더미로 |  |
| TC-CARD-042-TURN | TC-FX-TURN-001, TC-FX-AIM-009 |  | 사용 → 정보창 | "턴 종료 시 다음 소환 힘 +1", "턴 종료 시 다음 소환 체력 +3" 두 버프가 턴 수 없이 표시. 사용 시점에는 보너스가 늘지 않음 |  |
| TC-CARD-042-SUMMON |  |  | 아군 턴 종료 2회 → SummonCard | 턴 종료마다 "다음 소환 힘 +1", "다음 소환 체력 +3". 소환된 autoally 힘 3+2=5, 체력 5+6=11, 보너스 0으로 리셋 |  |
| TC-CARD-042-REPEAT |  | TC-CARD-042-SUMMON 직후 | 턴 종료 1회 → 다시 소환 | 턴 효과가 남아 소모 후에도 다시 +1/+3씩 쌓임 |  |
| TC-CARD-042-STACK |  |  | 같은 기물이 2장 사용(소멸 카드라 1장은 전투당 1회) → 턴 종료 1회 | 턴 효과 4개, 턴 종료당 +2/+6 | 중첩 상한 없음 |
| TC-CARD-042-MASTERY |  | 턴 종료 1회(+1/+3) 후 SummonMasteryCard(+2/+2) | SummonCard | 두 보너스 합산(힘 +3, 체력 +5) | 시전자가 죽으면 턴 효과도 사라짐 |
| TC-CARD-042-DESC | TC-FX-DESC-003 |  | 손패 확인 | "(소환 시 소모됨) (소멸)"로 "(소멸)"이 한 번만 | CRL-82 |

#### TC-CARD-043 MagicAttackCard
코스트 3 · 스킬 · 소환사 풀 · 기물이 있는 칸에 드롭 · 사거리 `AttackRangeInfo` · 설명 "기물에 10의 마법 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-043-COST | TC-FX-USE-001 |  | 사용 | 에너지 3 차감 |  |
| TC-CARD-043-DMG | TC-FX-DMG-004 | 시전자 힘 버프 보유 / 대상 취약(+1) | 적에게 사용 | 버프 무관 정확히 10 / 11(취약은 적용) |  |
| TC-CARD-043-TARGET | TC-FX-DROP-003 |  | 아군에게 드롭 | 아군도 10 피해(드롭 대상 `AnyPiece`) |  |
| TC-CARD-043-DESC | TC-FX-DESC-002 | 시전자 힘 버프 | 손패 확인 | "기물에 10의 마법 피해를 줍니다."(힘 미반영), 정보창에 "마법 피해" 용어 창 | CRL-91 |

#### TC-CARD-044 ZoneAttackCard
코스트 2 · 공격 · 소환사 풀 · 아무 칸 지정(보드 전체) · 효과 범위 `BasicRangeInfoSO`(지정 칸 중심) · 설명 "범위 내 모든 기물에게 5의 마법 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-044-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-044-RANGE | TC-FX-AIM-002, TC-FX-AIM-005 |  | 멀리 있는 칸 지정 | 사거리 제한 없이 지정 칸 중심 마름모 2 범위가 미리보기로 뜨고 그 범위에 적용 | `noRangeLimit`, MouseCentered |
| TC-CARD-044-TARGET | TC-FX-AIM-006, TC-FX-DMG-004 | 범위에 아군·적 섞임, 시전자 힘 버프 | 사용 | 팀 무관 전원 5 마법 피해(아군 포함, 힘 미반영) |  |
| TC-CARD-044-DESC | TC-FX-DESC-002 |  | 손패 확인 | 설명 문구 일치, "마법 피해" 용어 창 |  |

#### TC-CARD-045 MagicMissileCard
코스트 2 · 스킬 · 소멸 · 소환사 풀(LoadMagicMissileCard로도 획득) · 기물이 있는 칸에 드롭 · 사거리 `AttackRangeInfo` · 설명 "기물에 6의 마법 피해를 줍니다. (소멸)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-045-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-045-DMG | TC-FX-DMG-004 | 시전자 힘 버프 | 사거리 안 적에게 사용 | 버프 무관 정확히 6 |  |
| TC-CARD-045-EXILE | TC-FX-USE-009 |  | 사용 | 소멸 더미로 |  |
| TC-CARD-045-DESC | TC-FX-DESC-003 |  | 손패 확인 | 끝에 "(소멸)" 한 번 |  |

#### TC-CARD-046 MagicVulnerableAttackCard
코스트 2 · 스킬 · 희귀 · 소환사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "2의 마법 피해를 3번 줍니다. 2턴간 취약(2)을 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-046-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-046-HIT | TC-FX-DMG-006 | 체력 충분한 적 | 드롭 | 같은 대상에 2 피해 3회(총 6, 이 카드의 취약은 미적용) |  |
| TC-CARD-046-STATUS | TC-FX-AIM-014, TC-FX-STATUS-001 |  | 사용 → AttackCard(4)로 공격 | 다시 클릭하지 않아도 그 대상에 `취약 (+2)` 2턴(`useLastTarget`). 이후 AttackCard 피해 6 |  |
| TC-CARD-046-OVERKILL | TC-FX-AIM-014 | 체력 4 이하 적 | 사용 | 2타에 처치, 3타 없음, 취약은 건너뜀(텍스트·에러 없음) | `skipIfTargetGone` |
| TC-CARD-046-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-047 CurseCard
코스트 1 · 스킬 · 소환사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "적에게 3턴간 독(3)을 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-047-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-047-STATUS | TC-FX-STATUS-001, TC-FX-DMG-010 | 사거리 안 체력 충분한 적 | 드롭 → 적 턴 3회 | `독 (3)` 텍스트·파티클. 적 턴 종료마다 3 피해(총 9), 세 번째 뒤 "독 (3) 해제" |  |
| TC-CARD-047-RANGE | TC-FX-AIM-001 | 사거리 밖에만 적 | 드롭 | 사용되지 않음 |  |
| TC-CARD-047-DMG | TC-FX-VULN-009 | 대상 취약(+1), 시전자 힘 버프 | 적 턴 종료 | 독 피해는 그대로 3(힘·취약 미적용) |  |
| TC-CARD-047-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치, "독" 용어 창 |  |

#### TC-CARD-048 LoadMagicMissileCard
코스트 0 · 스킬 · 소환사 풀 · 보드에 드롭 · 설명 "MagicMissileCard 카드를 2장 손에 가져옵니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-048-COST | TC-FX-USE-001 | 에너지 0 | 사용 | 사용됨, 에너지 변화 없음 |  |
| TC-CARD-048-ADDCARD | TC-FX-PILE-005 |  | 사용 | 손패에 MagicMissileCard 2장이 보드 연출 순서대로 한 장씩 등장 |  |
| TC-CARD-048-PERSIST | TC-FX-PILE-006 |  | 사용 → 전투 승리 → 덱 확인 | 저장된 덱에 MagicMissileCard가 남지 않음(전투 한정) |  |
| TC-CARD-048-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-049 ImmobilizeCard
코스트 1 · 스킬 · 소환사 풀 · 적에게 드롭 · 사거리 제한 없음 · 설명 "적을 선택해 2턴간 이동 불가 상태로 만듭니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-049-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-049-RANGE | TC-FX-AIM-002 | 멀리 떨어진 적 | 사용 | 거리 제한 없이 `이동 불가` 적용 |  |
| TC-CARD-049-STATUS | TC-FX-MOVE-022 | 다음 행동이 이동인 적 | 사용 → 적 턴 2회 | 이동 행동 차례에 적 위에 "이동 불가!" 텍스트와 흔들림만 나오고 이동·이동공격이 일어나지 않음. 같은 카드의 뒤 효과(예: EnemyMoveAndBlastCard의 광역)도 함께 취소. 그 적의 턴이 두 번 끝나면 해제 | `Board.ApplyCardEffectNow`의 Move 분기가 `pendingEffects`를 비움 |
| TC-CARD-049-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-050 WeakenDrawCard
코스트 2 · 스킬 · 희귀 · 소환사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "적에게 2턴간 약화(2)를 부여하고 카드를 2장 드로우합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-050-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-050-STATUS | TC-FX-STATUS-001, TC-FX-PILE-001 | 사거리 안 힘 5 적, 덱 2장 이상 | 드롭 | `약화 (-2)` 텍스트·파티클, 적 힘 3. 이어서 2장 드로우 |  |
| TC-CARD-050-RANGE | TC-FX-AIM-001 | 사거리 밖에만 적 | 드롭 | 사용되지 않음(약화·드로우·에너지 변화 없음) |  |
| TC-CARD-050-EFFECT | TC-FX-STATUS-008 | TC-CARD-050-STATUS 직후 | 적 턴 2회 | 카드 공격 5 → 3, 이동공격 5 → 3. 두 번째 적 턴 종료 시 "약화 (-2) 해제", 힘 5로 복구 | 힘이 2 미만이면 0까지만 |
| TC-CARD-050-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-051 MoveAndDrawCard
코스트 2 · 이동 · 소환사 풀 · 아무 칸에 드롭 · 설명 "이동한 후 카드를 1장 드로우합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-051-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-051-DRAW | TC-FX-AIM-008, TC-FX-PILE-001 |  | 빈 칸으로 이동 | 이동 연출이 끝난 뒤 1장 드로우 |  |
| TC-CARD-051-MOVEATTACK | TC-FX-AIM-008, TC-FX-MOVE-003 | 이동 범위 안 적 | 적 칸에 드롭 | 이동공격 후 1장 드로우 |  |
| TC-CARD-051-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-052 SafeMoveCard
코스트 1 · 이동 · 소환사 풀 · 아무 칸에 드롭 · 설명 "이동합니다. (이동공격 불가)"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-052-COST | TC-FX-USE-001 |  | 빈 칸으로 이동 | 에너지 1 차감 |  |
| TC-CARD-052-MOVE | TC-FX-MOVE-001, TC-FX-MOVE-004 | 이동 범위 안 빈 칸과 적 | ① 빈 칸에 드롭 ② 적 칸에 드롭 | ① 이동 ② 이동공격 없이 막힘 |  |
| TC-CARD-052-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-053 WardZoneCard
코스트 2 · 스킬 · 소환사 풀 · 자신 대상 · 턴 효과 범위 `SurroundingRangeInfo` · 설명 "3턴 동안 턴 종료 시 주변 적에게 3 마법 피해, 주변 아군을 2 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-053-COST | TC-FX-USE-001 |  | 사용 | 에너지 2 차감 |  |
| TC-CARD-053-TURN | TC-FX-TURN-001, TC-FX-TURN-004, TC-FX-AIM-007 | 주변에 적 1기·부상 아군 1기 | 사용 → 정보창 → 아군 턴 종료 | 정보창에 "턴 종료 시 광역 피해 3", "턴 종료 시 광역 회복 2"(각 3턴). 턴 종료 시 주변 적 3 피해, 주변 아군 2 회복, 시전자 본인은 회복 안 됨 |  |
| TC-CARD-053-DMG | TC-FX-DMG-004 | 시전자 힘 버프 | 아군 턴 종료 3회 | 피해는 매번 3(힘 미반영), 정확히 3회 후 두 턴 효과 모두 만료 |  |
| TC-CARD-053-DESC | TC-FX-DESC-002 |  | 손패 확인 | 설명 문구 일치, "마법 피해" 용어 창 |  |

#### TC-CARD-054 VulnerableCard
코스트 1 · 스킬 · 소환사 풀 · 적에게 드롭 · 사거리 제한 없음 · 설명 "2턴간 취약(1)을 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-054-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-054-RANGE | TC-FX-AIM-002 | 사거리 밖 먼 적 | 사용 | 거리 제한 없이 `취약 (+1)` 디버프 텍스트·파티클 |  |
| TC-CARD-054-STATUS | TC-FX-VULN-001, TC-FX-VULN-012 |  | 사용 → AttackCard(4)로 공격 | 피해 5, 2턴 뒤 해제 | 취약 세부는 TC-CARD-054-VULN |
| TC-CARD-054-VULN | TC-FX-VULN-002~014 | VulnerableCard로 취약(+1)을 건 적 | 기본 TC 절차(이동공격·광역·다중 타격·흡혈·연쇄·독 등) | 취약 보정·만료·디스펠 결과가 기본 TC대로 |  |
| TC-CARD-054-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-055 GraveHealCard
코스트 1 · 스킬 · 소환사 풀 · 아군에게 드롭 · 사거리 제한 없음 · 무덤 1 소모 · 설명 "무덤을 1 소모해 아군 하나를 3 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-055-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-055-TARGET | TC-FX-AIM-002 | 시전자 무덤 1, 멀리 있는 부상 아군 | 그 아군에게 드롭 | 대상 3 회복(시전자 체력 변화 없음, 회복 연출도 대상 칸), 시전자 무덤 1 → 0 | 무덤은 시전자 것을 씀. 회귀: 예전엔 시전자가 회복됐음 |
| TC-CARD-055-ALLYONLY | TC-FX-DROP-002 | 시전자 무덤 1 | 적에게 드롭 | "아군 기물에 사용해야 합니다", 무덤·에너지 변화 없음 |  |
| TC-CARD-055-GRAVE |  | 시전자 무덤 0 | 카드 상태 확인 → 사용 시도 | 사용 불가 표시, "무덤이 부족합니다" |  |
| TC-CARD-055-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치, "무덤" 용어 창 |  |

#### TC-CARD-056 GraveAttackCard
코스트 1 · 공격 · 소환사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 두 번째 타격에 무덤 1 소모 · 설명 "적에게 5 피해를 줍니다. 무덤을 1 소모해 같은 대상에게 5 피해를 더 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-056-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-056-GRAVE | TC-FX-AIM-014 | 시전자 무덤 1, 체력 충분한 적 | 적 클릭 | 5 + 5 = 10 피해, 두 번째 타격은 다시 클릭하지 않아도 같은 대상, 무덤 1 → 0 |  |
| TC-CARD-056-NOGRAVE |  | 시전자 무덤 0 | 사용 | 사용 가능, 5 피해만, 두 번째 타격 건너뜀 | 첫 효과엔 무덤 비용 없음 |
| TC-CARD-056-PARTIAL |  | 시전자 무덤 2 | 사용 | 무덤 1만 소모되어 1 남음 |  |
| TC-CARD-056-OVERKILL | TC-FX-AIM-014 | 시전자 무덤 1, 체력 5 이하 적 | 사용 | 1타 처치, 2타 없음, 무덤 1 유지 | `skipIfTargetGone` — CRL-24 |
| TC-CARD-056-DMG | TC-FX-VULN-001 | 시전자 무덤 1, 취약(+1) 적 | 사용 | 6 + 6 = 12 | 두 타격 모두 공격 경로 |
| TC-CARD-056-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-057 GraveHarvestCard
코스트 1 · 공격 · 소환사 풀 · 적에게 드롭 · 사거리 `AttackRangeInfo` · 설명 "적에게 6 피해를 줍니다. 처치 시 무덤을 1 얻습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-057-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-057-KILL | TC-FX-DMG-008 | ① 체력 6 이하 적 ② 체력 충분한 적 | 사용 | ① 처치, "무덤 +1", 무덤 +1 ② 무덤 변화 없음 |  |
| TC-CARD-057-IMMEDIATE |  | 시전자 무덤 0, 손패에 GraveHealCard | 처치 후 같은 턴에 GraveHealCard 사용 | 처치 직후 GraveHealCard가 사용 가능 상태로 바뀌고 정상 사용 | 무덤 증가는 연출을 기다리지 않음 |
| TC-CARD-057-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-058 GraveDefenseCard
코스트 1 · 스킬 · 소환사 풀 · 자신 대상 · 무덤 전부 소모 · 설명 "무덤을 모두 소모하고, 소모한 만큼 방어도를 4 얻습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-058-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-058-SHIELD | TC-FX-DEF-001 | 시전자 무덤 3, 방어막 보너스 0 | 사용 | 방어도 +12, 무덤 3 → 0 | `consumeAllGrave` + `dmgPerGrave=4`, 방어막 보너스는 따로 더해짐 |
| TC-CARD-058-NOGRAVE |  | 시전자 무덤 0 | 카드 상태 확인 → 사용 시도 | 사용 불가 표시, "무덤이 부족합니다" | 첫 효과 `graveCost=1` |
| TC-CARD-058-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-059 GrowingGraveCard
코스트 3 · 스킬 · 희귀 · 소환사 풀 · 자신 대상 · 설명 "매 턴 시작 시 무덤을 1 얻습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-059-COST | TC-FX-USE-001 |  | 사용 | 에너지 3 차감 |  |
| TC-CARD-059-TURN | TC-FX-TURN-002 |  | 사용 → 정보창 → 아군 턴 시작 3회 | 정보창에 턴 수 없이 "턴 시작 시 무덤 +1". 사용 시점엔 무덤 변화 없고, 다음 턴 시작부터 매 턴 "무덤 +1"(3회 후 3). 전투가 끝나면 사라짐 |  |
| TC-CARD-059-STACK |  |  | 2장 사용 | 턴당 +2 |  |
| TC-CARD-059-DESC | TC-FX-DESC-001 |  | 손패 확인 | 설명 문구 일치 |  |

#### TC-CARD-060 FetchAttackCard
코스트 0 · 스킬 · 획득 경로 없음(`4c31bad`에서 소환사 풀 제외, LoadMagicMissileCard로 대체) · 보드에 드롭 · 설명 "공격 카드를 1장 손에 가져옵니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-060-COST | TC-FX-USE-001 | 에너지 0 | 사용 | 사용됨 |  |
| TC-CARD-060-ADDCARD | TC-FX-PILE-005 |  | 사용 | AttackCard 1장이 카드를 낸 기물의 손패에 추가(화면 중앙 1초 → 손패) |  |
| TC-CARD-060-PERSIST | TC-FX-PILE-006 |  | 사용 → 전투 승리 → 덱 확인 | 저장된 덱에 추가된 AttackCard가 남지 않음 |  |

#### TC-CARD-061 PoisonTestCard
코스트 1 · 스킬 · 획득 경로 없음(독 검증용, 정식 독 카드는 CurseCard) · 적에게 드롭 · 사거리 제한 없음 · 설명 "적을 선택해 2턴간 매 턴 2의 독 피해를 입힙니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-061-COST | TC-FX-USE-001 |  | 사용 | 에너지 1 차감 |  |
| TC-CARD-061-STATUS | TC-FX-STATUS-001~003, TC-FX-DMG-010, TC-FX-AIM-002 | 멀리 있는 적 | 사용 → 적 턴 2회 | `독 (2)`, 적 턴 종료마다 2 피해, 2회 후 해제 | ⚠ 코드 주석상 임시 카드 — 유지 여부 결정 필요 |

### 6.4 적

적 카드는 플레이어가 쓰지 않으므로 COST·DESC 대신 **AI**(쓰는 적, 행동 순환 위치, 머리 위 예고, 보드 예고)를 확인한다. 머리 위 예고 아이콘은 `AutoPiece.BuildEffectDescription` 기준이다(이동 → 이동 아이콘, 피해 → "N + 피해 아이콘", 방어도 → "N + 방어막 아이콘", 회복 → "N + 회복 아이콘", 힘 증가 → "피해 아이콘 +N", 상태이상 → 이로우면 Positive·해로우면 Negative 아이콘, 카드 넣기 → Negative, 그 밖 → Unknown). 피해 예고 숫자에는 그 적이 이번 전투에서 얻은 힘이 더해진다. 범위 표기는 Confluence "적 목록" 기준.

#### TC-CARD-062 EnemyMoveCard
이동 · 쓰는 적: StrikerKnight, ThornKnight, WoundKnight, BindKnight, CurseKnight, Bond 3종, Boss-Pursuer·Berserker·Colossus · 범위는 그 적의 이동 범위 · 설명 "이동합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-062-AI | TC-FX-AI-001 | 다음 행동이 이동인 적 | 플레이어 턴에 예고 확인 | 머리 위 이동 아이콘, 보드에 그 적의 이동 범위 |  |
| TC-CARD-062-MOVE | TC-FX-TAUNT-004, TC-FX-MOVE-019 | ① 이동 범위 안에 아군 2명(도발 1명 포함) ② 이동 범위 안에 아군 없음 | 적 턴 | ① 도발 아군을 우선, 그다음 가장 가까운 아군을 이동공격(힘만큼) ② 가장 가까운 아군 쪽 빈 칸으로 다가감 | 도발 규칙은 TC-CARD-038-TAUNT |
| TC-CARD-062-IMMOBILE | TC-FX-MOVE-022 | 적이 이동 불가 | 적 턴 | "이동 불가!" 텍스트와 흔들림, 이동 없음 | TC-CARD-049-STATUS |

#### TC-CARD-063 EnemyAttackCard
공격 · 쓰는 적: StrikerKnight·BondStriker(범위 고리), GrowingRook(범위 굵은 십자) · 설명 "적에게 5 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-063-AI | TC-FX-AI-001 | 다음 행동이 이 카드인 적 | 예고 확인 | 머리 위 "5 + 피해 아이콘", 보드에 카드 범위(고리 또는 굵은 십자) |  |
| TC-CARD-063-TARGET | TC-FX-AI-002, TC-FX-TAUNT-002 | 범위 안에 아군 2명 | 적 턴 | 범위 안 가장 가까운 아군 1명(도발 우선)에게 5 피해 |  |
| TC-CARD-063-MISS | TC-FX-AI-002 | 범위 안에 아군 없음 | 적 턴 | 피해 없이 행동 종료, 다음 행동으로 순환 |  |
| TC-CARD-063-DMG |  | 그 적의 힘이 전투 중 +N(GrowingRook·결속 강화) | 예고·적 턴 | 예고·실제 피해가 5 + N |  |

#### TC-CARD-064 EnemyMoveAndColDamageUpCard
이동 + 강화 · 쓰는 적: GrowingRook(2번째 행동), Boss-Berserker(1번째 행동) · 설명 "이동한 후 힘을 3 올립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-064-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "이동 아이콘 + 피해 아이콘 +3", 보드에 이동 범위 |  |
| TC-CARD-064-POWER | TC-FX-DEF-004 |  | 적 턴 | 이동(또는 이동공격) 뒤 힘 +3, 이후 이동공격·EnemyAttackCard·BossFrenzyCard 피해와 예고 숫자가 3 오름. 순환마다 누적(이번 전투 한정) | 설명 수치 연동은 CRL-84 |
| TC-CARD-064-IMMOBILE | TC-FX-MOVE-022 | 적이 이동 불가 | 적 턴 | 이동이 막히면 힘 증가도 함께 취소 | 이동 효과가 `pendingEffects`를 비움 |

#### TC-CARD-065 EnemyAreaShieldCard
방어 · 쓰는 적: Enemy-Guardian(평상시, 방어도 2), Enemy-BondWarden(1번째 행동, 방어도 3) · 범위 5×5(중심 포함) · 설명 "모든 아군(자신 포함)에게 방어도 N를 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-065-AI | TC-FX-AI-001 | `L2-Guardian` | 플레이어 턴 예고 확인 | 머리 위 "2 + 방어막 아이콘", 보드에 5×5(중심 포함)가 적 색으로 | TC-TURN-009 |
| TC-CARD-065-SHIELD | TC-FX-DEF-001, TC-FX-DEF-007 | 수호자와 5×5 안 기사 2기, 범위 밖 적 1기 | 적 턴 여러 번 | 매 적 턴 수호자와 범위 안 기사가 방어도 +2, 범위 밖 기물·플레이어 기물은 받지 않음. 플레이어 턴 종료 시 적 방어도가 사라져 쌓이지 않고 매 플레이어 턴 2씩만 남음 | 방어도 규칙 TC-FX-DEF-007 |
| TC-CARD-065-DESC | TC-FX-DESC-001 |  | 수호자 칸 정보창 → 다음 행동 카드 | "모든 아군(자신 포함)에게 방어도 2를 부여합니다." |  |

#### TC-CARD-066 EnemyMoveAndBlastCard
이동 + 광역 · 쓰는 적: Enemy-Guardian(격노 후 매 턴) · 범위 이동 후 위치 기준 5×5(중심 제외) · 설명 "이동한 뒤 모든 적에게 6 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-066-AI | TC-FX-AI-001 | 격노한 수호자 | 예고 확인 | 머리 위 "이동 아이콘 + 6 + 피해 아이콘", 보드 예고는 첫 효과(이동) 범위인 주변 8칸만 보이고 5×5 피해 범위는 안 보임 | 격노 전환은 TC-TURN-012 |
| TC-CARD-066-DMG |  | 격노한 수호자, 아군이 이동 후 위치 5×5 안 | 적 턴 | 가장 가까운 아군 쪽으로 1칸 이동(이동 범위 안에 아군이 있으면 이동공격, 힘 3) → 이동한 자리 기준 5×5 안 아군 전원 6 피해 |  |

#### TC-CARD-067 EnemyThornCard
버프 · 쓰는 적: Enemy-ThornKnight(1번째 행동) · 범위 없음(`EmptyRangeInfo`) · 설명 "자신에게 가시(2)를 부여합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-067-AI | TC-FX-AI-001 | `L2-Thorn` | 예고 확인 | 머리 위 Positive 아이콘, 보드 예고 없음 |  |
| TC-CARD-067-STATUS | TC-FX-STATUS-010, TC-FX-MOVE-008 |  | 적 턴 2회 | 첫 적 턴에 두 기사가 각자 영구 `가시 (2)`, 다음 적 턴에 이동. 가시를 가진 기사를 이동공격(판정 포함)하면 공격자가 2 반격 피해(비공격 피해 — 반격 효과음, 피격 시 유물 없음) | 반격은 TC-FX-MOVE-016 |

#### TC-CARD-068 EnemyAddCardCard
방해 · 쓰는 적: Enemy-WoundKnight(1번째 행동) · 범위 5×5(중심 제외) · 넣는 카드 WoundCard(덱의 무작위 위치) · 설명 "모든 적의 덱에 WoundCard 1장을 넣습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-068-AI | TC-FX-AI-001 | `L2-Wound` | 예고 확인 | 머리 위 Negative 아이콘, 보드에 5×5 |  |
| TC-CARD-068-ADDCARD | TC-FX-PILE-006 | 아군 A(화면에 띄움)·B 모두 범위 안 | 적 턴 → 덱 보기 | 시전 타격 시점에 WoundCard가 화면 중앙 1초 뒤 A의 덱 더미로 날아감, A의 덱 장수 +1(손패 변화 없음). B 위에는 "카드 +1", B로 전환하면 덱 장수 +1 |  |
| TC-CARD-068-DECKPOS |  | 아군 A의 덱 5장 이상 | WoundCard가 든 뒤 여러 전투(또는 CardTestScene 재시작)로 반복해 다음 드로우 순서 관찰 | 덱 맨 아래에만 가지 않고 매번 다른 자리(맨 위 포함)에 들어가, 덱을 다 뽑기 전에도 뽑힘 | `CardCanvas.InsertIntoDeckAtRandom`(덱 N장이면 N+1곳 중 하나) |
| TC-CARD-068-RANGE | TC-FX-AIM-007 | 아군 1명이 범위 밖 | 적 턴 | 범위 밖 아군의 손패·덱·버림 더미 변화 없음 |  |

#### TC-CARD-069 WoundCard
방해 카드 · 적이 넣음(WoundKnight의 EnemyAddCardCard, Boss-Berserker의 상처 입히기) · 보상 풀·기본 덱에 없음 · 설명 "사용 불가."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-069-UNPLAYABLE |  | 손패에 있음, 에너지 충분 | 손패 확인 → 보드에 끌어 놓기 | 항상 어둡게(사용 불가) 표시, 코스트 숫자 없음(회색 원은 남음). 놓으면 "사용할 수 없는 카드입니다"로 손패 복귀, 에너지·덱 변화 없음 | `Card.IsUnplayable`, `CardCanvas.UseCard`에서 코스트 검사보다 먼저 거부 |
| TC-CARD-069-DISCARD | TC-FX-PILE-003 | 손패에 있음 | ① 아군 턴 종료 ② SacrificeShieldCard 등 손패 버리기 선택 | ① 버린 카드 더미로 ② 선택 패널에 나오고 버릴 수 있음 |  |
| TC-CARD-069-PERSIST | TC-FX-PILE-006 | TC-CARD-068-ADDCARD 직후 | WoundCard가 뽑힐 때까지 턴 진행 → 턴 종료 → 전투 승리 → 다음 노드 | 뽑히면 드로우 1장을 차지해 손패에 남고(사용 불가), 턴 종료 때 버린 카드 더미로. 전투 후 원본 덱(`save.json`의 `deckCardIDs`)에 남지 않음 |  |

#### TC-CARD-070 EnemyAreaImmobilizeCard
방해 · 쓰는 적: Enemy-BindKnight(2번째 행동) · 범위 주변 8칸 · 설명 "모든 적을 1턴간 이동 불가 상태로 만듭니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-070-AI | TC-FX-AI-001 | `L2-Bind`, 기사가 아군 옆 | 예고 확인 | 머리 위 Negative 아이콘, 보드에 주변 8칸 |  |
| TC-CARD-070-STATUS | TC-FX-MOVE-020 | 범위 안 아군 | 적 턴 → 플레이어 턴 | 범위 안 아군에 `이동 불가` 텍스트. 다음 플레이어 턴에 그 아군은 첫 효과가 이동인 카드를 집는 순간 "이동 불가 상태입니다", 플레이어 턴 종료 시 "이동 불가 해제" | 지속은 걸린 기물의 턴 종료 때 줄어듦 |
| TC-CARD-070-COUNTER |  | 이동 불가 상태 | 이동이 아닌 카드(AttackCard, DefenseCard, ColDamageAttackCard)로 기사 처치나 방어도 확보 | 이동이 아닌 카드는 정상 사용(이동공격 판정 카드 포함). 기사를 처치하면 다음 광역이 오지 않음 | 대응 수단 |

#### TC-CARD-071 EnemyAreaBlastCard
광역 · 쓰는 적: Enemy-BindKnight(3번째 행동, 12), Enemy-BondSweeper(2번째 행동, 4) · 범위 주변 8칸 · 설명 "모든 적에게 N 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-071-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "12(또는 4) + 피해 아이콘", 보드에 주변 8칸 |  |
| TC-CARD-071-DMG | TC-FX-AIM-007, TC-FX-DMG-002, TC-FX-DMG-003 | TC-CARD-070-STATUS 직후, 범위 안·밖 아군 | 적 턴 | 범위 안 아군 전원 12 피해(방어도 먼저 차감), 범위 밖은 피해 없음 |  |
| TC-CARD-071-POWER |  | BondSweeper가 결속으로 힘 +2 | 예고·적 턴 | 예고·피해가 6 | 결속은 TC-TURN-015 |

#### TC-CARD-072 EnemyAreaExileCurseCard
방해 · 쓰는 적: Enemy-CurseKnight(2번째 행동) · 범위 5×5(중심 제외) · 설명 "모든 적에게 1턴간 소멸 저주를 겁니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-072-AI | TC-FX-AI-001 | `L2-Curse` | 예고 확인 | 머리 위 Negative 아이콘, 보드에 5×5 |  |
| TC-CARD-072-STATUS | TC-FX-CURSE-001 | 범위 안·밖 아군 | 적 턴 → 다음 플레이어 턴 | 범위 안 아군에 "소멸 저주". 다음 턴 그 아군이 쓰는 카드가 모두 소멸, 범위 밖 아군은 영향 없음 | 소멸 저주 세부는 TC-CARD-072-CURSE |
| TC-CARD-072-CURSE | TC-FX-CURSE-002~007 | CurseKnight에게 소멸 저주를 받은 아군 | 기본 TC 절차 | 카드 면 표시·소멸 처리·해제가 기본 TC대로 |  |

#### TC-CARD-073 BossAttack1Card
광역 · 쓰는 적: Boss-Bastion(1번째 행동, 표시 이름 "보스 패턴 1") · 범위 퀸 3칸 · 설명 "모든 적에게 6 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-073-AI | TC-FX-AI-001 | `L3-Bastion` | 예고 확인 | 머리 위 "6 + 피해 아이콘", 보드에 8방향 3칸 |  |
| TC-CARD-073-DMG | TC-FX-AIM-007 | 범위 안·밖 아군 | 적 턴 | 범위 안 아군만 6 피해 |  |

#### TC-CARD-074 BossAttack2Card
광역 · 쓰는 적: Boss-Bastion(2번째 행동, "보스 패턴 2") · 범위 井 모양 · 설명 "모든 적에게 5 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-074-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "5 + 피해 아이콘", 보드에 井 모양 24칸 |  |
| TC-CARD-074-DMG | TC-FX-AIM-007 | 범위 안·밖 아군 | 적 턴 | 범위 안 아군만 5 피해 |  |

#### TC-CARD-075 BossAttack3Card
광역 + 방어 · 쓰는 적: Boss-Bastion(3번째 행동, "보스 패턴 3") · 범위 5×5(중심 제외) · 설명 "모든 적에게 5 피해를 주고, 방어도 5를 획득합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-075-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "5 + 피해 아이콘 5 + 방어막 아이콘", 보드에 5×5 |  |
| TC-CARD-075-DMG | TC-FX-AIM-007, TC-FX-DEF-007 | 범위 안 아군 | 적 턴 | 범위 안 아군 5 피해, 보스 방어도 +5 |  |

#### TC-CARD-076 AllyLockedStrikeCard
아군 위치 고정 광역 · 쓰는 적: Boss-Pursuer(1번째 행동 16·그 칸 1칸, 5번째 행동 10·3×3) · 설명 "모든 적에게 N 피해를 줍니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-076-AI | TC-FX-AI-001 | `L3-Pursuer` | 플레이어 턴 시작 시 예고 확인 | 머리 위 "16 + 피해 아이콘", 보드 예고(빨간 칸)가 턴 시작 시점의 각 아군 위치 기준. 정보창 효과 범위 창에 "각 아군 위치를 중심으로 펼쳐집니다." |  |
| TC-CARD-076-LOCK |  |  | 턴 중 아군을 예고 칸 밖으로 이동 → 턴 종료 | 예고가 아군을 따라오지 않고, 적 턴에 예고 칸에 남은 아군만 피해 |  |
| TC-CARD-076-DODGE |  |  | 모든 아군이 예고 칸 밖으로 → 턴 종료 | 피해 없이 헛스윙 연출 + 잠긴 칸 표시만, 전투 진행 정상 |  |
| TC-CARD-076-STUN |  | 예고가 떠 있음 | 플레이어 턴에 보스 기절 | 예고 칸이 즉시 사라짐. 기절이 풀린 다음 플레이어 턴에 그 시점 아군 위치로 다시 잠김 |  |
| TC-CARD-076-RANGE | TC-FX-AIM-001 |  | 1번째·5번째 행동 관측 | 1번째: 아군 칸 1칸에 16. 5번째: 아군 칸 중심 3×3에 10. 보드 밖 칸은 예고에서 제외 | 순환 순서는 TC-TURN-010 |

#### TC-CARD-077 AllyLockedImmobilizeStrikeCard
아군 위치 고정 광역 + 이동 불가 · 쓰는 적: Boss-Pursuer(3번째 행동) · 범위 아군 칸 기준 십자 1(5칸) · 설명 "모든 적에게 14 피해를 주고 2턴간 이동 불가를 겁니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-077-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "14 + 피해 아이콘 + Negative", 보드에 아군 칸 기준 십자 |  |
| TC-CARD-077-STATUS | TC-FX-MOVE-020, TC-FX-STATUS-001 | 십자 예고 칸에 아군을 남김 | 턴 종료 → 다음 두 플레이어 턴 | 맞은 아군 14 피해 + "이동 불가"(정보창 "2턴 남음"). 다음 두 플레이어 턴 모두 이동 카드를 집으면 "이동 불가 상태입니다", 두 번째 플레이어 턴 종료 시 해제 |  |
| TC-CARD-077-DODGE |  |  | 예고 칸 밖으로 피함 | 피해·이동 불가 모두 없음 | TC-CARD-076-DODGE와 같은 규칙 |

#### TC-CARD-078 BossRageCard
스폰 시 패시브 · 쓰는 적: Boss-Berserker(`onSpawnCards`) · 설명 "자기 턴이 끝날 때마다 힘이 2 오릅니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-078-TURN | TC-FX-TURN-001 | `L3-Berserker` | 전투 시작 → 적 턴 종료마다 관측 | 스폰 즉시 영구 턴 효과, 적 턴 종료마다 "힘 +2", 머리 위 피해 예고 숫자도 함께 증가 |  |
| TC-CARD-078-DISPEL | TC-FX-STATUS-012 |  | DispelCard로 보스 버프 제거 | 턴 종료 성장 중단, 이미 오른 힘은 유지 |  |

#### TC-CARD-079 BossWoundOnHitCard
스폰 시 패시브 · 쓰는 적: Boss-Berserker(`onSpawnCards`) · 설명 "자신에게 상처 입히기를 부여합니다. 피해를 준 적의 버린 카드 더미에 상처를 2장 넣습니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-079-STATUS | TC-FX-STATUS-010 | `L3-Berserker` | 전투 시작 → 보스 칸 정보창 | 힘 +2 턴 효과(TC-CARD-078)와 함께 상태 목록에 영구 `상처 입히기`와 용어 설명 | 동작은 TC-CARD-079-WOUND |
| TC-CARD-079-WOUND | TC-FX-WOUND-001~005 | `L3-Berserker` | 기본 TC 절차 | 상처 입히기 발동 조건·대응·전투 후 정리가 기본 TC대로. 1회 발동마다 WoundCard 2장 |  |
| TC-CARD-079-COUNT |  | `L3-Berserker`, 아군 A(화면에 띄움)·B, 방어도 0 | 보스 공격에 A와 B가 맞음 → 버린 카드 더미 확인 | A: WoundCard 2장이 한 장씩 차례로 화면 중앙에 보였다가 버린 카드 더미로, 버린 카드 더미 +2. B: 머리 위 "카드 +2" 텍스트 1번, 전환하면 버린 카드 더미 +2 | `WoundOnHitEffect.count = 2` |

#### TC-CARD-080 BossFrenzyCard
광역 · 쓰는 적: Boss-Berserker(3번째 행동, 5·주변 8칸), Boss-Colossus(4번째 행동, 8·5×5) · 설명 "모든 적에게 N 피해를 줍니다. 힘이 오른 만큼 강해집니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-080-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "N + 오른 힘 + 피해 아이콘", 보드에 주변 8칸(Berserker) 또는 5×5(Colossus) |  |
| TC-CARD-080-DMG |  |  | 여러 순환 관측 | 피해가 기본 수치 + 이번 전투에서 오른 힘 |  |

#### TC-CARD-081 BossEmpowerCard
강화 · 쓰는 적: Boss-Colossus(1번째 행동) · 설명 "힘을 4 올립니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-081-AI | TC-FX-AI-001 | `L3-Colossus` | 예고 확인 | 머리 위 "피해 아이콘 +4" |  |
| TC-CARD-081-POWER | TC-FX-DEF-004 |  | 적 턴 → 다음 순환 | 힘 +4 누적(전투 동안), 이후 지진·충돌 피해 증가 |  |

#### TC-CARD-082 BossExileStrikeCard
광역 + 소멸 저주 · 쓰는 적: Boss-Colossus(3번째 행동) · 범위 5×5(중심 제외) · 설명 "모든 적에게 8 피해를 주고 1턴간 소멸 저주를 겁니다. 힘이 오른 만큼 강해집니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-082-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "8 + 오른 힘 + 피해 아이콘 + Negative", 정보창 다음 행동 카드에 "소멸 저주" 용어 창 |  |
| TC-CARD-082-STATUS | TC-FX-CURSE-001 | 보스 주변 5×5에 아군 | 적 턴 → 다음 플레이어 턴 | 범위 안 아군 피해 + "소멸 저주". 다음 플레이어 턴에 그 아군이 쓰는 카드 모두 소멸, 턴 종료 시 해제. 4번째 행동(BossFrenzyCard)은 저주를 걸지 않음 | 피격으로 죽은 기물에는 안 걸림 |

#### TC-CARD-083 BossRegenerateCard
회복 · 쓰는 적: Boss-Colossus(5번째 행동) · 설명 "자신을 15 회복합니다."

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-083-AI | TC-FX-AI-001 |  | 예고 확인 | 머리 위 "15 + 회복 아이콘" |  |
| TC-CARD-083-HEAL | TC-FX-DEF-003 | 보스 체력 ① 100/120 ② 115/120 | 적 턴 | ① 115 ② 120(최대 체력 초과 없음) |  |

#### TC-CARD-084 StunnedCard
기절 대체 행동 · 모든 적·자동 아군 · 코스트 표시 없음 · 설명 "기절"

| ID | 기본 TC | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|---|
| TC-CARD-084-AI | TC-FX-AI-003 | 적이 기절 | 예고 확인 → 적 턴 | 머리 위 기절 아이콘, 적 턴에 아무 효과 없이 턴 소모 |  |
| TC-CARD-084-RESUME | TC-FX-AI-003 |  | 기절이 풀린 다음 적 턴 | 원래 예고했던 행동이 그대로 이어짐(순환 위치가 넘어가지 않음) | TC-TURN-003 |
| TC-CARD-084-FACE |  | 적이 기절 | 그 적 칸에 마우스 올려 다음 행동 카드 이미지 확인 | 설명 "기절", 코스트 숫자가 없음(`WoundCard`처럼). 이름·종류는 그대로. "기절" 용어 창은 따로 뜨지 않음(상태 창이 이미 설명) | `Card.HidesCost` — `StunnedCard`는 AI가 실행하는 카드라 `IsUnplayable`이 아니라 코스트 숨김만 켬 |

## 7. 상태이상/버프 시스템

이 장의 TC는 Confluence [카드 효과 기본 TC](https://agilewhaleshark.atlassian.net/wiki/spaces/CRL/pages/98477) 페이지로 옮겼다(2026-10-18). 카드 효과 기본 기능 TC(`TC-FX-<영역>-NNN`)가 되어 6장 카드 TC의 `기본 TC` 열에서 참조해 확인한다. 옛 ID → 새 ID 대응표는 그 페이지 맨 아래에 있다.

---

## 8. 턴 진행 & 적 AI

**관련 스크립트**: `TurnManager.cs`, `Board.TurnControl.cs`, `Board.TurnEffects.cs`, `Board.ScheduledEffects.cs`(효과 큐 공용 API), `AutoPiece.cs`, `GuardianPiece.cs`, `Board.NotifyTeammateDeath`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-TURN-001 | 아군 턴 종료 직후 ~ 다음 아군 턴의 턴 시작 처리(유물·턴 효과·드로우)가 끝나기 전 구간 | 이 구간에 카드/보드 클릭, 턴 종료 버튼 클릭 시도 | `PlayerInputLocked`로 입력·턴 종료 차단, `IsPlayerActionable`이 false. 턴 시작 처리가 끝난 뒤에 열림 | |
| TC-TURN-002 | 자동 아군(소환수) 존재 | 아군 턴 종료 후 관찰 | `PlayAutoAllyTurnCoroutine`이 적 턴 시작 전에 실행되어 자동 행동 수행 | |
| TC-TURN-003 | 적 AI가 스턴 상태로 자기 턴 도달 | 적 턴 진행 | `GetNextMove()`가 `Movenum`을 증가시키지 않고 `StunnedCard` 반환(스턴 해제 후 원래 예고된 행동 그대로 재개) | |
| TC-TURN-004 | 적의 `actionCards` 인덱스가 마지막 항목(`Count-1`)일 때 `ChangeMove()` 호출 | 다음 행동 결정 | `Movenum`이 0으로 정확히 래핑 | |
| TC-TURN-005 | 적 턴 진행 중 스냅샷 순회 도중 다른 적이 사망(예: 반격/연쇄피해) | 적 턴 코루틴 진행 | 죽은 적의 스냅샷 항목은 null 체크로 스킵되어 예외 없이 계속 진행 | |
| TC-TURN-006 | `Board.CombatEnded=true`가 된 시점 | 턴 코루틴이 진행 중이었다면 | 이후 턴 전환 코루틴이 즉시 종료(추가 배너/행동 발생 안 함) | |
| TC-TURN-007 | 첫 전투 시작(`isFirstTurn`) | 전투 진입 | "전투 시작" 배너 후 "플레이어 턴" 배너가 순차 표시(1회성) | |
| TC-TURN-008 | 아군 턴 종료 | `AllyTurnEndCoroutine` 처리 | 진행 중이던 카드 정리 → 아군 턴 종료 턴 효과 → 턴 종료 유물 → 아군 상태이상 틱 → 적 방어도 초기화 → `RestoreThisTurnCosts()`·전체 아군 핸드 버림 순서로 모두 수행 | 각 단계는 효과 큐가 빈 뒤 다음 단계로 넘어감 |
| TC-TURN-009 | 다음 행동이 동료 대상 범위 버프(`TargetLogic.AllAlliesInRange`, 예: `EnemyAreaShieldCard`)인 적 | 플레이어 턴에 보드 예고 확인 → 그 적 칸 클릭 → 적 턴 진행 | 다른 적 행동과 똑같이 범위(5×5)가 평상시 예고·칸 정보창 범위·행동 연출 중에 적 색으로 표시. 머리 위 예고는 방어막 아이콘 | `AutoPiece.GetMoveableButton`이 `effects[0].effectRange`를 그대로 반환 |
| TC-TURN-010 | `L3-Pursuer`(Boss-Pursuer, 행동 6개) | 6턴 순환 관측 | 순서가 AllyLockedStrikeCard(16, 아군 칸) → 이동 → AllyLockedImmobilizeStrikeCard(14 + 이동 불가 2턴, 십자) → 이동 → AllyLockedStrikeCard(10, 3×3) → 이동이고, 7번째 턴에 처음으로 돌아감 | 카드별 동작은 TC-CARD-076·077, TC-CARD-062 |
| TC-TURN-011 | `L3-Colossus`(Boss-Colossus, 체력 120, 이동 범위 상하좌우 1칸) | 5턴 순환 관측 | 체력 120/120으로 시작. 순서가 BossEmpowerCard(힘 +4) → 이동 → BossExileStrikeCard(8 + 소멸 저주) → BossFrenzyCard(8) → BossRegenerateCard(15)이고, 이동은 5턴에 1번·상하좌우 1칸만. 힘 +4가 순환마다 쌓여 뒤 공격 피해가 커짐 | 카드별 동작은 TC-CARD-080~083 |
| TC-TURN-012 | `L2-Guardian`(Enemy-Guardian `GuardianPiece` + StrikerKnight 2기) | 기사 1기 처치 → 턴 종료 → 남은 기사 처치 | 1기 처치 후엔 패턴 그대로(EnemyAreaShieldCard). 2기째 사망 즉시(플레이어 턴 중) 수호자에 "격노" 텍스트, 머리 위 예고가 "이동 + 6 피해"로, 보드 예고가 주변 8칸 이동 범위로 바뀜. 이후 매 턴 EnemyMoveAndBlastCard 반복(방어막 패턴으로 돌아가지 않음) | 격노 후 카드 동작은 TC-CARD-066 |
| TC-TURN-013 | TC-TURN-012와 같음 | 광역 카드로 기사 2기를 한 번에 처치 | 동시 사망이어도 첫 사망 시점에 남은 동료 0명으로 판정되어 즉시 격노 전환(전환·"격노" 텍스트 1회만) | `Board.NotifyTeammateDeath`의 hp>0 필터 |
| TC-TURN-014 | `L2-Bond`(BondWarden (1,3), BondStriker (2,2), BondSweeper (2,4), 아군 (5,2)·(5,4)) | 전투 진입 → 각 적 칸 정보창 | 셋 다 상태 목록에 `결속`(턴 수 없음)과 용어 설명 "같은 편 기물이 쓰러질 때마다 체력을 모두 회복하고 힘이 오릅니다." | 행동 카드는 TC-CARD-062·063·065·071 |
| TC-TURN-015 | TC-TURN-014, 셋 다 체력이 깎인 상태 | 1기 처치 | 처치 즉시 남은 2기 위에 "결속" → 회복 텍스트 → "힘 +2"가 차례로 뜨고 체력이 최대치(Striker·Sweeper 25, Warden 20)까지 회복. 최대 체력은 그대로. 공격형·광역형의 머리 위 피해 예고가 2 오름 | 강화는 사망 판정 즉시 적용(같은 적 턴 행동에 반영) |
| TC-TURN-016 | TC-TURN-015 직후 | 2기째 처치 | 마지막 1기가 다시 완전 회복, 힘 누적 +4. 광역으로 2기를 한 번에 처치해도 생존자는 2번 강화(힘 +4) | 사망 1회마다 1번 |
| TC-TURN-017 | TC-TURN-014 | 한 적에게 DispelCard 사용 → 다른 적 처치 | "결속 해제" 텍스트. 결속이 지워진 적은 동료가 쓰러져도 회복·힘 증가 없음(나머지는 정상 강화) | 대응 수단 |
| TC-TURN-018 | 아군 3명(소환수 제외) 생존 | 최대 에너지 확인 | `maxenergy = baseMaxEnergy + max(0, 비소환 아군수-1)`로 계산(아군 사망/합류 시 실시간 재계산) |  |
| TC-TURN-019 | 아군 턴 시작 | 카드 드로우 | 살아있는 모든 아군이 각 5장씩 드로우, 활성 기물은 애니메이션 포함, 나머지는 데이터만 즉시 반영 |  |
| TC-TURN-020 | 아군 턴 종료 | 핸드 상태 확인 | 사용하지 않은 카드 전부 버림더미로 이동(다음 턴으로 이월되지 않음) |  |
| TC-TURN-021 | `PersistentShieldCard`·`GrowingGraveCard`를 사용한 아군, 전투 시작 유물 외 턴 시작 유물(생기면) 보유 | 다음 아군 턴 시작 | 턴 시작 유물 → 턴 시작 턴 효과(방어도 +4, 무덤 +1) → 5장 드로우 순서로 처리되고, 모두 끝난 뒤에 카드를 집을 수 있음 | 효과 큐 단일화: 턴 시작 턴 효과가 드로우·입력 허용보다 먼저 끝남 |
| TC-TURN-022 | 아군이 `WardZoneCard`(턴 종료 광역 피해)·독(`CurseCard`) 보유, 근처에 방어도 가진 적 | 턴 종료 | 턴 종료 광역 피해 → 독 틱 → 적 방어도 초기화 순서. 광역 피해는 적 방어도에 먼저 막힘(방어도 초기화 전) |  |
| TC-TURN-023 | 적에게 "자기 턴 시작 시" 턴 효과가 걸림(`OwnTurnStart` — 현재 적에게 거는 콘텐츠 없음) | 적 턴 시작 | 적이 행동하기 전에 그 턴 효과가 발동. 그 효과로 죽은 적은 행동하지 않음 | 예전에는 적의 턴 시작 턴 효과가 처리되지 않았음(CRL 이슈 참고) |
| TC-TURN-024 | 마지막 남은 적이 독(체력 이하 피해) 상태 | 적 턴 종료(독 틱으로 사망) | 다음 타이밍·플레이어 턴으로 넘어가지 않고 승리 처리. 아군 턴 종료 독 틱으로 마지막 아군이 죽어도 적 턴이 시작되지 않고 패배 처리 | `Board.IsCombatDecided` |
| TC-TURN-025 | 소멸 카드(`MagicMissileCard`·`ChainMoveAttackCard` 등 `exileOnUse`)를 집어 조준 중(드롭 전) | 턴 종료 버튼 클릭 | 카드가 소멸되지 않고 손패로 돌아갔다가 버림 더미로 감. 에너지 차감 없음 | 예전에는 사용한 카드로 처리돼 소멸됐음 |
| TC-TURN-026 | 한 턴에 카드 3장 이상 사용(공격·스킬 섞어서) | 턴 종료 → 적 턴 → 다음 플레이어 턴 | 턴 종료 처리 중 에너지가 추가로 차감되지 않고, 턴 순환이 멈추지 않음 | 회귀: 사용한 카드 상태(`currentActiveCard`)가 카드 종료 때 비워지는지 |

---

## 9. 유물 시스템

**관련 스크립트**: `Relic.cs`, `RelicDatabase.cs`, `Relics/ShieldRelic.cs`, `Relics/VampiricFangRelic.cs`, `Board.Relics.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-RELIC-001 | `ShieldRelic`(CombatStart, 자가 보호막+3) 보유 | 새 전투 레벨(비이벤트) 진입 | 전투 시작 시 자동으로 보호막 +3 | |
| TC-RELIC-002 | `ShieldRelic` 보유, 이벤트 레벨 진입 | 이벤트 레벨 진입 | `TriggerRelicsOnCombatStart` 미발동(이벤트 레벨은 스킵) | |
| TC-RELIC-003 | `VampiricFangRelic`(OnKill, 자가 회복+2) 보유 | 적 처치 | 처치한 효과 직후(같은 카드의 다음 효과보다 먼저) 2 회복. 처치 시 효과(`onKillEffect`)가 있는 카드면 유물 → 처치 시 효과 순서 | 반응 효과(효과 큐 맨 앞) |
| TC-RELIC-004 | 존재하지 않는 클래스명으로 유물 생성 시도(데이터 오류) | `RelicDatabase.CreateRelic("Typo")` 호출 | `Debug.LogError` 출력, `null` 반환, 크래시 없음 | |
| TC-RELIC-005 | 유물 클래스명과 아이콘 프리팹명이 불일치하는 경우 | `SpawnIcon` 호출 | 아이콘 표시 실패(에러 로그), 효과 자체는 정상 동작 | ⚠ 클래스명-아이콘명 매칭 무결성 별도 점검 필요 |
| TC-RELIC-006 | `CardUsed` 타이밍 유물, `TargetsCardTarget=true` | 아군이 적 대상 카드 사용 / 빈 칸으로 이동하는 카드 사용 | 유물 효과가 카드의 첫 효과 직후(두 번째 효과보다 먼저), 캐스터가 아닌 카드의 실제 대상 기물을 시전자로 적용. 대상 칸이 비어 있으면 발동하지 않음 | ⚠ 현재 `CardUsed` 유물 없음. 효과 큐 단일화로 "첫 효과 직전"에서 "직후"로 바뀜 |
| TC-RELIC-007 | 여러 레벨 연속 진행(2번째 전투 이상) | 새 레벨 진입마다 관찰 | `LoadOwnedRelics()`가 매 레벨마다 재호출되어 `CombatStart` 유물이 매번 재적용됨(1회성 아님) | |
| TC-RELIC-008 | 유물 On-Hit 타이밍(`TriggerRelicsOnHit`) | 아군이 공격으로 적 타격 | 피해를 준 효과 직후 발동(다중 타격이면 타격 수만큼, 모두 그 효과가 끝난 뒤). DoT·자기피해·가시 반격·이벤트 피해로는 발동 안 함(TC-RELIC-010 참고) | ⚠ 현재 On-Hit 유물 없음 |
| TC-RELIC-009 | 일반 공격(`isAttack=true`)이 teamID==0 기물에 적중 | 공격 실행 | `Board.TriggerRelicsOnHit`가 호출되어 On-Hit 유물 발동 |  |
| TC-RELIC-010 | 비공격 피해(`isAttack=false`)로 teamID==0 기물이 피해를 입음 | 독·화상 틱, 턴 시작/종료 DoT(`TurnDamageStart/End`), 자해(`HeavyAttackCard`), 가시 적(`L2-Thorn`)을 이동공격해 받은 반격, 이벤트 대화 선택지 피해(`damageAmount`) 각각 | On-Hit 유물이 발동하지 않음. 가시 반격은 반격 효과음만, 이벤트 피해는 타격음 없이 피해 텍스트만 | 턴 DoT·자해는 효과 큐 단일화 때, 가시 반격·이벤트 피해는 그 후속 수정 때 비공격 피해로 바뀜. 이벤트 피해가 큐에 처리되지 않는 반응 효과를 남기던 문제도 함께 해소 |
| TC-RELIC-011 | 적(teamID==1) 기물이 공격으로 피해를 입음 | 공격 실행 | On-Hit 유물이 발동하지 않음(코드상 teamID==0 조건) |  |

---

## 10. 맵 생성 & 진행

**관련 스크립트**: `Map.cs`, `MapUI.cs`, `MapCanvas.cs`, `NodeButton.cs`, `LevelDatabase.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-MAP-001 | 신규 런, 저장된 맵 없음 | 맵 생성 | 0층/마지막 층은 정확히 1노드, 중간층은 1~2노드 | |
| TC-MAP-002 | 마지막 층 | 노드 타입 확인 | 항상 `Boss` 타입 | |
| TC-MAP-003 | 맵을 여러 번(예: 50회) 새로 생성 | 매 생성 결과의 연결 그래프 검사 | 다음 층에 들어오는 연결이 0개인 고아 노드가 한 번도 발생하지 않음(안전망이 항상 작동) | ⚠ 확률적 로직이므로 반복 회귀 테스트 권장 |
| TC-MAP-004 | 0층에서 다음 층 진행 | 0층 노드에서 연결 확인 | 0층 노드가 다음 층의 모든 노드와 연결 | |
| TC-MAP-005 | 마지막 직전 층 | 연결 확인 | 마지막 층의 유일한 노드로 전부 연결 | |
| TC-MAP-006 | 현재 위치한 노드 | 인접하지 않은(연결 안 된) 다음 층 노드 클릭 시도 | 선택 불가(회색 처리, `IsNodeReachable=false`) | |
| TC-MAP-007 | 노드 버튼을 매우 빠르게 더블클릭 | 더블클릭 | `SetNextLevel` 중복 호출로 인한 이상 동작(중복 씬 로드 등) 여부 확인 | ⚠ 디바운스 없음 |
| TC-MAP-008 | `LevelDatabase`에 동일한 이름의 레벨이 여러 풀에 존재(데이터 오류) | `GetLevel(name)` 호출 | 첫 번째 매치를 반환(의도된 동작인지 확인) | |
| TC-MAP-009 | 특정 층의 레벨 풀이 비어있음 | `GetRandomLevel(floor)` 호출 | `null` 반환, 상위 로직에서 안전하게 처리되는지 확인 | |
| TC-MAP-010 | 맵 화면을 프로그램적으로 토글(SetActive) vs 정상 흐름으로 재진입 | 두 경로 각각 확인 | `OnEnable` 재드로우가 의도치 않게 중복 발생하지 않음 | |

---

## 11. 상점

**관련 스크립트**: `ShopCanvas.cs`, `ShopCardSlot.cs`, `ShopRelicSlot.cs`, `GoldDisplay.cs`, `PieceTargetPickerUI.cs`, `CardCanvas.cs`(카드 선택 패널), `CardDatabase.cs`(희귀도), `PieceDatabase.cs`(직업 풀)

**규칙 요약**
- 진열 카드 8장: 보유 기물들의 직업 `RewardCardPool` 합집합에서 시작 카드 4종(`CardDatabase.StarterCardNames`)을 뺀 풀에서 뽑는다. 풀에 희귀 카드가 있으면 1장 이상 반드시 포함하고, 나머지는 희귀도 구분 없이 균등하게 뽑는다.
- 진열 유물 4칸: 보유하지 않은 유물만 뽑는다.
- 가격(`ShopCanvas` 인스펙터 기본값)
  - 카드: 일반 35G, 고급 50G, 희귀 70G. 진열할 때 ±10% 변동을 한 번 정한다. 일반 32~38G, 희귀 63~77G.
  - 유물: 90G.
  - 카드 제거: 50G + 25G × 런 전체 제거 횟수(`GameData.cardRemoveCount`). 상점 방문당 1회.
- 재고는 레벨 로드 후 처음 열 때 한 번만 만든다. 닫았다 열어도 같고, 산 물건은 "매진"으로 남는다.
- 골드는 구매 대상이 확정될 때만 빠진다. 기물 선택이나 카드 선택을 취소하면 그대로다.
- 희귀 카드: `FinalAttackCard`, `ChainMoveAttackCard`, `SummonGrowthCard`, `WeakenDrawCard`, `MagicVulnerableAttackCard`, `GrowingGraveCard`. 나머지는 모두 일반.

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-SHOP-001 | 새 런, 상점 진입 | 가격 확인 | 일반 카드 32~38G, 희귀 카드 63~77G(`[희귀]` 표기), 유물 90G, 제거 버튼 "카드 제거 50G" | 제거 버튼 라벨은 `removeButtonLabel` 연결 시 |
| TC-SHOP-002 | 전사 1기만 보유 | 진열 확인 | 8장 모두 전사 `RewardCardPool` 카드. 시작 카드 4종·적 전용 카드 없음, 중복 없음 | |
| TC-SHOP-003 | 전사 1기만 보유 | 상점 진입을 여러 런 반복 | 매번 `FinalAttackCard`/`ChainMoveAttackCard` 중 1장 이상 진열 | |
| TC-SHOP-004 | 전사와 소환사 보유 | 진열 확인 | 두 직업 풀 합집합에서 진열, 희귀 6종 중 1장 이상 포함 | |
| TC-SHOP-005 | 상점 진입 | 닫기 후 상점 오브젝트 다시 클릭 | 같은 카드·가격·매진 상태 그대로(다시 뽑지 않음) | |
| TC-SHOP-006 | 골드가 카드 가격보다 적음 | 카드 클릭 | "골드가 부족합니다" 안내, 골드·덱 변화 없음. 가격 라벨은 빨간색 | |
| TC-SHOP-007 | 전사 1기, 골드 충분 | 전사 카드 클릭 | 기물 선택 없이 골드 차감 → 전사 덱에 추가 → 슬롯 "매진". 획득 연출 카드가 상점 패널 위에 보였다가 버린 카드 더미 쪽으로 날아감. 같은 슬롯을 다시 눌러도 반응 없음 | 획득 연출은 `CardCanvas.cardFxLayer` 연결 필요 |
| TC-SHOP-008 | 전사 2기(같은 직업), 골드 충분 | 카드 클릭 → 기물 선택창에서 취소 | 골드·덱 변화 없음, 슬롯은 그대로 판매 중 | 취소 버튼(`PieceTargetPickerUI.cancelButton`) 연결 필요 |
| TC-SHOP-009 | 전사와 소환사, 골드 충분 | 소환사 카드 클릭 | 기물 선택 없이 소환사 덱에 추가(전사는 후보에서 빠짐) | |
| TC-SHOP-010 | 골드 충분, 보유하지 않은 유물 진열 | 유물 클릭 | 골드 차감, 유물 획득, 파티 유물 아이콘 즉시 갱신, 슬롯 "매진" | ⚠ 지금은 팔 유물이 없어 확인 불가(유물 2종 모두 시작 유물) |
| TC-SHOP-011 | 시작 유물만 보유 | 유물 칸 확인 | `ShieldRelic`, `VampiricFangRelic`은 진열되지 않음 | |
| TC-SHOP-012 | `cardRemoveCount` 0, 골드 50 이상 | 카드 제거 → 기물 선택 → 카드 1장 선택 → 확인 | 카드 영구 제거, 골드 50 차감, `cardRemoveCount` 1, 버튼 "제거 완료"(비활성) | ⚠ 카드 선택 창이 상점 패널에 가려지지 않아야 함(`CardSelectionPanel` 정렬 미설정 시 가려짐) |
| TC-SHOP-013 | TC-SHOP-012 직후(같은 방문) | 카드 제거 다시 시도 | 버튼 비활성(연결 시). 연결 안 했으면 "이번 상점에서는 더 이상 카드를 제거할 수 없습니다" 안내. 변화 없음 | |
| TC-SHOP-014 | 세이브의 `cardRemoveCount`를 2로 수정 | 상점 진입 | 제거 가격 100G(50 + 25×2) 표시 | 런 안에서 누적. 새 런은 0부터 |
| TC-SHOP-015 | 골드 충분 | 카드 제거 → 카드 선택 패널에서 취소 | 골드·덱·`cardRemoveCount` 변화 없음, 다시 제거 가능 | 취소 버튼(`CardCanvas.cancelSelectionBtn`) 연결 필요 |
| TC-SHOP-016 | 골드가 제거 가격보다 적음 | 카드 제거 클릭 | "골드가 부족합니다" 안내, 버튼 라벨 빨간색 | |
| TC-SHOP-017 | 모든 기물의 덱이 빔(비정상 데이터) | 카드 제거 클릭 | "제거할 카드가 없습니다" 안내, 골드 변화 없음 | |
| TC-SHOP-018 | 덱이 빈 기물과 덱이 있는 기물 보유 | 카드 제거 클릭 | 덱이 빈 기물은 선택지에 없음. 남는 기물이 1기면 선택 없이 바로 카드 선택 패널 | |
| TC-SHOP-019 | 기물 선택창 또는 카드 선택 패널이 열린 상태 | 뒤의 다른 카드·유물·닫기 버튼 클릭 | 반응 없음(진행 중인 구매/제거가 끝날 때까지 상점 조작 잠금) | |
| TC-SHOP-020 | `GoldDisplay` 배치 | 전투 보상 골드 수령, 상점 구매·제거 | 골드 표시가 즉시 갱신 | |
| TC-SHOP-021 | 상점 진입 직후 | 나가기(Exit) 버튼 확인 | 아무것도 사지 않아도 바로 나갈 수 있음 | 의도된 동작(골드가 모자랄 때 소프트락 방지) |
| TC-SHOP-022 | 상점에서 구매·제거 후 나가기 | 맵에서 다음 노드 선택 후 `save.json` 확인 | gold, 덱, `cardRemoveCount`, 유물이 반영되어 저장됨 | |
| TC-SHOP-023 | 상점에서 구매 후 노드 선택 전 강제 종료 | 재실행 후 상점 재진입 | 구매·골드가 저장 전 상태로 돌아가고 재고는 새로 뽑힘 | TC-SAVE-009와 같은 저장 시점 규칙 |
| TC-SHOP-024 | 대화 이벤트 카드 제거, 전투 중 카드 선택 효과(버리기 등) | 패널 확인 | 취소 버튼이 보이지 않고 기존처럼 반드시 선택해야 함 | 회귀 확인 |

---

## 12. 보상/결과 화면

**관련 스크립트**: `ResultCanvas.cs`, `RewardSlot.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-RESULT-001 | 전투 승리, 생존 아군 2명, `rewardGoldMin/Max` 설정됨 | 결과 화면 진입 | 골드 슬롯 1개(범위 내 무작위) + 생존 아군 수만큼 카드 보상 슬롯 | |
| TC-RESULT-002 | `rewardGoldMin=0, rewardGoldMax=0`인 레벨 | 결과 화면 진입 | 골드 보상 슬롯 자체가 생성되지 않음 | |
| TC-RESULT-003 | 생존 아군의 `PieceInfo.Job`이 설정됨(Warrior/Summoner) | 카드 선택 오픈 | 해당 직업의 `RewardCardPool`에서 3장 무작위 제시 | |
| TC-RESULT-004 | 생존 아군에 `Job`이 없거나 `RewardCardPool`이 비어있음 | 카드 선택 오픈 | `CardDatabase.GetAllCardNames()` 전체에서 폴백 선택(크래시 없음) | |
| TC-RESULT-005 | 보상 후보 풀에 `AttackCard`/`DefenseCard`/`MoveCard`/`SummonCard`가 포함될 수 있는 상황 | 카드 선택 오픈 | 4종 기본 카드는 `CardDatabase.StarterCardNames`로 걸러져 절대 제시되지 않음 | |
| TC-RESULT-006 | 생존 아군 3명 | 각 아군별 카드 보상 개별 수령 | 한 아군이 보상을 골라도 다른 아군의 보상 슬롯은 독립적으로 유지 | |
| TC-RESULT-007 | 마지막 층 클리어 | 결과 화면 이후 흐름 | `MapCanvas.ShowRunComplete()` 호출(일반 층 클리어와 다른 화면) | |
| TC-RESULT-008 | 일반 층 클리어, `RewardType=PieceUpgrade` | 결과 화면 대신 | `Board.GrantLevelReward()`가 선택형 영구 스탯 강화 플로우로 분기(카드/골드 보상 화면 생략) | |
| TC-RESULT-009 | 일반 전투 레벨 클리어 | 골드 슬롯 금액 확인 | `L1` 10~20G, `L2`·`summonerLevel` 15~25G 범위 안 | 상점(8번째 층) 도착 시 평균 약 230G(시작 골드 100 + 전투 보상 약 130) |
| TC-RESULT-010 | 전투 중 `TempColDamageUpCard`(힘 +2, 1턴)·`ColDamageUpCard`(이번 전투 힘 +2, 소멸)·`ShieldBonusUpCard`로 활성 기물을 강화한 채 승리 | 카드 보상 선택을 열어 `AttackCard`류·방어도 카드의 설명 수치 확인 | 기물의 버프가 반영되지 않은 카드 기본 수치(검은 글씨)로 표시. 예: `HeavyAttackCard` "적에게 6 피해", `HeavyShieldCard` "자신에게 방어도 8를 부여합니다. (소멸)" | 카드 수치는 활성 기물의 전투 더미(손패·덱·버림·소멸)에 든 카드거나 사용 중인 카드일 때만 그 기물의 힘·방어막 보너스를 반영(`Card.PreviewCaster`) |
| TC-RESULT-011 | 보상 후보에 `ColDamageAttackCard`(힘만큼 피해)가 나옴 | 설명 확인 | "이동 범위 내 적에게 힘만큼 피해를 줍니다. (이동공격 판정)" — 숫자 대신 "힘만큼" | 상점·덱 보기에서도 같음 |
| TC-RESULT-012 | 전투 중 손패에 `AttackCard`, 덱·버림 더미에도 `AttackCard`, 활성 기물에 힘 버프(+2) | 손패 카드 설명 확인 → `CardsPanel` 덱/버림 보기 확인 → 다른 아군으로 활성 기물 전환 → 다시 덱/버림 보기와 저장 덱 보기 확인 | 손패와 덱/버림 보기 모두 6(파랑)으로 표시. 버프 없는 기물로 바꾸면 그 기물의 손패·덱/버림 보기는 기본 수치. 저장 덱 보기는 언제나 기본 수치 | 전투 더미 보기의 복제 카드는 `Card.ShowFaceOf`로 원본(활성 기물 더미 소속) 카드의 얼굴을 보여줌 |

---

## 13. 다이얼로그/이벤트 오브젝트

**관련 스크립트**: `DialogueSO.cs`, `DialogueUI.cs`, `RestObject.cs`, `ShopObject.cs`, `NPC.cs`, `EventExitButton.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-DLG-001 | 다이얼로그 표시 중(`IsShowing=true`) | 다른 다이얼로그를 트리거 | 두 번째 요청은 무시됨(큐잉되지 않고 드롭) | |
| TC-DLG-002 | 선택지에 `damageAmount`/`healAmount`/`addPieceInfo`/`cardPool`/`removeCardCount`/`permanentStatSelectCount`가 모두 설정된 복합 선택지 | 해당 선택지 클릭 | 정확히 문서 순서(피해·회복 → 영입 → 카드보상(선택UI 대기) → 카드제거(선택UI 대기) → 영구스탯(아군만, 선택UI 대기) → 전투진입/다음줄) 로 순차 처리 | |
| TC-DLG-003 | `healAmount=-1`(전체 회복) 선택지 | 클릭 | 전체 아군 최대체력까지 회복 | |
| TC-DLG-004 | `permanentStatSelectCount`가 설정된 선택지 | 대상 선택 시 필터 확인 | 적 기물은 선택 대상에서 제외(아군만, `PieceSelectFilters.Team(0)`) | |
| TC-DLG-005 | NPC의 `dialogues` 리스트 끝까지 이미 클릭함 | 한 번 더 클릭 | 인덱스가 클램프되어 마지막 대사가 반복 표시(에러 없음) | |
| TC-DLG-006 | 아직 트리거되지 않은 NPC | 화면에서 확인 | "!" 액션 텍스트 버블 표시, 최초 대화 후 사라짐 | |
| TC-DLG-007 | 아직 사용하지 않은 `RestObject` | 휴식 버튼 클릭(`RestHeal`) | 전체 아군 완전 회복, `used=true`로 재사용 방지, 나가기 버튼 노출 | |
| TC-DLG-008 | 이미 `used=true`인 `RestObject` | 화면 확인 | 휴식/업그레이드 버튼이 더 이상 노출되지 않음 | |
| TC-DLG-009 | 아군 전원이 이미 만피 상태 | 휴식 버튼 클릭 | 회복량 0이어도 나가기 버튼은 정상 노출(회복 여부와 무관) | ⚠ "아무 효과 없는 휴식"도 정상 소모 처리됨 |
| TC-DLG-010 | 이벤트 레벨이 아닌 상태(전투 레벨) | `EventExitButton.OnClickLeave()` 강제 호출 상황 | `GameManager.FinishEventLevel`이 경고 로그와 함께 실행을 거부 | |
| TC-DLG-011 | `triggerCombat`이 설정된 선택지 | 클릭 | 다이얼로그 닫힘 + `Board.EnterCombat`으로 지정 레벨 즉시 진입(`nextLineIndex` 무시) | |
| TC-DLG-012 | 아군 체력이 충분한 상태에서 `damageAmount`가 설정된 선택지 | 클릭 | 모든 아군이 그 수치만큼 피해(방어도 먼저 소모), 피해 텍스트·피격 반응은 나오지만 공격 타격음은 없음(비공격 피해). 이후 전투 진입·턴 진행이 정상(효과 큐에 남은 항목 없음) | `Board.DamageAllAllies` → 시전자 없는 `AreaAttackPiece`. TC-RELIC-010 참고 |

---

## 14. UI/입력/애니메이션 타이밍

**관련 스크립트**: `Board.InputHandler.cs`, `Board.Animation.cs`, `Board.RangeUI.cs`(범위 표시 우선순위 `RefreshRangeDisplay`), `Card.cs`(손패 호버·드래그), `CardCanvas.cs`(HandZone), `EndTurnButton.cs`, `AnnouncementUI.cs`, `CardDragArrow.cs`, `ISelectable.cs`, `CameraFX.cs`, `GameSpeedManager.cs`, `PieceGaugeListUI.cs`, `ButtonInfo.cs`, `Assets/CardsPanel.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-UI-001 | 공격 애니메이션 클립에 Animation Event가 설정되지 않음 | 해당 애니메이션 재생 | 1.2초 타임아웃 폴백으로 다음 리액션이 정상 진행(무한 대기 없음) | |
| TC-UI-002 | 공격 트리거에 대응하는 Animator 상태가 없는 기물(애니메이터 미구성) | 공격 실행 | DOTween 기반 "펀치" 폴백 연출 재생 후 콜백으로 `OnAnimationEvent()` 수동 발화 | |
| TC-UI-003 | `CameraFX.HitStop` 진행 중 배속 변경 버튼 클릭 | 배속 변경 | 즉시 반영되지 않고, 히트스탑 종료 시점에 새 배속이 적용됨 | |
| TC-UI-004 | 히트스탑 A(0.05s) 진행 중 더 짧은 히트스탑 B 요청 | B 요청 | 무시됨(끝나는 시점이 더 이른 요청은 연장하지 않음) | |
| TC-UI-005 | 종료 턴 버튼 5개 조건(플레이어 턴/큐 미진행/전투 미종료/카드 미처리/대기 애니메이션 없음) 중 하나라도 거짓 | 매 조건별 개별 검증 | 해당 조건이 거짓인 동안 버튼이 비활성 상태 유지 | |
| TC-UI-006 | 마우스 없는 환경(터치 전용 디바이스) | `CardDragArrow` 활성 상태에서 프레임 진행 | `Mouse.current`가 null이어도 예외 발생하지 않음 | ⚠ 현재 null 가드 미비, 크래시 가능성 점검 |
| TC-UI-007 | 카드/버튼/유물 아이콘에 마우스를 빠르게 반복 호버(in/out) | 반복 호버 | `ScaleAnimator`가 이전 트윈을 kill하고 새로 시작해 스케일이 누적/꼬이지 않음 | |
| TC-UI-008 | 같은 순위의 범위 두 개가 같은 칸에 겹침(예: 카드 사거리 + 범위 효과 미리보기) | 하나를 해제(마우스를 다른 칸으로) | 남은 범위는 그대로 보임. 해제 순서와 무관 | 칸 표시는 `Board.RefreshRangeDisplay`가 각 기능의 목록에서 매번 다시 그림(우선순위: 1 조준 > 2 행동 > 3 기물 선택 > 4 평상시) |
| TC-UI-009 | `AnnouncementUI`가 메시지 표시 중 | 새 메시지 발생 | 기존 표시를 즉시 중단하고 새 메시지로 갱신(경고음은 `isWarning=true`인 경우만) | |
| TC-UI-010 | `PieceGaugeListUI` 표시 중 기물 스폰/사망 발생 | 매 프레임 확인 | 게이지 목록이 프레임마다 보드 전체를 스캔해 항상 최신 상태 반영 | ⚠ 매 프레임 전수 스캔이라 보드가 커질 경우 성능 확인 필요 |
| TC-UI-011 | 카드 덱이 없는 오브젝트(RestObject/ShopObject/NPC)를 게이지 목록에서 확인 | 확인 | 핸드/덱 카운트 텍스트가 숨김 처리(예외 없음) | |
| TC-UI-012 | `CardsPanel`에서 동일한 뷰 모드(예: RuntimeDeck) 버튼을 연속 두 번 클릭 | 두 번째 클릭 | 패널이 갱신되지 않고 닫힘(토글-닫기 동작) | ⚠ 의도된 UX인지 확인 |
| TC-UI-013 | `CardsPanel`이 RuntimeDeck/Discard/Exile 보기 상태에서 전투 중 카드 드로우/버림/소멸 발생 | 실시간 관찰 | `CardCanvas.OnPileChanged` 이벤트로 그 프레임 끝에 갱신(카드 사용 직후 소멸도 포함) | 갱신은 `CardsPanel.LateUpdate`에서 프레임당 한 번 |
| TC-UI-014 | `CardsPanel`이 SavedDeck 보기 상태에서 전투 중 카드 변화 발생 | 관찰 | 자동 갱신되지 않음(명시적 재호출 전까지 정적 스냅샷 유지) | |
| TC-UI-015 | `CardsPanel`에서 카드 확대 미리보기 애니메이션 진행 중 같은 카드 재클릭 | 재클릭 | 애니메이션 진행 중이면 무시(중복 미리보기 방지) | |
| TC-UI-016 | 로스터가 1명뿐인 상태에서 `CardsPanel`의 SavedDeck 보기 | 확인 | 이전/다음 기물 전환 버튼(switcher)이 숨김 처리 | |
| TC-UI-017 | 정보창(`ButtonInfo`)에서 `RestObject`/`ShopObject` 클릭 | 확인 | 각각 "휴식 지점"/"상점" 전용 placeholder 텍스트 표시, 일반 기물 스탯 UI로 빠지지 않음 | |
| TC-UI-018 | 일반 기물 정보창에서 버프/디버프 상태 확인 | 확인 | 버프는 초록, 디버프는 빨강, 영구 효과(`duration<0`)는 지속시간 텍스트 미표시 | |
| TC-UI-019 | 아군·적이 섞여 배치된 레벨로 전투 진입 | 게이지 목록 확인 | 아군(`teamID 0`) 항목이 모두 위, 적(그 외 teamID) 항목이 모두 아래. 같은 팀 안에서는 보드 스캔 순서(x→y) | `PieceGaugeListCanvas`의 `VerticalLayoutGroup.reverseArrangement`는 꺼져 있어야 함 |
| TC-UI-020 | 전투 중 게이지 목록 표시 상태 | ① 아군 소환 ② 적 소환 ③ 중간 기물 사망 | ① 새 아군이 아군 블록 맨 아래(첫 적 바로 위)에 추가 ② 새 적이 목록 맨 아래에 추가 ③ 해당 항목만 빠지고 나머지 순서 유지 | |
| TC-UI-021 | `PieceAnimator`를 쓰는 기물(Warrior/SummonerAlly/sin/sin2) | 같은 칸에서 공격·방어막·버프 카드를 여러 번 연속 사용 | 애니메이션이 끝날 때마다 기물이 원래 칸 중앙에 그대로 서 있음(조금씩 밀려나지 않음) | Animator `Apply Root Motion`이 꺼져 있어야 함. Idle/Shield/Buff 클립은 XZ 루트 모션을 포즈에 굽지 않아서, 켜져 있으면 위치가 누적해서 어긋남 |
| TC-UI-022 | 플레이어 턴, 적 예고 범위가 여러 개 겹쳐 표시된 상태, 아무 기물도 선택하지 않음 | ① 적 A에 마우스 올림 ② 아군 B에 마우스 올림 ③ 마우스를 뗌 | ① 정보창에 A가 뜨는 동안 A의 예고 범위와 A 칸 초록만 보임(다른 적 범위 숨김) ② B의 이동 범위와 B 칸 초록만 보이고 적 범위는 모두 숨김 ③ 정보창이 닫히며 전체 적 예고 범위 복원(초록 없음) | 3순위 "기물 선택" — 켜지는 시점 = `ButtonInfo`가 그 기물 정보를 띄우는 시점. 기물 게이지 목록 항목 hover, 카드 없는 클릭 선택도 같은 경로 |
| TC-UI-023 | 아군 위치 고정 공격(`lockOnAllyPositions`)을 예고한 적 | 그 적에 마우스 올림 | 잠긴 절대 좌표 예고 칸이 그대로 보임(시전자 기준 오프셋으로 바뀌지 않음) | `GetActionRangeCells`를 상시 표시와 공유 |
| TC-UI-024 | 플레이어 턴 | ① 카드 없이 아군 클릭 ② 그 상태에서 카드를 집음 ③ 카드 사용(연출 종료까지) 또는 선택 해제 | ① 그 아군의 이동 범위와 그 칸 초록만 보이고 적 예고 범위 숨김 ② 카드 사거리·사용 가능 칸·카드 주인 초록만 보임(1순위) ③ 연출이 끝나거나 선택을 해제하면 적 예고 범위 복원 | 카드 없는 클릭 선택은 판정용 사거리(`selectedButtonMovable`)를 채우지 않음 |
| TC-UI-025 | 아군에 hover해 3순위가 켜진 상태 | ① 그 사이 적이 사망하거나 기절(`ShowAllEnemyRanges` 재호출) ② 마우스를 뗌 / 별도로 hover한 채 턴 종료 | ① 적 예고 범위가 보이지 않음 ② 갱신된 예고 범위로 보임. 턴 종료 후에는 적 예고 범위 없음 | 휴식·상점 레벨의 모닥불/상점 hover는 범위·초록 없음, 예외 없음 |
| TC-UI-026 | 소환사가 `ZoneAttackCard`(MouseCentered) 보유, 시전자에서 떨어진 칸에 범위 안 적 | 떨어진 칸을 중심으로 사용 | 공격 애니메이션 동안 표시되는 범위가 **클릭한 칸 중심**으로 펼쳐지고, 실제로 피해를 받은 칸과 일치(시전자 중심으로 그려지지 않음) | 범위 효과는 `ExecuteAreaEffect`의 실제 중심(`actualCenter`)을 `rangeOrigin`으로 넘김 |
| TC-UI-027 | 전사가 `DirectionalAttackCard` 2장 보유, 앞 카드 연출이 진행 중일 때 다음 카드를 사용할 수 있는 상태(카드 예약) | 첫 카드를 위쪽으로 사용 → 그 공격 애니메이션이 재생되기 전에 두 번째 카드를 아래쪽으로 바로 사용 | 첫 공격 애니메이션의 범위는 위쪽(실제로 맞은 방향), 두 번째는 아래쪽으로 표시됨. 겨눈 방향을 따라 앞 범위가 돌아가지 않음 | 범위 칸·방향은 효과 처리(motionQueue에 넣는) 시점에 계산, 켜고 끄기만 재생 시점. 카드를 들고 겨누는 동안에는 1순위(조준)가 우선이라 연출 범위는 숨음(TC-UI-030) |
| TC-UI-028 | 카드 예약이 가능한 상태 | 공격 카드 사용 직후, 그 연출이 재생되기 전에 같은 기물로 이동 카드 사용 | 공격 애니메이션 범위가 공격할 때 서 있던 칸 기준으로 표시됨(이동 후 칸 기준으로 그려지지 않음) | |
| TC-UI-029 | 플레이어 턴, 적 예고 범위 표시 중 | 적을 처치하는 공격 카드 사용 | 연출 큐가 도는 동안 적 예고 범위가 꺼짐. 공격 애니메이션 동안 시전자 칸 초록 + 공격 범위만 보임. 사망 연출 등 행동 연출 사이에는 아무 범위도 없음. 큐가 끝나면 적 예고 범위 복원 | 2순위 "행동" — 큐가 도는 동안은 내용이 없어도 켜진 것으로 봄 |
| TC-UI-030 | 카드 예약 가능, 앞 카드 연출 재생 중 | 다음 카드를 집음 → 내려놓음(취소) | 집는 즉시 조준 범위(사거리·사용 가능 칸·카드 주인 초록)만 보이고 연출 범위는 숨음. 취소하면 남은 연출의 범위로 돌아감 | 1순위 > 2순위 |
| TC-UI-031 | 적 턴 | 적 행동 관찰 | 행동하는 적의 AI 선택 범위(빨강)와 행동 범위, 그 적 칸 초록만 보임. 적 예고 범위·정보창 기물 범위는 보이지 않음 | AI 선택 범위와 행동 범위는 같은 2순위라 함께 보임. 플레이어가 행동할 수 없을 때(`IsPlayerActionable == false`)의 사거리는 2순위로 분류 |
| TC-UI-032 | 자기 대상 버프 카드(예: `ColDamageUpCard`) | 사용 | 연출 동안 시전자 칸 초록만 보이고 범위 칸은 없음 | |
| TC-UI-033 | `DirectionalAttackCard`를 듦 | 보드 위에서 마우스 이동 | 카드 사거리와 방향 미리보기가 함께 보이고(같은 1순위), 카드 주인 칸 초록 | 미리보기는 시전자 팀과 무관하게 파랑 |
| TC-UI-034 | 플레이어 턴, 손패 5장 이상 | 가운데 카드와 양 끝 카드에 각각 마우스 올림 → 뗌 | 올리면 1.3배(250×300 → 325×390)로 커지고, 최소 y +20 그리고 아랫변이 화면 아래 끝에 잘리지 않을 만큼 올라오며(양 끝 카드 포함), 기울기가 0으로 똑바로 서고, 왼쪽 이웃 카드에 가려지지 않고 맨 앞에 그려짐. 떼면 원래 크기·높이·부채꼴 기울기·겹침 순서로 돌아감(0.12초) | 맨 앞 표시는 카드에 붙인 하위 Canvas의 `overrideSorting`(sortingOrder = `CardCanvas.hoveredCardSortingOrder`, 기본 3)으로 처리 — 형제 순서·`handNumber`는 바뀌지 않음 |
| TC-UI-035 | 손패 여러 장, 한 카드를 호버해 맨 앞에 꺼낸 상태 | ① 확대된 카드가 이웃 카드를 덮은 부분으로 마우스 이동 ② 그 카드를 집어 드래그 ③ 손패로 되돌려 놓음 | ① 호버가 앞에 그려진 카드에 그대로 유지(뒤 카드로 넘어가지 않음) ② 드래그 중에도 카드가 MainCanvas UI(턴 종료 버튼 등)보다 앞에 그려지고, 들고 있는 카드에 가려진 손패 카드·기물 게이지 목록은 호버·클릭에 반응하지 않음(소리·확대 없음) ③ 원래 부채꼴 순서로 정렬되고 앞으로 꺼낸 상태가 풀림 | 레이캐스트 우선순위도 sortingOrder를 따름. 들고 있는 카드는 `blocksRaycasts`를 끄지 않음 |
| TC-UI-036 | 카드 선택 패널(`SelectAndDiscard` 등, 손패 대상)이 열린 상태 | 패널 안 카드에 마우스 올림 → 클릭 | 카드가 패널 뒤로 숨거나 패널 밖으로 튀어나오지 않고, 기존처럼 확대·선택 토글만 동작 | 패널(하위 Canvas, Sort Order 3)이 열려 있는 동안에는 앞으로 꺼내지 않음 |
| TC-UI-037 | 게임 창 비율을 16:9 / 16:10 / 21:9로 바꿔 각각 전투 진입 | ① 손패 카드를 집어 조금만(손패 위쪽 경계 아래에서) 움직임 ② 보드 맨 위 줄·양끝 칸을 대상으로 타겟팅 카드 사용 | ① 카드가 사용(NowUsing으로 이동)되지 않고 손에 들린 상태 유지 ② 카드가 손에 들린 상태로 되돌아가지 않고 대상 칸에 사용됨 | HandZone 앵커가 화면 높이에 맞춰 늘어나도록(아래 끝은 손패와 같은 화면 중앙 기준) 설정돼 있어야 함. ⚠ 손패 위치(`heightOffset`)가 화면 중앙 기준이라 21:9에선 손패 아래 절반가량이 화면 밖으로 잘림 |
| TC-UI-038 | 손패 4장 이상, 드로우 카드 보유 | ① 손패 끝 카드에 마우스를 올린 채 다른 카드효과로 드로우가 일어나 손패가 다시 정렬됨 ② 마우스를 뗌 ③ 다른 카드를 집어 손패 위로 지나가게 드래그한 뒤 손패로 되돌려 놓음 ④ 드로우 연출로 날아오는 카드에 마우스를 올림 | ① 호버한 카드가 들린 채 똑바로 선 상태로 새 자리로 미끄러져 감 ② 새 자리의 부채꼴 기울기·높이로 돌아감(옛 자리 각도로 돌아가지 않음) ③ 들고 있는 카드에 가려진 카드들은 반응하지 않고, 놓은 뒤 모두 제자리 기울기·높이로 미끄러져 정렬됨 ④ 날아오는 동안엔 호버 자세가 걸리지 않고, 도착 후 마우스가 위에 있으면 들려 올라옴 | 정렬(`AlignCards`/`ExcludeAlignCards`)은 `Card.SetHandPose`로 슬롯 자세를 넘기고, 이동 연출·선택 패널 배치 시작 시 `Card.CancelHoverPose`로 호버 자세를 버림 |
| TC-UI-039 | 플레이어 턴, 손패에 타겟팅 카드(`AttackCard`)와 타겟팅 없는 카드(`ColDamageUpCard`(Self), `DrawCard`(Inspect)) 보유 | ① 각 카드를 집어 HandZone 안으로 끌고 다님 ② HandZone 밖(손패 쪽)으로 내렸다가 다시 올림 ③ 보드 위에 놓음 / 보드 밖에 놓음 | ① 타겟팅 카드는 NowUsing 위치로 가고 화살표가 나옴. 타겟팅 없는 카드는 이동하지 않고 계속 카드 중앙이 마우스를 따라감 ② 타겟팅 없는 카드는 멈칫하지 않고 계속 따라감 ③ 보드 위에 놓으면 지금처럼 사용되고, 사용이 끝나면 `usedCardZone`으로 모였다가 보드 연출이 끝날 때 버림 더미로 날아감(소멸 카드는 그 자리에서 사라진 뒤 소멸 더미에 나타남). 보드 밖에 놓으면 손패로 돌아감 | 구분 기준은 `Card.NeedsTargeting()`. 기물을 클릭해 고르는 카드(`EmpowerAllyCard`, `SquadTrainingCard`)도 `NeedsTargeting() == false`라 마우스를 따라가며, 놓은 자리에 남은 카드에 가려진 기물은 클릭되지 않음 |
| TC-UI-040 | 플레이어 턴, 손패 4장 이상, 드로우 카드(`DrawCard`·`MoveAndDrawCard`) 보유 | ① 카드 사용 / 버림·소멸 효과 / 카드를 집었다가 손패에 다시 놓기 / 우클릭 취소 ② 턴 시작 5장 드로우가 날아오는 도중 곧바로 카드를 집음 ③ `MoveAndDrawCard` 사용 직후(드로우 연출이 보드 연출 뒤에서 대기 중) 다른 카드를 사용 ④ 재정렬이 진행 중일 때 다른 카드에 호버 | ① 남은 손패가 순간이동하지 않고 0.2초 동안 새 자리로 미끄러지고, 집었던 카드도 제자리로 미끄러져 돌아감 ② 날아오던 카드들이 멈추지 않고 바뀐 새 자리로 휘어 들어감 ③ 대기 중인 드로우 카드가 덱 근처에서 미리 끌려오지 않고, 차례가 되면 그때의 자기 자리로 날아감. 손패를 떠난 카드는 날아오지 않음 ④ 카드가 중간에 멈추거나 떨리지 않고 호버 자세로 이어짐 | 손패 안 자세(정렬·호버)는 카드당 트윈 하나(`Card.SettleToHandPose`)로 현재 위치에서 이어 감. 등장 연출은 `Card.HandSlotTween`이 매 프레임 슬롯을 따라감. 등장 대기 카드는 `CardCanvas.HoldForEntrance`로 정렬에서 제외 |
| TC-UI-041 | 플레이어 턴, 카드 예약 가능, `usedCardZone`(`CardCanvas.UsedCardPos`) 연결됨, 공격 카드 3장·`ColDamageUpCard` 보유 | ① 공격 카드 3장을 앞 카드 연출이 끝나기 전에 연달아 사용 ② 그 사이 다음 카드를 집어 조준 ③ `ColDamageUpCard`를 보드에 놓아 사용 | ① 사용이 끝난 카드들이 `usedCardZone`에 큐 순서대로 (−28, −20)씩 겹쳐 쌓이되 먼저 쓴 카드가 맨 위에 그려져 가려지지 않고(나중 카드는 그 뒤로 비켜 보임), 연출이 끝나는 순서대로 버림 더미로 날아가거나(소멸 카드는 그 자리에서 페이드 아웃) 빠지며, 남은 카드는 한 칸씩 앞으로 당겨짐 ② 조준 중인 카드는 `usingCardZone`에 따로 있어 대기 카드와 겹치지 않음 ③ 놓은 자리에 남지 않고 `usedCardZone`으로 모임 | 정렬은 `CardCanvas.ArrangeAwaitingCards`(대기 목록이 들어오고 나갈 때마다). `UsedCardPos`가 비어 있으면 `CardNowUsingPos`에 모여 조준 중인 카드와 겹칠 수 있음 |
| TC-UI-042 | 전투 중, 덱에 코스트가 다른 카드 여러 장(같은 카드 중복 포함) | `CardsPanel` 덱 보기를 열고 순서 확인 → 닫고 카드 1장 드로우 → 다시 열기 | 카드가 코스트 오름차순, 같은 코스트 안에서는 카드 ID(프리팹 이름) 순으로 나오고 같은 카드끼리 붙어 있음. 실제로 뽑힌 카드는 목록 순서와 무관 | 덱(Queue) 순서 = 드로우 순서를 노출하지 않으려는 정렬. 버림·소멸 보기는 더미 순서 그대로 |
| TC-UI-043 | 전투 중 `SelectAndChangeCost` 카드로 버림·덱 더미의 카드 코스트를 바꿈(또는 `WarmUpDamageCard`처럼 자기 코스트를 깎는 카드를 사용해 버림 더미로 보냄) | 덱/버림 보기 확인 | 복제 카드에 바뀐 코스트가 표시됨(프리팹 기본 코스트로 되돌아가 보이지 않음) | 저장 덱 보기는 원본이 없어 프리팹 값 |
| TC-UI-044 | 전투 중, 소멸 카드(`exileOnUse`) 보유. 씬에 소멸 보기 버튼(OnClick → `CardsPanel.ShowExile`) 연결 | ① 소멸 카드 사용 후 소멸 보기 열기 ② 열어둔 채 소멸 카드를 하나 더 사용 ③ 소멸 보기 버튼 다시 클릭 | ① 소멸된 카드가 보이고 제목 "{기물 이름}의 소멸 카드 (N장)" ② 목록과 장수가 바로 갱신 ③ 패널 닫힘(다른 보기와 같은 토글) | |
| TC-UI-045 | 아군 2명 이상 전투 중 | 덱/버림/소멸 보기를 연 채 다른 아군 클릭(활성 기물 전환) | 목록이 새 활성 기물의 더미로 바뀌고 제목이 "{새 기물 이름}의 덱/버린 카드/소멸 카드 (N장)"으로 바뀜 | 이름은 `PieceInfo.PieceName`(저장 덱 제목과 같은 기준) |
| TC-UI-046 | 덱 보기를 연 채 턴 시작(5장 드로우), 또는 여러 장을 한 번에 버리는 카드 사용 | 관찰 | 패널이 한 프레임에 한 번만 다시 만들어지고 깜빡이거나 끊기지 않음. 최종 장수가 실제 더미와 일치 | |
| TC-UI-047 | 손패에 소멸 카드(`exileOnUse`, 예: `HeavyShieldCard`)와 손패 소멸 효과(`EffectType.ExileHand`) 카드, 일반 카드 보유 | ① 소멸 카드 사용 ② 손패 소멸 효과 사용 ③ 일반 카드 사용 | ① 보드 연출이 끝나면 `usedCardZone` 자리에서 0.25초 동안 페이드 아웃한 뒤 소멸 더미 자리에 다시 보임(날아가는 궤적 없음) ② 고른 손패 카드가 손패 자리에서 같은 방식으로 사라졌다가 소멸 더미에 나타나고, 그 뒤 남은 손패가 재정렬됨 ③ 버림 더미로는 지금처럼 날아감 | 소멸 연출은 `CardCanvas.ExileFadeTween` 하나(사용 후 소멸·`ExileCard`·`ExileCardVisualCor`가 공유) |
| TC-UI-048 | 이동 중(애니메이션 재생 중) 다른 입력 시도 | 애니메이션 진행 중 보드 클릭 | 논리적 점유는 이미 갱신되어 있어 다음 액션이 올바른 위치 기준으로 처리(시각적 지연과 무관) |  |
| TC-UI-049 | 카드가 보드 클릭 중(`isCardEffecting=true`)인 상태 | 다른 아군 기물로 활성 캐릭터 전환 시도 | "카드 사용 중에는 기물을 전환할 수 없습니다" 안내와 함께 전환 거부 |  |
| TC-UI-050 | `Card.IsSelectable()` 확인 | 카드가 `CardCanvas.instance.cards`에서 이미 제거된 상태(다른 코루틴 처리 중)에 클릭 | 실제 리스트 멤버십 기준으로 선택 불가 처리(캐시된 플래그로 인한 오탐 없음) |  |
| TC-UI-051 | 카드 사용 완료 애니메이션이 여러 장 동시에 대기 중(`pendingCardFlights`) | 연속으로 여러 카드를 빠르게 사용 | 각 카드가 올바른 파일-존(버림/추방)으로, 올바른 순서로 날아가 애니메이션이 꼬이지 않음 | ⚠ FIFO 가정에 의존하므로 다중 카드 동시 처리 스트레스 테스트 필요 |
| TC-UI-052 | 손패의 카드를 자신의 손패 영역 위로 빠르게 드래그 인/아웃 반복 | 반복 드래그 | 카드가 의도치 않게 중복 사용되지 않음 | ⚠ `Card.MouseDrag`의 HandZone 진입 자동사용 경로 재현 테스트. 진입 판정은 HandZone 사각형(`CardCanvas.IsScreenPointInHandZone`) 기준 |
| TC-UI-053 | 조준 카드·조준 없는 카드·Self 카드 각각, 에너지 충분 | 좌클릭으로 카드를 든 채 ① HandZone 안(보드 위)에서 우클릭 후 마우스 이동 → 좌클릭 해제 ② HandZone 밖(손패 위)에서 우클릭 ③ HandZone에 들어가기 전 막 집은 상태에서 우클릭 ④ 조준 없는 카드를 보드 위에서 우클릭 누른 채 이동 → 우클릭 해제 | ①~③ 우클릭을 누르는 순간 카드가 손패 슬롯으로 돌아가고, 마우스를 움직여도 다시 들리지 않으며 좌클릭을 떼도 아무것도 사용되지 않음. 에너지·범위 표시·화살표 원래대로 ④ 카드가 사용되지 않음 | `CardCanvas.CancelCardUsage`가 잡힌 카드를 `SelectedFalse`로 놓음. 손패 카드 포인터 이벤트(`Card.MouseDown/Up/Drag`)는 좌클릭만 받음 |
| TC-UI-054 | 전투 중 아군 A가 `EmpowerMoveAttackCard`로 다음 이동공격 +4(지속 -1), tauntAutoAlly가 도발(영구), 적 B가 취약(+1, 2턴) 보유 | 기물 게이지 목록의 상태 줄 확인 | A "다음 이동공격 +4"(버프 파랑 `#5A6BE0`), tauntAutoAlly "도발"(파랑), B "취약 (+1)"(분홍) 뒤에 "2턴"(회색 `#AAAAAA`). 남은 턴은 이름과 색이 다름. 지속이 음수인 효과는 이름만 나오고 "이동공격까지"·"영구"가 붙지 않음 | 규칙은 `StatusEffect.StatusLabel` 하나: 지속 0 이상이면 이름 뒤에 회색 "N턴", 음수면 이름만. 다음 이동공격 버프도 지속이 양수면 "N턴"이 붙음 |
| TC-UI-055 | 손패 3장 이상, `SacrificeShieldCard` 사용으로 카드 선택 패널(손패 대상)이 열린 상태 | 패널 안 카드 하나에 마우스를 올렸다가 뺌 → 다른 카드로 반복 → 하나 클릭해 선택 후 마우스를 뺌 | 마우스를 올리면 그 자리에서 살짝 들리고, 빼면 원래 격자 칸으로 정확히 돌아옴(옆 카드와 줄이 어긋나거나 다른 자리로 가지 않음). 선택한 카드는 확대 유지 | 패널 Content의 `GridLayoutGroup` 배치를 `LayoutRebuilder.ForceRebuildLayoutImmediate`로 바로 적용하고 그 칸을 호버 복귀 자리로 기억(`CardCanvas.LayoutCardsInPanel`). 회귀: 예전엔 코드가 계산한 한 줄 자리로 돌아갔음 |
| TC-UI-056 | ① `SacrificeShieldCard`로 연 카드 선택 패널(1장 선택), 손패 3장 이상 ② 2장 선택 패널(아래 비고) | ① 카드 A 클릭 → 카드 B 클릭 → 확인 ② A → B 클릭 후 C 클릭 ③ 선택된 카드를 다시 클릭 | ① B를 누르는 순간 A의 확대가 풀리고 B가 확대, "1/1 선택됨" 유지, 확인하면 B만 버려짐 ② C를 누르면 가장 먼저 고른 A만 해제되고 B·C 선택, "2/2 선택됨" ③ 그 카드만 해제(기존 동작) | 꽉 찼을 때 새 카드를 누르면 가장 먼저 고른 카드와 교체(`CardCanvas.ToggleCardInPanel`, 고른 순서는 `selectedInPanel` 리스트 순서). 선택 제한이 없는 패널(selectCount 0)은 교체 없음. ⚠ 2장 이상 고르는 패널이 정상 플레이에 없음 — ②는 `SacrificeShieldCard`의 `selectCount` 또는 다이얼로그 `removeCardCount`를 2로 임시 수정해 확인 후 원복 |

---

## 15. 승리/패배 조건

**관련 스크립트**: `GameManager.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-WIN-001 | 마지막 남은 적 처치 | 처치 직후 | 적 리스트 카운트 0 도달 시점에 즉시 승리 처리(`FinishLevel` 경로) | |
| TC-WIN-002 | 마지막 남은 아군 사망 | 사망 직후 | `TriggerDefeat` 호출되나 현재는 로그만 출력, 실질적 패배 화면/흐름 없음 | ⚠ 패배 플로우 미구현 |
| TC-WIN-003 | 같은 틱에 마지막 아군과 마지막 적이 동시 사망(예: 상호 이동공격 반격) | 동시 사망 유발 | 승리(`RemoveEnemy`)와 패배(`RemoveAlly`) 로직이 모두 실행될 때 상태 충돌 여부 확인 | ⚠ 레이스 컨디션 가능성 |
| TC-WIN-004 | `currentNodeX < 0`인 상태(맵 선택 없이 진입, 예: 테스트 부트스트랩/일부 다이얼로그 전투) | 레벨 클리어 | `FinishCurrentLevel()`이 노드 0을 강제로 방문 처리 | |
| TC-WIN-005 | 마지막 층이 아닌 일반 층 클리어 | 클리어 처리 | `Board.GrantLevelReward()` 경로로 진행(런 종료 아님) | |
| TC-WIN-006 | 애플리케이션 종료 시점(`OnApplicationQuit`)에 마지막 기물이 파괴됨 | 종료 처리 중 관찰 | `isQuitting` 가드로 인해 승리/패배 판정이 오발동하지 않음 | |

---

## 16. 직업 시스템

**관련 스크립트**: `JobInfo.cs`, `Assets/SO/Jobs/Warrior.asset`, `Assets/SO/Jobs/Summoner.asset`, Confluence "카드 목록" 페이지(전사·소환사)

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-JOB-001 | Summoner 기물 보상 오픈 | 보상 풀 확인 | `Summoner.asset`의 24개 카드(SummonCard, GreaterSummonCard, TauntSummonCard, EmpowerAllyCard, SummonMasteryCard, AreaHealCard, SummonGrowthCard, MagicAttackCard, ZoneAttackCard, MagicMissileCard, MagicVulnerableAttackCard, CurseCard, LoadMagicMissileCard, ImmobilizeCard, WeakenDrawCard, MoveAndDrawCard, SafeMoveCard, WardZoneCard, VulnerableCard, GraveHealCard, GraveAttackCard, GraveHarvestCard, GraveDefenseCard, GrowingGraveCard) 전부 실제 프리팹으로 존재 확인 | |
| TC-JOB-002 | Warrior 기물 보상을 반복 오픈(예: 50회) | 카드명 수집 | `FinalAttackCard`도 보상으로 제시됨(프리팹이 `Database.prefab`의 `cardPrefabs`에 등록되어 `PickRandomDistinctFrom`에서 스킵되지 않음) | |
| TC-JOB-003 | Warrior 보상 풀의 30개 카드 각각 | 보상으로 제시될 때 수치 확인 | Confluence 전사 카드 표에 명시된 코스트·수치(예: HeavyAttackCard 코스트 2·6뎀/2자해, DoubleAttackCard 코스트 3·4뎀×2)와 실제 구현이 일치 | |
| TC-JOB-004 | `Warrior.asset`(30장)·`Summoner.asset`(24장)과 Confluence 카드 목록(전사·소환사 페이지) | 두 목록을 대조 | 카드 구성·개수가 일치하고, 카드 표의 설명이 게임 내 카드 설명(`EffectDescription`)과 같음 | 카드를 보상 풀에 추가·제거할 때마다 Confluence 카드 목록 페이지도 함께 갱신 |
| TC-JOB-005 | Job이 설정되지 않은 기물 | 보상 오픈 | `ResolveRewardPoolFor`가 전체 카드 목록으로 폴백 | |

---

## 17. 교차 시스템 회귀 테스트

과거 수정 이력이 코드 주석으로 남아있어, 회귀 가능성이 높은 항목들이다.

**관련 스크립트**: `Board.ScheduledEffects.cs`(`EnqueueEffect`·`InsertEffectNext`·`ResolveEffects`), `Board.Combat.cs`(`TriggerOnKillEffect`), `Board.CardEffect.cs`(`ProcessNextCardEffectStep`, `ApplyCardEffectNow`, `ApplyPieceSelectionEffect`), `CardCanvas.cs`, `Board.TurnEffects.cs`, `Piece.onSpawnCards`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-REG-001 | 단일 셀 방향성 이동공격 범위(예: Warrior류)를 가진 `ChainMoveAttackBuff` 보유 기물 | 방향 회전된 이동공격으로 정확히 1명만 타격 | 회전된 범위가 "1칸짜리 특수 패턴"이라는 이유로 체인이 잘못 차단되지 않고 정상 발동(`hitMultipleTargets`는 실제 스플래시 적중 수 기준, `isAreaAttack` 플래그 기준 아님) | 과거 버그 수정 사항 회귀 테스트 |
| TC-REG-002 | 자가 비용 감소 효과(`WarmUpDamageCard`)를 가진 카드 시전 | 시전 시점의 에너지 차감량 확인 | 이번 시전에 비용 감소가 소급 적용되지 않고, 차감은 원래 비용 기준으로 발생(에너지 차감 로직이 `FinishUseCard`가 아닌 `ExecuteEffect` 시점에 있음) | 과거 버그 수정 사항 회귀 테스트 |
| TC-REG-003 | 새 `EffectType` 추가 시 | 그 타입을 일반 카드 효과·처치 시 효과(`onKillEffect`)·기물 선택(`pieceSelectCount`)·턴 효과·유물에 각각 넣어 실행 | 모든 경로에서 동일하게 동작(전부 `ApplyCardEffectNow` 한 곳을 거침). 분기를 빠뜨리면 "효과 타입을 찾지 못했습니다" 에러 로그 | 처치 시 효과·영구 버프 전용이던 좁은 경로(`ExecuteCardEffectOnPiece`)는 제거됨. TC-REG-012 참고 |
| TC-REG-004 | 여러 카드 효과가 동시에 `motionQueue`에 쌓이는 상황(예: 이동공격 스플래시 + 체인 + DoT 동시 트리거) | 빠르게 연쇄 발동 | `awaitingFlyOut`/`pendingCardFlights`의 FIFO 순서 가정이 깨지지 않고 카드별로 올바른 완료 신호가 매칭됨 | ⚠ 동시다발 상황 스트레스 테스트 권장 |
| TC-REG-005 | 취약이 없는 상태 | 단일 공격, 광역 공격, 이동공격(스플래시 포함), `healOnHit` 카드(`LifeDrainCard`, `BloodChargeCard`), 체인 이동공격을 각각 실행 | 피해 텍스트, 체력 감소, 사망 판정, 자힐량, On-Hit 유물 발동이 공격 피해 공통화(`ApplyAttackDamage`) 이전과 동일 | 공격 피해 경로 공통화 회귀 테스트 |
| TC-REG-006 | 공격으로 피해를 주는 새 경로(카드/효과) 추가 시 | 해당 경로로 취약 대상 공격 | 취약 보정이 적용됨(`Board.ApplyAttackDamage`를 거침) | ⚠ 새 경로가 `GetDamage`를 직접 호출하면 취약이 조용히 누락됨. 신규 공격 경로 추가 시 회귀 취약 지점 |
| TC-REG-007 | 턴 효과 만료 규칙이 `StatusEffect.OnTurnEnd` 공용 규칙으로 바뀜(음수 지속시간 = 영구) | `FlameThrowingCard`(3턴), `PersistentShieldCard`(다음 턴 1회) 사용 후 턴 진행 | `FlameThrowingCard`는 정확히 3회 발동 후 만료, `PersistentShieldCard`는 다음 턴 시작에 1회 발동 후 만료(변경 전과 동일) | 턴 효과 만료 규칙 공통화 회귀 테스트 |
| TC-REG-008 | `Piece.onSummonCards`가 `onSpawnCards`로 이름 변경(`FormerlySerializedAs`). `autoally.prefab`은 옛 이름으로 저장됨 | `SummonCard`로 autoally 소환 | 스폰 시 효과(`DefenseCard`) 연결이 유지되어 방어도 2 획득 | ⚠ `FormerlySerializedAs`를 지우면 연결이 끊김. 프리팹을 다시 저장해 새 필드명으로 갱신 권장 |
| TC-REG-009 | `lockedCaster`/`lockCasterForNext`가 제거되고 효과마다 시전자(`CardEffect.caster`)를 기록하는 방식으로 바뀜 | `MoveandAttackCard`, `MoveAndDrawCard`, `SummonGrowthCard`, `SummonMasteryCard`, `HeavyAttackCard`(자해) 각각 사용 | 두 번째 이후 효과가 카드를 쓴 기물의 현재 위치 기준으로 실행되고, 다른 아군에게 효과가 새지 않음 | 시전자 처리 방식 변경 회귀 테스트. TC-FX-AIM-009~011 참고 |
| TC-REG-010 | 전투 시작 유물이 즉시 적용에서 효과 큐 처리로 바뀜 | `ShieldRelic` 보유 상태로 아군 2명 이상 전투 진입 | 아군마다 방어도 +3, 첫 턴 입력·기물 전환 정상 | TC-REG-018, TC-RELIC-001 참고 |
| TC-REG-011 | 이동공격 타격부가 `ResolveMoveAttackHit`로, 공격 1타 처리가 `StrikeTarget`으로 공용화됨 | 일반 이동공격(처치 시 전진·생존 시 인접 칸·도착 칸 없음 실패·대각선), `ChargeCard`, `BloodChargeCard`(스플래시 포함), `LethalChargeCard`, 적 이동공격, 가시 반격, 연쇄, `DoubleAttackCard`, `FinalAttackCard`를 각각 실행 | 피해·사망·전진 위치·방어도·자힐·처치 효과·연쇄·반격과 연출 순서(다가감 → 마주봄 → 공격)가 변경 전과 동일 | 공격 범위 표시는 이제 도착 칸 기준으로 고정(예전엔 처치 후 전진한 칸 기준으로 보일 수 있었음). 카드 효과 기본 TC의 MOVE·CHAIN 영역 참고 |
| TC-REG-012 | 처치 시 효과와 기물 선택 결과가 즉시 실행에서 효과 큐 맨 앞 삽입(`executeOnCaster`)으로 바뀜 | `ExecutionerCard`·`GraveHarvestCard`로 처치, `LethalChargeCard`로 이동공격 처치, `ZoneAttackCard` 등 광역으로 여러 적 동시 처치(처치 시 효과 카드가 있다면), `EmpowerAllyCard`·`SquadTrainingCard` 사용, 처치 직후 같은 카드의 다음 효과·다음 카드 사용 | 수치는 변경 전과 같고(힘 +1 영구 / 무덤 +1 / 코스트 +2 최대치 클램프 / 선택 기물 힘 영구 증가), 상태는 연출을 기다리지 않고 다음 효과로 넘어갈 때 즉시 반영됨. 처치 시 효과가 카드의 다음 효과(대상 선택, `useLastTarget`)나 입력 대기를 방해하지 않음. 에러 로그 없음 | 처치 시 효과 연출은 바깥 효과의 연출(다른 대상 사망·연쇄 이동공격 등) 뒤에 재생. 처치 순간 시전자도 죽었으면 처치 시 효과는 스킵 |
| TC-REG-013 | 소환될 기물 프리팹의 `onSpawnCards`(스폰 시 효과)에 `DefenseCard`(self 실드) 연결 | `SummonCard`로 소환 | 소환 연출 → 소환된 기물이 시전자로 실드. 카드 종료 후 선택/사거리 표시 정상 해제, 에너지 1회만 차감 | 소환 시 효과는 소환된 기물 칸에서 실행(효과 큐 `executeOnCaster`) — **자기 대상 효과만 가능**, `NearestEnemy` 같은 자동 대상 선택 없음. 현재 소환 시 카드(TauntCard, BossWoundOnHitCard, BossRageCard)는 모두 자기 대상 |
| TC-REG-014 | 소환 효과 뒤에 효과가 더 있는 카드 + 소환 시 효과를 가진 기물 | 카드 사용 | 소환 → 소환 시 효과 → 카드의 나머지 효과 순서로 실행, 나머지 효과의 시전자는 원래 카드 시전자 | |
| TC-REG-015 | 적 AutoPiece의 `actionCards`에 user=Enemy 소환 카드, 소환될 기물에 소환 시 효과 연결 | 적 턴 진행 | 소환 시 효과가 소환된 기물 기준으로 실행되고 적 턴이 멈추지 않고 진행 | |
| TC-REG-016 | 소환 시 효과 중 앞 효과가 자해로 소환된 기물을 처치 | 소환 | 남은 소환 시 효과는 스킵되고 카드는 정상 종료 | |
| TC-REG-017 | 소환 시 효과에 `graveCost`가 있는 효과 | 소환 | 소환된 기물의 무덤(0부터 시작) 기준으로 판정 — 부족하면 그 효과만 스킵 | |
| TC-REG-018 | 전투 레벨에서 아군 프리팹과 레벨 배치 적 프리팹의 `onSpawnCards`에 `DefenseCard` 연결, `ShieldRelic` 보유 | 전투 진입 | 전투 시작 시 배치 순서대로(아군 → 레벨 배치 기물) 스폰 시 효과가 실행된 뒤, 아군마다 `ShieldRelic` 실드가 적용. 첫 턴 정상 진행, 활성 기물 전환이 막히지 않음 | 스폰 시 효과와 전투 시작 유물이 같은 효과 큐에서 함께 처리됨 |
| TC-REG-019 | `onSpawnCards`가 연결된 기물이 이벤트 레벨(휴식/상점/대화)에 스폰 | 이벤트 레벨 진입 | 스폰 시 효과가 발동하지 않음 | 유물 전투 시작 효과와 동일하게 전투 레벨에서만 |
| TC-REG-020 | 전투 레벨에서 대화 선택지로 `onSpawnCards`가 연결된 기물이 합류(`SpawnPiece`) | 선택지 선택 | 합류 즉시 스폰 시 효과가 실행 | |
| TC-REG-021 | `effects[0].effectRange`가 비어있는 커스텀 카드(데이터 오류 상황) | 카드 `Awake()` 실행 | 인덱싱 시 예외 발생 여부 확인(리그레션 방지용 데이터 무결성 점검) |  |
| TC-REG-022 | 일반 `AddColDamage(음수, permanent 무관)` 경로(Weaken이 아닌 다른 효과) | 큰 음수값 적용 | ⚠ 별도 하한 클램프가 없어 `colDamage`가 음수가 될 수 있는지 확인 |  |
| TC-REG-023 | 이미 `isDeathScheduled`인 기물에 추가 피해/사망 트리거 발생 | 중복 사망 처리 유발 | `DeathCor`가 중복 실행되지 않음(idempotent) |  |
| TC-REG-024 | 독·화상·재생 틱이 직접 적용에서 효과 큐(`SelfDamage`/`Heal` 효과)로 바뀜 | `CurseCard`(독)·화상·재생을 아군과 적에게 각각 걸고 턴 진행, 독으로 죽을 체력의 기물 포함 | 수치·지속시간·만료는 변경 전과 같음. 피해에 타격음 없음(비공격), 피격/회복 반응 연출이 재생됨. 틱으로 죽으면 같은 팀 무덤 +1, 사망 연출, 그 기물의 남은 틱(화상·재생)은 스킵 | 틱은 자기 턴 종료 시. TC-FX-STATUS 영역 참고 |
| TC-REG-025 | 턴 효과(`TurnEffect`)가 예약 큐(코루틴, 효과마다 연출 대기)에서 공용 효과 큐로 바뀜 | `PersistentShieldCard`·`GrowingGraveCard`(턴 시작), `SummonGrowthCard`·`FlameThrowingCard`·`WardZoneCard`(턴 종료), `BossRageCard`(적 턴 종료) 사용 후 턴 진행 | 수치·지속시간은 변경 전과 같고, 같은 타이밍의 효과가 모두 끝난 뒤 다음 타이밍(드로우·턴 넘김)으로 진행. 턴 효과로 처치하면 처치 시 유물이 그 효과 직후 발동 | TC-TURN-021·022 |
| TC-REG-026 | 반응 효과(처치 시·피격 시·카드 사용 시 유물, 처치 시 효과, 소환 시 효과, 기물 선택 결과)가 효과 큐 맨 앞에 유발 순서대로 들어감 | `VampiricFangRelic` 보유 아군이 광역 카드(`ZoneAttackCard` 등)로 적 2기 동시 처치, `ExecutionerCard`로 처치 | 처치 1건마다 유물 회복 2가 그 효과 직후에 실행되고(총 4), 카드의 다음 효과보다 먼저 끝남. `ExecutionerCard`는 유물 회복 → 힘 +1 순서 | 피격 시 유물은 피격 "도중"이 아니라 그 효과 직후로 바뀜(현재 해당 유물 없음) |
| TC-REG-027 | 이동 불가(`MovementDisabled`) 상태의 기물 | 이동 효과가 들어간 카드 사용(`Card.PrepareUseEffects`가 걸러내지 못하는 경로 포함) | "이동 불가!" 표시 후 그 카드의 남은 효과만 취소되고, 처치 시 효과 등 이미 큐에 들어간 엔진 효과는 그대로 실행. 엔진이 넣은 Move(유물·턴 효과)가 막히면 진행 중인 카드 효과는 지워지지 않음 | `Board.RemoveCardEffects` |
| TC-REG-028 | 충전(`EnemyChargeCard`) 예고 중인 적에게 독 | 적 턴 종료(독 틱) → 다음 적 턴 | 독 틱 뒤에도 충전 표시(Charge 애니메이션)가 유지되고, 다음 행동에서 정상 해제 | 엔진이 넣은 효과(`executeOnCaster`)는 Charge를 끄지 않음 |
| TC-REG-029 | 무덤 비용이 있는 기물 선택 효과(현재 해당 카드 없음 — 테스트용으로 `pieceSelectCount` + `graveCost` 효과 구성) | 무덤이 0인 아군을 골라 사용 | 무덤은 카드 시전자가 한 번만 내고, 선택된 기물에게 무덤을 다시 요구하지 않아 효과가 정상 적용 | `ApplyPieceSelectionEffect`가 사본의 `graveCost`·`consumeAllGrave`·`pieceSelectCount`를 지움 |

---

## 18. 무덤(Grave) 시스템

무덤은 기물마다 따로 쌓이는 전투 한정 자원이다(`Piece.grave`). 같은 팀 기물이 죽으면 그 순간 살아 있는 같은 팀 기물 전원이 무덤 +1을 얻는다(소환수 포함). 저장되지 않으므로 전투마다 0에서 시작한다. 카드 효과의 `graveCost`로 소모하며, 무덤은 카드를 낸 기물(시전자)의 것을 쓴다. 첫 효과의 무덤이 부족하면 카드를 쓸 수 없고, 두 번째 이후 효과의 무덤이 부족하면 그 효과만 건너뛴다. 무덤은 효과가 실제로 실행될 때 차감된다. 카드별 케이스는 [6.3 소환사](#63-소환사)의 TC-CARD-055~059(무덤 카드)에 있다.

**관련 스크립트**: `Piece.cs`(`AddGrave`/`HasGrave`/`ConsumeGrave`), `Board.Occupancy.cs`(`AddGraveToTeammates`), `Board.CardEffect.cs`(`PayGrave`, `ProcessNextCardEffectStep`), `Card.HasGraveForFirstEffect`, `CardCanvas.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-GRAVE-001 | 이전 전투에서 무덤을 쌓은 채 승리 | 다음 전투 레벨 진입 | 모든 기물의 무덤이 0에서 시작(이월되지 않음) | |
| TC-GRAVE-002 | 아군 A, B와 소환수 C 생존 | 소환수 C가 사망 | A, B 위에 각각 "무덤 +1", 무덤 1. 소환수 사망도 집계됨 | |
| TC-GRAVE-003 | 아군 A, 적 2기 | 적 1기 처치 | 아군 A의 무덤은 변화 없음. 남은 적 위에 "무덤 +1" 텍스트 | ⚠ 적은 무덤을 쓰는 카드가 없어 적 쪽 "무덤 +1" 연출은 의미 없는 표시. 의도 확인 필요 |
| TC-GRAVE-004 | 아군 3기 중 2기가 체력 5 이하, `ZoneAttackCard`(팀 무관 5 피해) 범위 안 | 두 기물이 같은 공격에 함께 사망하도록 시전 | 살아남은 아군만 무덤 +2(사망 1기당 +1). 함께 죽은 두 기물은 서로의 사망으로 무덤을 받지 않음 | |
| TC-GRAVE-005 | 사망 처리 중인 기물에 추가 피해가 겹치는 상황(예: 광역 공격 + 턴 종료 DoT) | 같은 기물이 연달아 사망 판정 | 그 기물의 사망은 무덤에 한 번만 집계 | |
| TC-GRAVE-006 | 활성 기물의 무덤 0, 손패에 `GraveHealCard`(첫 효과 무덤 1) | 카드 상태 확인 후 사용 시도 | 카드가 사용 불가 상태로 표시되고, 사용을 시도하면 "무덤이 부족합니다" 안내와 함께 거부 | |
| TC-GRAVE-007 | TC-GRAVE-006 상태 | 다른 아군이 사망 | 손패의 `GraveHealCard`가 즉시 사용 가능 상태로 바뀜 | |
| TC-GRAVE-008 | 아군 A 무덤 1, 아군 B 무덤 0, 두 기물 손패에 모두 `GraveHealCard` | A, B로 활성 기물을 바꿔 가며 확인 | A의 카드만 사용 가능. 무덤은 기물별로 따로 관리됨 | |
| TC-GRAVE-009 | 시전자 무덤 1, `GraveHealCard` | 카드를 집어 대상 선택 단계에서 취소 | 무덤 1 유지, 에너지 변화 없음 | |
| TC-GRAVE-010 | 무덤이 있는 아군 | 그 기물이 있는 칸에 마우스를 올려 칸 정보창 확인 | 칸 정보창의 스탯 창에 "무덤 N"이 현재 값으로 표시됨 | |
| TC-GRAVE-011 | 시전자 무덤 1에 `GraveHealCard`, 또는 시전자 무덤 3에 `GraveDefenseCard`(무덤 모두 소모) | 카드 사용 | 효과 연출 직전 시전자 위에 소모량만큼 "무덤 -1" / "무덤 -3" 텍스트가 회색으로 떠오름(파티클·사운드 없음). 이어서 칸 정보창의 무덤 수가 0으로 표시됨 | |

---

## 19. 카드 정보창(호버 툴팁)

카드에 마우스를 올리면 카드 옆에 정보창이 뜬다. 창 하나에 정보 하나(범위 격자 또는 용어 설명)이며, 범위 창이 먼저, 용어 창이 다음이다. 창 폭은 모두 같고(카드 로컬 320), 카드 윗변부터 아래로 쌓다가 다음 창이 카드 아랫변을 넘으면 바깥쪽에 새 줄을 만들어 맨 위부터 다시 쌓는다. 정보창은 카드의 자식 `TooltipRoot`로 런타임 생성된다. 호버 후 0.3초(실제 시간) 뒤 0.2초 동안 페이드 인한다.

**관련 스크립트**: `Card.cs`(`MouseEnter`/`MouseExit`/`ShowTooltip`/`SetFront`/`ExtraKeywords`), `CardTooltipView.cs`(창 생성·배치), `CardTooltipContent.cs`(범위·용어 구성), `KeywordGlossary.cs`(용어집)

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-TIP-001 | 손패에 `AttackCard` | 카드에 마우스 올림 → 뗌 | 약 0.3초 뒤 카드 오른쪽에 "사거리" 창 1개가 0.2초 동안 페이드 인(격자, 중심 칸 파랑·사거리 칸 주황). 용어 창 없음. 뗄 때 사라짐 | |
| TC-TIP-002 | 손패에 `ZoneAttackCard` | 마우스 올림 | "사거리"("보드 전체" — `noRangeLimit`) → "효과 범위"(중심 칸 노랑, "지정한 칸을 중심으로 펼쳐집니다.") → "마법 피해" 순 | |
| TC-TIP-003 | 손패에 `MoveandAttackCard` | 마우스 올림 | "이동 범위"(활성 기물의 이동 범위 격자)와 "사거리" 창이 따로 뜸. 용어 창은 "이동", "이동공격"이 맨 앞 | |
| TC-TIP-004 | 손패에 `MagicVulnerableAttackCard`, `ThornCard`, `ChainMoveAttackCard` | 각각 마우스 올림 | 각각 마법 피해·취약 / 가시·버프·이동공격 / 이동공격·버프·소멸 용어 창. "이동공격 판정"이 있는 설명에서 "이동공격" 창이 중복으로 뜨지 않음 | |
| TC-TIP-005 | 손패에 `DirectionalAttackCard` | 마우스 올림 후 보드에서 카드를 들어 위쪽 방향을 겨눔 | "효과 범위" 격자(+"4방향 중 고른 방향으로 회전합니다.")가 위쪽을 겨눴을 때 보드 하이라이트 모양과 같은 방향 | 격자 방향은 RangeInfoSO 인스펙터 배치(행 0 = 위) 기준 |
| TC-TIP-006 | 창이 4개 이상 나오는 카드(예: 범위 2개 + 용어 3개) | 마우스 올림 | 모든 창 폭이 같고, 다음 창이 카드 아랫변을 넘을 때 오른쪽에 새 줄이 생겨 카드 윗변 높이부터 다시 쌓임 | |
| TC-TIP-007 | 손패 5장 이상 | 오른쪽 끝 카드에 마우스 올림 | 오른쪽 공간이 부족하면 정보창이 카드 왼쪽에 같은 규칙으로 배치(새 줄은 왼쪽 바깥으로) | |
| TC-TIP-008 | 손패 여러 장 | 카드 호버 → 정보창이 덮은 이웃 카드 위로 마우스 이동 | 정보창이 이웃 카드보다 위에 그려지고, 마우스는 정보창을 통과해 이웃 카드 호버가 정상 동작 | |
| TC-TIP-009 | 손패 카드 호버 중 | 카드를 집어 드래그 → 손패로 되돌림 | 집는 순간 정보창이 사라지고 드래그 중엔 다시 뜨지 않음 | |
| TC-TIP-010 | 카드 선택 패널(`SelectAndDiscard` 등)이 열린 상태 | 패널 안 카드에 마우스 올림 | 정보창이 패널 위에 뜸. 이동 범위는 활성 기물 기준 격자 | |
| TC-TIP-011 | 상점 진입 | 진열 카드에 마우스 올림 → 뗌 → 구매 | 카드가 1.3배로 확대되며 이웃 슬롯 위로 그려지고 정보창이 뜸. 슬롯 정렬은 흐트러지지 않음. 이동 카드는 이동 범위 창 없이 "이동"·"이동공격" 용어 창만. 구매 후 정보창이 남지 않음 | 슬롯에 마스크가 있으면 정보창이 잘리는지 확인 |
| TC-TIP-012 | 전투 승리 후 보상 화면 | 보상 카드에 마우스 올림 → 뗌 → 선택 | TC-TIP-011과 같이 확대 + 정보창, 뗄 때 원래 크기로 복귀 | |
| TC-TIP-013 | 손패에 `DrawCard`, `DefenseCard` 등 자기 대상 카드 | 마우스 올림 | 범위 창 없음(용어가 있으면 "드로우"·"방어도" 등 용어 창만) | |
| TC-TIP-014 | 맵 또는 전투에서 덱 보기(`CardsPanel` 저장 덱/덱/버림 보기) 열기 — 카드가 Grid Layout Group 셀(260×260)에 들어감 | 창이 3개 이상 나오는 카드(예: `ZoneAttackCard`)에 마우스 올림 | 손패와 같은 규칙으로 배치: 카드 그림 바로 옆(겹치지 않음)에서 시작해 카드 윗변부터 아래로 쌓이고, 카드 그림 아랫변을 넘을 때만 다음 줄 | 배치 기준은 RectTransform 크기가 아니라 스폰 시점의 카드 원래 크기(500×600). 스크롤 뷰 마스크에 정보창이 잘리는지도 확인 |
| TC-TIP-015 | 손패 여러 장 | ① 카드에 마우스를 올렸다가 0.3초 안에 뗌 ② 카드 위에서 이웃 카드로 빠르게 훑고 지나감 ③ 게임 배속을 바꾸거나 히트스탑 중에 호버 | ① 정보창이 전혀 보이지 않음 ② 지나간 카드의 정보창은 뜨지 않고, 마지막으로 멈춘 카드에서만 0.3초 뒤 페이드 인 ③ 배속·히트스탑과 무관하게 항상 실제 시간 0.3초 뒤에 뜸 | 지연·페이드는 DOTween `SetUpdate(true)`(unscaled time). 같은 카드를 다시 호버하면 지연이 처음부터 다시 시작 |
| TC-TIP-016 | 손패에 `WoundCard` | 마우스 올림 | 용어 창 "사용 불가"("이 카드는 어떤 방법으로도 사용할 수 없습니다. 버리거나 소멸시키는 효과의 대상은 됩니다.")가 뜸. 칸 정보창 등 카드 이미지로 보여 줄 때도 코스트 숫자가 없음 | `KeywordGlossary` |
| TC-TIP-017 | 소멸 저주에 걸린 아군 / `Enemy-CurseKnight`의 다음 행동 카드 | 보드 칸 정보창, 행동 카드에 마우스 올림 | 상태 목록에 "소멸 저주"와 용어 설명("지속되는 동안 사용하는 카드가 모두 소멸합니다."), 행동 카드 호버 시에도 "소멸 저주" 용어 창 | `KeywordGlossary`, `BoardTooltipContent.GlossaryTermOf`, `CardTooltipContent.AddStatusTerm` |
| TC-TIP-018 | `L2-Bond`의 결속 적 | 보드 칸 정보창 | 상태 목록에 "결속"과 용어 설명이 뜸 | `BondEffect`, `BoardTooltipContent.GlossaryTermOf` |
| TC-TIP-019 | `L3-Berserker`의 보스 | 보드 칸 정보창 | 상태 목록에 "상처 입히기"(영구)와 용어 설명 "이 기물에게 공격 피해를 받아 체력이 줄면 버린 카드 더미에 상처가 2장 들어갑니다."가 뜸 | `WoundOnHitEffect`, `BoardTooltipContent.GlossaryTermOf` |
| TC-TIP-020 | 전투 중, 활성 기물의 덱·버린 카드·소멸 카드에 `MoveCard`·`SafeMoveCard`, 손패에 `MoveCard`·`ChainMoveAttackCard` | ① 덱·버린 카드·소멸 카드 보기 패널에서 `MoveCard`에 마우스 올림 ② 같은 패널에서 `SafeMoveCard` ③ 손패의 `ChainMoveAttackCard` ④ 손패의 `MoveCard` | ① "이동 범위" 창이 뜨지 않음. 용어 창 "이동"("기물의 이동 범위 안의 칸으로 움직입니다."), "이동공격" ② "이동" 창만(이동공격 창 없음) ③ "이동" 창 없음(설명의 "이동 범위"는 용어로 잡지 않음) ④ "이동 범위" 격자 창 + "이동"·"이동공격" 용어 창 | 기물을 모르면(`CardTooltipContent.Build`의 owner가 null — 더미 보기·상점·보상·저장 덱) 이동 범위 창을 띄우지 않음(`AddMoveRange`). "이동"은 Move 효과 데이터로만 붙음(`KeywordGlossary` `MatchInText = false`). 회귀: 예전엔 격자 없이 "기물의 이동 범위" 문구만 든 빈 창이 떴음 |

---

## 20. 보드 칸 정보창

보드 칸(`Button`)에 마우스를 올리면 카드 정보창(섹션 19)과 같은 UI로 칸의 상태와 칸 위 기물의 상태를 함께 띄운다. 칸 상태(장판 등)는 아직 없어 지금은 기물 정보만 나오고, 빈 칸은 띄우지 않는다. 칸은 3D 오브젝트라 화면 공간 오버레이 캔버스(`BoardTooltipCanvas`, sortingOrder 10)의 앵커를 칸 화면 위치로 옮겨 띄운다. 창 크기는 호버된 손패 카드와 같은 스케일(0.65)이고, 칸 윗변부터 호버된 카드 높이만큼 쌓은 뒤 줄을 바꾼다. 0.3초 지연 후 페이드 인. 기존 `ButtonInfo` 정보창은 그대로 함께 뜬다.

**관련 스크립트**: `Button.cs`(`MouseEnter`/`MouseExit`), `Board.TileTooltip.cs`, `BoardTooltipHost.cs`(오버레이·앵커·배치), `BoardTooltipContent.cs`(칸·기물 내용), `TooltipView.cs`(창 UI·카드 이미지), `CardTooltipContent.cs`(`BuildRanges`), `Card.cs`(`ShowFaceOf`, `previewCasterOverride`), `AutoPiece.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-TILE-001 | 전투, 빈 칸 | 빈 칸에 마우스를 올리고 1초 기다림 | 정보창이 뜨지 않음(칸 상태가 아직 없어 보여줄 정보가 없음) | 칸 상태(장판 등)는 `BoardTooltipContent.AddTileState`에 추가할 자리만 있음 |
| TC-TILE-002 | 아군 기물 칸, 무덤 1, 힘 버프(+2) | 마우스 올림 → 뗌 | 0.3초 뒤 칸 오른쪽에 페이드 인: 스탯 창(제목 = 기물 이름, "아군", 체력 hp/maxhp, 방어도, 힘 "N (+2)" 파랑, 방어막 보너스, 무덤 1) → "이동 범위" 격자. 뗄 때 사라짐 | 창 폭·글자 크기가 손패 카드 정보창과 같음 |
| TC-TILE-003 | 적 칸, 적이 독(3) 2턴·취약(+1) 상태, 다음 행동 `EnemyAttackCard`, 적 힘 +2 | 마우스 올림 | 스탯 창("적", 무덤 없음) → "독 (3)" 창(독 색 제목, "2턴 남음" + 독 설명) → "취약 (+1)" 창 → "다음 행동" 카드 이미지(피해 수치가 적의 힘을 반영해 파랑) → "다음 행동 사거리" 격자 | 다음 행동 카드는 템플릿(`AttackCard` 프리팹)에 원래 카드의 이름·코스트·설명을 표시 |
| TC-TILE-004 | 적을 `StunCard`로 기절 | 그 칸에 마우스 올림 | "기절" 디버프 창, 다음 행동 카드 이미지가 `StunnedCard`("기절", 코스트 숫자 없음), 다음 행동 범위 창 없음. "기절" 용어 창은 따로 뜨지 않음(상태 창이 이미 설명) | |
| TC-TILE-005 | 아군이 `ChainMoveAttackCard`·`EmpowerMoveAttackCard`·`FlameThrowingCard`를 사용 | 그 칸에 마우스 올림 | "연쇄 이동공격"(이번 전투 동안), "다음 이동공격 +4"("이동공격할 때까지" + 소모 안내), "턴 종료 시 광역 피해 2"("N턴 남음" + 범위 격자, 중심 파랑 = 이 기물) 버프 창이 각각 뜸(초록 제목) | 연쇄 이동공격은 StatusEffect가 아니라 컴포넌트라 `ButtonInfo`에는 안 보임 |
| TC-TILE-006 | 아군이 `SummonMasteryCard` 사용 | 그 칸에 마우스 올림 | 스탯 창에 "다음 소환: 힘 +2, 체력 +2" | |
| TC-TILE-007 | 이동공격 범위(`MoveAttackRangeInfoSO`)가 설정된 기물(예: 전사) | 그 칸에 마우스 올림 | "이동공격 범위" 격자만 표시(중심 노랑 = 부딪힌 칸, 설명 문구 없음) | 설정 없는(중앙 1칸) 기물은 이 창 없음 |
| TC-TILE-008 | 창이 많은 기물(상태이상 3개 이상 + 다음 행동 카드) | 마우스 올림 | 칸 윗변부터 아래로 쌓이다가 호버된 손패 카드 높이만큼 내려가면 오른쪽에 새 줄. 모든 창 폭 동일 | |
| TC-TILE-009 | 보드 오른쪽 끝 칸·아래쪽 끝 칸의 기물 | 마우스 올림 | 오른쪽이 부족하면 칸 왼쪽에 배치, 아래로 넘치면 위로 밀려 화면 안에 표시 | |
| TC-TILE-010 | 휴식·상점·대화 이벤트 레벨 | 모닥불·상점·NPC 칸에 마우스 올림 → 모닥불 사용 후 다시 올림 | 모닥불: "휴식 지점" + 휴식(회복량)·강화(인원·힘) 안내, 사용 후 "이미 사용했습니다." / 상점: 구매·카드 제거 안내 / NPC: 이름 + "대화할 수 있습니다." 범위·상태 창 없음 | |
| TC-TILE-011 | 적 칸에 마우스를 올려 정보창이 뜬 상태 | 마우스를 둔 채 그 적을 다른 아군이 처치(또는 적이 이동) | 정보창이 사라지거나 새로 칸에 들어온 기물 기준으로 다시 뜸(죽은 기물 정보가 남지 않음) | `BoardTooltipHost.LateUpdate`가 칸 위 기물 변경을 감지 |
| TC-TILE-012 | 손패 카드를 들고 보드를 빠르게 훑음 / 기물 게이지 목록(`PieceGaugeItem`)에 마우스 올림 | 관찰 | 훑고 지나간 칸은 뜨지 않고 멈춘 칸에서만 0.3초 뒤 뜸 / 게이지 항목 호버 시 그 기물 칸에 정보창 | 손패 카드 정보창(섹션 19) 동작은 변화 없음 |
| TC-TILE-013 | 아군이 `WardZoneCard`·`PersistentShieldCard` 사용 | 그 칸에 마우스 올림 | "턴 종료 시 광역 피해 3"·"턴 종료 시 광역 회복 2" 창에 각각 `SurroundingRangeInfo` 범위 격자(중심 파랑 = 이 기물)가 함께 표시. 범위가 없는 턴 효과("턴 시작 시 방어막 4" 등)는 격자 없이 이름·남은 턴만 | 범위 격자는 턴 효과의 `effectRange`가 있고 대상이 범위형(`All*InRange`)일 때만. 발동 시 효과를 가진 기물의 위치가 중심 |
| TC-TILE-014 | 다음 행동이 `BossEmpowerCard`(힘 +3)인 보스(`L3-Colossus`), 다음 행동이 `EnemyAttackCard`인 적, 영구 가시가 걸린 적(White Knight 4, 다음 행동 `EnemyThornCard`) | 각 칸에 마우스 올림 | 보스: "다음 행동" 카드 이미지 바로 뒤에 "힘" 용어 창 / `EnemyAttackCard`("근처 적에게 N 피해"): 용어 창 없음 / White Knight 4: "가시" 상태 창만 있고 다음 행동 카드 뒤에 "가시" 용어 창은 없음 | 손패 카드 정보창과 같은 용어 규칙(`CardTooltipContent.BuildKeywords`). 이미 걸린 상태이상과 같은 용어는 생략 |
| TC-TILE-015 | 아군 A가 `StunningChargeCard` 사용(다음 이동공격 적중 시 1턴 기절) / A가 이 카드를 2장 사용 / 기절한 적 | A 칸·게이지 항목에 마우스 올림 / 기절한 적 칸에 마우스 올림 | "다음 이동공격 적중 시 1턴 기절" 버프 창 아래에 "기절" 용어 창(행동할 수 없습니다.)이 1개 뜸 / 버프 창은 2개, "기절" 용어 창은 1개 / 기절한 적은 "기절" 디버프 창만 있고 다음 행동(`StunnedCard`)의 "기절" 용어 창은 따로 뜨지 않음 | 버프가 거는 상태의 용어 창은 `BoardTooltipContent.AddInflictKeywords`(용어는 `CardTooltipContent.StatusTermOf`). 회귀: 예전엔 버프 창만 뜨고 "기절" 설명이 없었음 |
