# ChessRogueLike QA 테스트 케이스

이 문서는 `Assets/Scripts/` 전체(Board 16개 partial class, Piece/Card 핵심 엔진, `Cards/` 폴더 카드 66종, 매니저/UI/맵/상점/다이얼로그/유물 시스템)와 `Assets/LevelData.cs`, `Assets/CardsPanel.cs`, `Assets/SO/Jobs/*.asset`, `전사_소환사_카드목록.txt`를 근거로 작성된 QA 테스트 케이스 모음이다.

## 사용법

- **ID 규칙**: `TC-<섹션약어>-NNN` (예: `TC-CARD-014`)
- **표 컬럼**: `ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고`
- **비고란**: 코드 분석 중 발견된 버그성 이슈/설계 미비 사항은 관련 케이스의 비고란에 `⚠` 표시로 남긴다. 정상 동작 확인이 목적인 일반 케이스는 비고란을 비워둔다.
- 모든 케이스는 실제 스크립트의 정확한 수치·조건을 근거로 하며, 코드가 변경되면 함께 갱신되어야 한다.

## 목차

1. [세이브/로드 & 신규 게임](#1-세이브로드--신규-게임)
2. [보드 초기화 & 레벨 진입](#2-보드-초기화--레벨-진입)
3. [이동 & 이동공격](#3-이동--이동공격)
4. [피해/회복/보호막 처리](#4-피해회복보호막-처리)
5. [카드 엔진 공통 로직](#5-카드-엔진-공통-로직)
6. [개별 카드 기능](#6-개별-카드-기능)
7. [상태이상/버프 시스템](#7-상태이상버프-시스템)
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

## 알려진 이슈 인덱스

코드 분석 중 발견되었으며 아래 섹션의 개별 케이스 비고란에 표시된 항목들의 요약:

- 프리팹이 없어 정상 플레이로는 도달 불가능한 orphan 카드: `AreaAttackCard`, `AreaShieldCard`, `RecoverShieldCard`, `ThornCard`, `ZoneHealCard`, `ZoneShieldCard` (섹션 6)
- Warrior 보상 풀(`Warrior.asset`)에 스크립트/프리팹이 존재하지 않는 `FinalAttackCard` 참조 (섹션 16)
- `ThornCard` 툴팁 텍스트가 `statusDuration - 1`을 표시해 실제 지속시간(2턴)과 1턴 차이 남 (섹션 6.7)
- `StatusEffects.cs`의 `MovementDisabledEffect` 주석은 "게임플레이 미적용"이라 하지만 실제로는 Move 카드 차단 로직에 적용되어 있음 (섹션 7)
- `GameManager.TriggerDefeat`가 로그 출력만 하는 플레이스홀더로, 실제 패배 화면/흐름 없음 (섹션 15)
- 동시 전멸(마지막 아군·마지막 적 같은 틱에 사망) 시 승리/패배 판정 경쟁 상태 가능성 (섹션 15)
- `RangeInfoSODatabase.GetRangeInfoSO`는 조회 실패 시 에러 로그 없이 `null` 반환 (다른 Database류와 불일치) (섹션 3, 6)
- `ShopCanvas`의 `cardPrice`/`removePrice`/`relicPrice` 인스펙터 기본값이 0이고, `MainScene.unity`에서도 `removePrice`·`relicPrice`가 0이라 카드 제거와 유물 구매가 무료 (섹션 11)
- `PieceTargetPickerUI`/다이얼로그 보상 선택 등에 취소 버튼이 없어 일부 흐름은 되돌릴 수 없음 (섹션 11, 13)
- `Map.cs` 노드 생성 시 다음 층 고아 노드 방지 안전망이 있으나 반복 생성으로 회귀 검증 필요 (섹션 10)
- `SummonerPieceInfo`의 기물 이름이 `Summoner`에서 `SummonerAlly`로 바뀌어, 변경 전에 만든 세이브의 소환사 기물은 `PieceDatabase`에서 찾지 못해 스폰에 실패할 수 있음 (섹션 1)
- `GraveAttackCard`의 1타로 적을 처치하면 2타는 빈 칸에 헛스윙하는데도 무덤 1이 소모됨 (섹션 6.10)
- 현재 무덤 수를 보여주는 UI가 없음. 정보창에도 표시되지 않고, 늘어날 때 "무덤 +N" 텍스트만 뜸 (섹션 18)
- 적이 죽으면 다른 적 전원에게도 "무덤 +1" 텍스트가 뜸. 적은 무덤을 쓰는 카드가 없어 의미 없는 연출 (섹션 18)
- 소환 카드 설명에 소환수의 스폰 시 효과(`autoally`·`tauntAutoAlly`의 방어도 2)가 표시되지 않음 (섹션 6.4)
- `autoally.prefab`이 `onSpawnCards`의 옛 이름(`onSummonCards`)으로 저장되어 있어 `FormerlySerializedAs`에 의존함 (섹션 17)

---

## 1. 세이브/로드 & 신규 게임

**관련 스크립트**: `DataManager.cs`, `MainMenuCanvas.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-SAVE-001 | 세이브 파일 없음(최초 실행) | 메인메뉴에서 "기본" 기물로 새 게임 시작 | `basicPieceinfo` 기준 로스터 1기, 기본 덱 9장(AttackCard×3, DefenseCard×2, SummonCard×1, MoveCard×2, StunCard×1) 생성 | |
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

---

## 3. 이동 & 이동공격

**관련 스크립트**: `Board.Combat.cs`, `Board.Occupancy.cs`, `Board.RangeUI.cs`, `Board.HoverRange.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-MOVE-001 | 빈 칸으로 이동 가능한 카드 보유 | 빈 칸으로 이동 | 점유 정보가 애니메이션 시작 전 즉시 갱신, 이동 애니메이션 재생 | |
| TC-MOVE-002 | 목적지에 아군 기물 존재 | 해당 칸으로 이동 시도 | 이동 실패(시전자는 원래 칸에 남음), 아무 효과 없음 | |
| TC-MOVE-003 | 목적지에 적 기물 존재, `noMoveAttack=false`인 카드 | 이동 시도 | `MoveAttack` 발동(충돌 공격) | |
| TC-MOVE-004 | 목적지에 적 존재, `SafeMoveCard`(`noMoveAttack=true`) 사용 | 이동 시도 | 이동 실패로 처리(공격 없음), 일반 막힌 이동과 동일 | |
| TC-MOVE-005 | 이동공격 목표 주변에 착지 가능한 인접 칸이 하나도 없음(사방 점유) | 이동공격 시도 | 이동공격 전체가 취소되어 실패한 이동으로 처리 | |
| TC-MOVE-006 | 이동공격으로 적 처치 | 이동공격 실행 | 공격자가 적의 원래 칸(`impactPos`)까지 전진 | |
| TC-MOVE-007 | 이동공격으로 적이 생존 | 이동공격 실행 | 공격자가 인접 착지 칸(`adjacentPos`)에 멈춤 | |
| TC-MOVE-008 | 대상이 `ThornEffect` 보유 | 이동공격으로 해당 대상 타격 | 대상을 죽여도 공격자가 반격 피해(`OnReceiveMoveAttack`)를 받아 사망할 수 있음(최종 위치는 `finalAttackerPos` 기준) | |
| TC-MOVE-009 | `ChargeCard`(`shieldOnMoveAttack=true`, 3 보호막)로 실제 충돌 발생 | 이동공격 실행 | 캐스터가 보호막 3 획득 | |
| TC-MOVE-010 | `ChargeCard`로 빈 칸 이동(충돌 없음) | 이동 실행 | 보호막 획득 없음 | ⚠ 반드시 충돌해야만 효과 발동함을 확인 |
| TC-MOVE-011 | `MoveAttackRangeInfoSO`가 스플래시(다중 오프셋)인 카드로 여러 적 동시 타격 | 이동공격 실행 | `isAreaAttack=true`, 스플래시 대상 전원에게 개별 피해, 사망 기물은 점유 정보 즉시 제거 | |
| TC-MOVE-012 | `ChainMoveAttackBuff` 부착된 기물이 스플래시 이동공격 수행 | 이동공격 실행(다중 타격) | 체인 발동 안 함(`hitMultipleTargets=true`이므로 차단) | |
| TC-MOVE-013 | `ChainMoveAttackBuff` 부착된 기물이 단일 대상만 타격하는 이동공격 수행 | 이동공격 실행 | 최종 위치의 이동범위 내 다른 적 1기를 자동으로 추가 타격(동일 피해량, 재귀 체인 없음) | |
| TC-MOVE-014 | `BloodChargeCard`(`healOnHit=true`)로 스플래시 이동공격 2명 적중 | 이동공격 실행 | 자힐량 = 주 타겟 + 스플래시 대상별 실제 피해(취약 보정 포함)의 합. 취약 대상이 없으면 `dmg × (1+스플래시 수)`와 같음 | 취약 포함 케이스는 TC-STATUS-023 |
| TC-MOVE-015 | `LethalChargeCard`로 이동공격 킬 성공 | 이동공격 실행 | `onKillEffect`(에너지 +2) 발동 | |
| TC-MOVE-016 | `LethalChargeCard`로 이동공격했지만 대상 생존 | 이동공격 실행 | `onKillEffect` 발동 안 함 | |
| TC-MOVE-017 | 아무 기물도 없는 빈 칸에 단일 대상 공격 카드 사용 | 카드 사용 | 캐스터 애니메이션만 재생, 효과 없음, 카드는 정상 소모(에너지 차감/버림) | |
| TC-MOVE-018 | 대각선 방향 이동공격 | 이동공격 실행 | `GetAdjacentLocation`이 직선 우선이 아닌 두 개의 직교 인접 칸 중에서 착지 위치 결정 | |
| TC-MOVE-019 | 소환 카드로 대상 칸 및 주변이 모두 점유된 상태에서 소환 시도 | 소환 카드 사용 | 범위 내 빈 칸을 찾지 못하면 소환 없이 카드만 소모 | |
| TC-MOVE-020 | 이동 중(애니메이션 재생 중) 다른 입력 시도 | 애니메이션 진행 중 보드 클릭 | 논리적 점유는 이미 갱신되어 있어 다음 액션이 올바른 위치 기준으로 처리(시각적 지연과 무관) | |

---

## 4. 피해/회복/보호막 처리

**관련 스크립트**: `Piece.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-DMG-001 | 보호막 5, 체력 10 | 피해 3 적용 | 보호막이 먼저 흡수(보호막 2 남음), 체력 불변 | |
| TC-DMG-002 | 보호막 3, 체력 10 | 피해 7 적용 | 보호막 전부 소모 후 초과분 4가 체력에 적용(체력 6) | |
| TC-DMG-003 | 보호막 0 | 보호막 카드 2회 연속 사용(합 8) | 보호막 상한 없이 그대로 누적(8) | |
| TC-DMG-004 | 체력 5, 최대체력 10 | 회복량 20 카드 사용 | 실제 회복은 5만 적용(최대치 클램프), 표시 텍스트도 5 | |
| TC-DMG-005 | 일반 공격(`isAttack=true`)이 teamID==0 기물에 적중 | 공격 실행 | `Board.TriggerRelicsOnHit`가 호출되어 On-Hit 유물 발동 | |
| TC-DMG-006 | DoT(자기 피해, `isAttack=false`)로 teamID==0 기물이 피해를 입음 | 턴 종료 DoT 발동 | On-Hit 유물이 발동하지 않음 | |
| TC-DMG-007 | 적(teamID==1) 기물이 공격으로 피해를 입음 | 공격 실행 | On-Hit 유물이 발동하지 않음(코드상 teamID==0 조건) | |
| TC-DMG-008 | `WeakenEffect`의 `reducedDamage`가 현재 `colDamage`보다 큼(예: colDamage 2, reduce 5) | 디버프 적용 | 실제 감소량은 2로 클램프되어 colDamage가 0 미만으로 내려가지 않음, 제거 시 정확히 2만 복구 | |
| TC-DMG-009 | 일반 `AddColDamage(음수, permanent 무관)` 경로(Weaken이 아닌 다른 효과) | 큰 음수값 적용 | ⚠ 별도 하한 클램프가 없어 `colDamage`가 음수가 될 수 있는지 확인 | |
| TC-DMG-010 | 턴 종료 시점 DoT 적용 후 체력이 0 이하가 됨 | `TurnEnd` 처리 | 사망 판정은 상태이상 처리 직후, 턴 종료 시점에 확정(즉시 사망이 아님) | |
| TC-DMG-011 | 이미 `isDeathScheduled`인 기물에 추가 피해/사망 트리거 발생 | 중복 사망 처리 유발 | `DeathCor`가 중복 실행되지 않음(idempotent) | |

---

## 5. 카드 엔진 공통 로직

**관련 스크립트**: `Card.cs`, `Board.CardEffect.cs`, `CardCanvas.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-ENGINE-001 | `dragDropTarget=Ally`인 카드를 적 기물 위로 드롭 | 드롭 실행 | `IsValidDragTarget` 실패로 사용 거부, `AnnouncementUI`에 실패 사유 표시 | |
| TC-ENGINE-002 | `dragDropTarget=Self`인 카드 | 드래그 없이 즉시 사용 시도 | 타겟팅 단계 없이 캐스터 본인에게 즉시 적용 | |
| TC-ENGINE-003 | 이동(Move) 효과 뒤에 공격/버프 효과가 이어지는 카드(`MoveandAttackCard`, `MoveAndDrawCard` 등) | 카드 사용 | 두 번째 효과가 이동 "이전" 위치가 아닌 "이후" 위치 기준으로 처리 | |
| TC-ENGINE-004 | 캐스터가 `MovementDisabledEffect` 보유, Move 포함 카드 사용 | 카드 사용 시도 | 카드의 남은 효과 큐 전체가 취소, "이동 불가 상태입니다" 안내 표시 | |
| TC-ENGINE-005 | `CostDuration.OneUse`로 비용이 임시 변경된 카드 | 해당 비용으로 카드 사용 | 사용 즉시 `originalCost`로 원복 | |
| TC-ENGINE-006 | `CostDuration.ThisTurnOnly`로 비용 변경된 카드가 핸드/버림/덱 중 한 곳에 있음 | 아군 턴 종료(`AllyTurnEnd`) | `RestoreThisTurnCosts()`가 세 존을 모두 스캔해 원래 비용으로 복원 | |
| TC-ENGINE-007 | `WarmUpDamageCard`(초기 Cost 3, 자가 `ReduceCost -1`) | 3회 연속 사용(3→2→1→0) | 정확히 몇 번째 사용에서 `ShouldExileOnUse()`가 true가 되어 추방되는지 확인 | ⚠ 카드 설명 대비 실제 소모 시점 경계 검증 필요 |
| TC-ENGINE-008 | 에너지 자가 감소 카드(`WarmUpDamageCard` 등) 사용 | 카드 사용 | 에너지 차감은 `ExecuteEffect`에서 발생 시점에 1회만 적용되어, 자가 비용 감소가 현재 시전에 소급 적용되지 않음 | |
| TC-ENGINE-009 | 아군 3명(소환수 제외) 생존 | 최대 에너지 확인 | `maxenergy = baseMaxEnergy + max(0, 비소환 아군수-1)`로 계산(아군 사망/합류 시 실시간 재계산) | |
| TC-ENGINE-010 | 아군 턴 시작 | 카드 드로우 | 살아있는 모든 아군이 각 5장씩 드로우, 활성 기물은 애니메이션 포함, 나머지는 데이터만 즉시 반영 | |
| TC-ENGINE-011 | 덱이 빈 상태에서 드로우 필요 | 드로우 처리 | 버림더미를 셔플하여 덱으로 재구성 후 계속 드로우, 둘 다 비면 조기 종료 | |
| TC-ENGINE-012 | 아군 턴 종료 | 핸드 상태 확인 | 사용하지 않은 카드 전부 버림더미로 이동(다음 턴으로 이월되지 않음) | |
| TC-ENGINE-013 | `pieceSelectCount=1`, `excludeCasterFromPieceSelection=true`인 카드, 아군이 캐스터 1명뿐 | 카드 사용 | 대상 0명으로 자동 클램프, 카드가 소모되지만 효과는 발동하지 않음 | ⚠ EmpowerAllyCard 등에서 확인 |
| TC-ENGINE-014 | 카드가 보드 클릭 중(`isCardEffecting=true`)인 상태 | 다른 아군 기물로 활성 캐릭터 전환 시도 | "카드 사용 중에는 기물을 전환할 수 없습니다" 안내와 함께 전환 거부 | |
| TC-ENGINE-015 | 카드를 핸드에서 드래그해 보드에 뗐다가 다시 손패 영역 위로 되돌림 | 드래그 취소 | `RevertNowUsingCardToHeld` 동작, 단 `effectApplied=true` 시점 이후에는 취소 불가 | |
| TC-ENGINE-016 | `AllPiecesInRange`(`ZoneAttackCard` 등) AoE 카드 | 아군과 적이 모두 있는 위치에 시전 | 팀 무관하게 범위 내 모든 기물에 적용(아군도 피해/효과 받음) | |
| TC-ENGINE-017 | `AreaTargetMode.Fixed`인 AoE 카드 | 마우스 위치를 캐스터가 아닌 다른 곳에 두고 사용 | 항상 캐스터 중심으로 재계산(클릭 위치 무시) | |
| TC-ENGINE-018 | `AreaTargetMode.Directional4/8`인 카드 | 캐스터 기준 각 방향으로 마우스 이동 | 회전된 패턴이 4/8방향으로 정확히 스냅되어 미리보기/적용 | |
| TC-ENGINE-019 | `requiresCasterNotMoved=true`인 카드(`DefensiveStanceCard`), 캐스터가 이미 이번 턴 이동함 | 카드 사용 시도 | "이동 전에만 사용할 수 있습니다" 안내와 함께 사용 거부 | |
| TC-ENGINE-020 | `blocksMovementAfterUse=true`인 카드 사용(이동 전) | 카드 사용 후 이동 카드 사용 시도 | 이동이 차단됨(해당 턴 내) | |
| TC-ENGINE-021 | `effects[0].effectRange`가 비어있는 커스텀 카드(데이터 오류 상황) | 카드 `Awake()` 실행 | 인덱싱 시 예외 발생 여부 확인(리그레션 방지용 데이터 무결성 점검) | |
| TC-ENGINE-022 | `Card.IsSelectable()` 확인 | 카드가 `CardCanvas.instance.cards`에서 이미 제거된 상태(다른 코루틴 처리 중)에 클릭 | 실제 리스트 멤버십 기준으로 선택 불가 처리(캐시된 플래그로 인한 오탐 없음) | |
| TC-ENGINE-023 | 카드 사용 완료 애니메이션이 여러 장 동시에 대기 중(`pendingCardFlights`) | 연속으로 여러 카드를 빠르게 사용 | 각 카드가 올바른 파일-존(버림/추방)으로, 올바른 순서로 날아가 애니메이션이 꼬이지 않음 | ⚠ FIFO 가정에 의존하므로 다중 카드 동시 처리 스트레스 테스트 필요 |
| TC-ENGINE-024 | 손패의 카드를 자신의 손패 영역 위로 빠르게 드래그 인/아웃 반복 | 반복 드래그 | 카드가 의도치 않게 중복 사용되지 않음 | ⚠ `Card.MouseDrag`의 손패-호버 자동사용 경로 재현 테스트 |
| TC-ENGINE-025 | 카드 선택 패널(`ShowCardSelectionPanel`)을 대상 풀이 0장인 상태로 오픈(예: 빈 버림더미에서 카드 선택) | 패널 오픈 | 빈 목록으로 즉시 자동 확정(교착 없음) | |
| TC-ENGINE-026 | 두 번째 효과가 `targeting`·`self`인 아군 카드(`SummonGrowthCard`: 턴 효과(다음 소환 콜대미지) → 턴 효과(다음 소환 체력)) | 카드 사용 | 두 번째 효과가 클릭 없이 카드를 쓴 기물에게 바로 적용되고 카드가 정상 종료(교착 없음) | 후속 효과는 카드를 쓴 기물을 현재 위치에서 다시 선택한 것처럼 처리 |
| TC-ENGINE-027 | 두 번째 효과가 `command`인 아군 카드(`MoveandAttackCard`) — 빈 칸 이동 / 아군 충돌로 이동 실패 / 이동공격 후 적 생존 / 이동공격으로 적 처치 | 각각 카드 사용 | 두 번째 효과의 사거리가 시전자의 실제 위치(이동 칸 / 원래 칸 / 인접 칸 / 적이 있던 칸) 기준으로 표시되고 클릭으로 실행 | |
| TC-ENGINE-028 | 효과가 여러 개인 카드의 앞 효과로 시전자가 사망(자해 등) | 카드 사용 | 남은 효과는 건너뛰고 카드가 정상 종료 | |

---

## 6. 개별 카드 기능

**관련 스크립트**: `Assets/Scripts/Cards/*.cs` (66종), `Assets/Prefab/Cards/*.prefab`

### 6.1 기본 공격형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-001 | `AttackCard`(Cost 1, dmg 3, LowestHP) 보유, 사거리 내 적 2기(체력 다름) | 카드 사용, 대상 자동/클릭 선택 | 사거리 내 체력이 가장 낮은 적에게 3 피해 | |
| TC-CARD-002 | `ColDamageAttackCard`(Cost 1, `useColDamageAsDmg=true`) 사용, 캐스터 colDamage 7 | 이동범위 내 최근접 적에게 사용 | 피해량 = colDamage(7)로 적용(카드 자체 dmg 무시) | |
| TC-CARD-003 | `MagicAttackCard`(Cost 3, dmg 10, `ignoreCasterColDamageBonus=true`), 캐스터 colDamage 버프 보유 | 임의 기물(`AnyPiece`)에 사용 | 버프 무관하게 정확히 10 고정 피해 | |
| TC-CARD-004 | `HeavyAttackCard`(Cost 2, dmg 6 적/2 자해) | LowestHP 적에게 사용, 대상 존재 | 적 6 피해 + 자신 2 자해가 함께 발생 | |
| TC-CARD-005 | `HeavyAttackCard`를 대상 없는 빈 칸/사거리 내 적 없음 상황에서 사용 | 카드 사용 | 적 피해는 미발생(헛스윙)하지만 자해 2는 대상과 무관하게 별도 효과로 발생하는지 확인 | ⚠ 두 효과가 독립적으로 처리되어 실패 조건이 연동되지 않음 |
| TC-CARD-006 | `DoubleAttackCard`(Cost 3, dmg 4, hitCount=2) | 체력 4 이하 적에게 사용 | 1타에 적이 죽고, 2타는 동일 위치 재타격 시 헛스윙 처리(에러 없음) | |
| TC-CARD-007 | `DoubleAttackCard` | 체력 20 적에게 사용 | 동일 대상에게 4 피해 2회(총 8) 적용 | |
| TC-CARD-008 | `ExecutionerCard`(Cost 1, dmg 3)로 적을 처치 | 처치 성공 | `onKillEffect`로 자신의 `colDamageBonus` 영구 +1 (다음 전투까지 유지) | |
| TC-CARD-009 | `ExecutionerCard`로 3연속 처치 | 3회 처치 | `colDamageBonus`가 누적되어 +3 | |
| TC-CARD-010 | `DirectionalAttackCard`(Cost 2, dmg 5, Directional4) | 4방향 중 특정 방향에 적 배치 후 사용 | 마우스/클릭 방향으로 회전된 직선 범위에만 적중 | |
| TC-CARD-011 | `ZoneAttackCard`(Cost 2, dmg 2, MouseCentered, `AllPiecesInRange`) | 아군·적 혼재 지역에 시전 | 팀 무관 전원 2 피해(아군도 피해 받음) | |
| TC-CARD-012 | `FetchAttackCard`(Cost 0) | 사용 | `AttackCard` 1장이 즉시 손패에 추가 | |

### 6.2 이동/이동공격형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-013 | `MoveCard`(Cost 2) | 이동범위 내 빈 칸으로 이동 | 정상 이동, 이동공격 가능(적 있으면 충돌) | |
| TC-CARD-014 | `SafeMoveCard`(Cost 1, `noMoveAttack=true`) | 적이 있는 칸으로 이동 시도 | 이동 실패(공격 없이 막힘) | |
| TC-CARD-015 | `ChargeCard`(Cost 2, `moveAttackShieldAmount=3`) | 이동공격 충돌 발생 | 캐스터 보호막 +3 | |
| TC-CARD-016 | `BloodChargeCard`(Cost 2, `healOnHit=true`) | 이동공격으로 적 1기 명중(스플래시 없음) | 자힐 = 가한 피해량과 동일 | |
| TC-CARD-017 | `LethalChargeCard`(Cost 3)로 이동공격 킬 | 처치 성공 | `RestoreEnergy 2` 발동(에너지 +2, 최대치 클램프) | |
| TC-CARD-018 | `MoveandAttackCard`(Cost 3, 이동 후 dmg 3 LowestHP) | 이동 후 사거리 내 적 존재 | 이동 완료 후 새 위치 기준으로 LowestHP 적에게 3 피해 | |
| TC-CARD-019 | `MoveandAttackCard`로 이동 자체가 이동공격이 되어 적을 처치한 경우 | 이동 실행(충돌로 처치) | 이어지는 Damage 효과는 새 위치 기준 LowestHP 재탐색(자동 스킵되지 않음) | |
| TC-CARD-020 | `MoveAndDrawCard`(Cost 2) | 이동 후 | 이동 완료 후 카드 1장 드로우 | |

### 6.3 광역(AoE) & 지속형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-021 | `AreaAttackCard` | 정상 플레이에서 획득 가능 여부 확인 | 프리팹이 없어 상점/보상/직업 풀 어디서도 등장하지 않음(Boss류 프리팹에 컴포넌트로만 부착) | ⚠ orphan/의도된 내부 전용 카드 여부 기획 확인 필요 |
| TC-CARD-022 | `LifeDrainCard`(Cost 2, dmg 2, `healOnHit=true`, AoE 적) | 적 3기가 범위 내 있는 곳에 시전 | 각 적 2 피해, 자힐 = 적중한 적마다 실제 입힌 피해의 합(합산 1회 반영). 취약 대상이 없으면 적 수 × 2 | 취약 포함 케이스는 TC-STATUS-023 |
| TC-CARD-023 | `ZoneHealCard`(dmg 5, 팀 무관 회복) | 정상 플레이에서 획득 가능 여부 확인 | 프리팹/보상풀/직업풀 어디에도 연결되지 않아 도달 불가 | ⚠ orphan |
| TC-CARD-024 | `ZoneShieldCard`(dmg 3, 팀 무관 보호막) | 정상 플레이에서 획득 가능 여부 확인 | 프리팹/보상풀 미연결로 도달 불가 | ⚠ orphan |
| TC-CARD-025 | `AreaHealCard`(Cost 2, dmg 5, `SurroundingRangeInfo`=캐스터 주변 8칸, 자신 제외) | 캐스터 주변에 아군 여러 명 배치 후 사용 | 주변 8칸 내 아군 전원 5 회복, 캐스터 본인은 제외 | |
| TC-CARD-026 | `AreaShieldCard`(Cost 1, dmg 3, `AllAlliesInRange`, Fixed) | 정상 플레이에서 획득 가능 여부 확인 | 프리팹 없음 — 스크립트만 존재, 상점/보상/직업풀 어디에도 연결 안 됨 | ⚠ orphan |
| TC-CARD-027 | `FlameThrowingCard`(Cost 1, `OwnTurnEnd`, 3턴간 매 턴 종료 시 AoE 2 피해) | 사용 후 3번의 자기 턴 종료 경과 | 매 턴 종료마다 적 대상 2 AoE 피해가 정확히 3회 발생 후 자동 만료 | |
| TC-CARD-028 | `FlameThrowingCard` 사용 후 캐스터가 사망 | 캐스터 사망 이후 턴 진행 | 죽은 기물의 `TurnEffect`가 더 이상 발동하지 않고 정리되는지 확인 | |

### 6.4 소환형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-029 | `SummonCard`(Cost 2, autoally: HP5/colDmg5) | 빈 칸에 사용 | 근처 빈 칸(BFS, 직교 우선)에 AutoAlly 소환, 팀0·`isSummon=true`. 소환 직후 스폰 시 효과(`DefenseCard`)로 방어도 2 획득 | ⚠ 카드 설명("autoally을(를) 소환합니다.")에는 방어도 2가 표시되지 않음 |
| TC-CARD-030 | `GreaterSummonCard`(Cost 4, HP9/colDmg7) | 빈 칸에 사용 | GreaterAutoAlly 소환, 스탯 확인 | |
| TC-CARD-078 | `TauntSummonCard`(Cost 2, tauntAutoAlly: HP5/colDmg5, 소멸) | 빈 칸에 사용 | tauntAutoAlly(비숍) 소환 → onSpawnCards 순서대로 방어도 2(`DefenseCard`) → 영구 도발(`TauntCard`) 적용, 카드는 버림 더미가 아니라 소멸 | ⚠ 카드 설명에는 방어도 2가 표시되지 않음 |
| TC-CARD-031 | `SummonMasteryCard`(Cost 2, +2 colDmg pending, +2 maxHp pending) 사용 후 바로 `SummonCard` 사용(동일 캐스터) | 순서대로 사용 | 소환된 기물의 colDamage/maxHp/hp에 각각 +2 보너스 적용, pending 값 소진(0으로 리셋) | |
| TC-CARD-032 | `SummonMasteryCard`를 2회 연속 사용 후 소환 | 2회 사용 → 소환 | 보너스가 누적(각 +4)되어 적용, 상한 없음 | |
| TC-CARD-033 | `SummonMasteryCard` 사용 후 그 전투에서 끝까지 소환을 하지 않음 | 전투 종료까지 관찰 | pending 값이 만료되지 않고 해당 기물에 계속 남아있음(다음 소환 시 뒤늦게 적용될 수 있음) | ⚠ StatusEffect가 아니므로 시각적 만료 표시 없이 무기한 잔존 |
| TC-CARD-079 | `SummonGrowthCard`(Cost 3, Self, 영구 턴 효과 2개) 보유 | 카드 사용 후 정보창 확인 | 정보창에 "턴 종료 시 다음 소환 콜대미지 +1", "턴 종료 시 다음 소환 체력 +3" 두 버프가 턴 수 없이 표시. 사용 시점에는 다음 소환 보너스가 늘지 않음 | |
| TC-CARD-080 | `SummonGrowthCard` 사용 | 아군 턴 종료를 2회 거친 뒤 `SummonCard` 사용 | 턴 종료마다 "다음 소환 강화 +1", "다음 소환 체력 +3" 텍스트. 소환된 autoally는 colDamage 5+2=7, 최대·현재 체력 5+6=11, 시전자의 다음 소환 보너스는 0으로 리셋 | |
| TC-CARD-081 | TC-CARD-080 직후(소환으로 보너스 소모) | 아군 턴 종료 1회 후 다시 소환 | 턴 효과는 영구라 그대로 남아 있고, 소모 후 다음 턴 종료부터 다시 +1/+3씩 쌓임 | |
| TC-CARD-082 | `SummonGrowthCard`를 같은 기물이 2회 사용 | 아군 턴 종료 1회 | 정보창에 턴 효과 4개, 턴 종료당 보너스 +2/+6 | 중첩 상한 없음 |
| TC-CARD-083 | `SummonGrowthCard` 사용 후 턴 종료 1회(+1/+3), 이어서 `SummonMasteryCard`(+2/+2) 사용 | `SummonCard` 사용 | 두 보너스가 합산되어 소환수에 colDamage +3, 체력 +5 적용 | 시전자가 사망하면 턴 효과도 함께 사라져 더 이상 쌓이지 않음 |
| TC-CARD-034 | `EmpowerAllyCard`(Cost 2, pieceSelectCount=1, 캐스터 제외) 아군 2명 이상 | 특정 아군 클릭 선택 | 선택된 아군에게 `colDamageBonus` 영구 +2 | |
| TC-CARD-035 | `EmpowerAllyCard`를 아군이 캐스터 1명뿐인 상태에서 사용 | 카드 사용 | 선택 대상 0명으로 클램프, 카드는 소모되나 효과 없음 | ⚠ TC-ENGINE-013과 동일 케이스 |
| TC-CARD-036 | `SquadTrainingCard`(Cost 2, pieceSelectCount=2, 캐스터 포함 가능) | 캐스터 포함 2명 선택 | 선택된 2명 모두 `colDamageBonus` 영구 +1 | |
| TC-CARD-037 | `SquadTrainingCard`를 아군 1명뿐인 상태에서 사용 | 카드 사용 | 선택 가능 인원이 1명으로 클램프되어 그 1명만 적용 | |
| TC-CARD-070 | 소환될 기물 프리팹의 `onSpawnCards`(스폰 시 효과)에 `DefenseCard`(self 실드)·`EnemyAttackCard`(NearestEnemy 공격) 연결 | `SummonCard`로 소환 | 소환 연출 → 소환된 기물이 시전자로 실드 → 가장 가까운 적 공격 순서로 실행. 카드 종료 후 선택/사거리 표시 정상 해제, 에너지 1회만 차감 | 소환 시 효과는 적 카드 규칙으로 자동 대상 결정(플레이어 입력 없음) |
| TC-CARD-071 | 소환 효과 뒤에 효과가 더 있는 카드 + 소환 시 효과를 가진 기물 | 카드 사용 | 소환 → 소환 시 효과 → 카드의 나머지 효과 순서로 실행, 나머지 효과의 시전자는 원래 카드 시전자 | |
| TC-CARD-072 | 적 AutoPiece의 `actionCards`에 user=Enemy 소환 카드, 소환될 기물에 소환 시 효과 연결 | 적 턴 진행 | 소환 시 효과가 소환된 기물 기준으로 실행되고 적 턴이 멈추지 않고 진행 | |
| TC-CARD-073 | 소환 시 효과 중 앞 효과가 자해로 소환된 기물을 처치 | 소환 | 남은 소환 시 효과는 스킵되고 카드는 정상 종료 | |
| TC-CARD-074 | 소환 시 효과에 `graveCost`가 있는 효과 | 소환 | 소환된 기물의 무덤(0부터 시작) 기준으로 판정 — 부족하면 그 효과만 스킵 | |
| TC-CARD-075 | 전투 레벨에서 아군 프리팹과 레벨 배치 적 프리팹의 `onSpawnCards`에 `DefenseCard` 연결, `ShieldRelic` 보유 | 전투 진입 | 전투 시작 시 배치 순서대로(아군 → 레벨 배치 기물) 스폰 시 효과가 실행된 뒤, 아군마다 `ShieldRelic` 실드가 적용. 첫 턴 정상 진행, 활성 기물 전환이 막히지 않음 | 스폰 시 효과와 전투 시작 유물이 같은 효과 큐에서 함께 처리됨 |
| TC-CARD-076 | `onSpawnCards`가 연결된 기물이 이벤트 레벨(휴식/상점/대화)에 스폰 | 이벤트 레벨 진입 | 스폰 시 효과가 발동하지 않음 | 유물 전투 시작 효과와 동일하게 전투 레벨에서만 |
| TC-CARD-077 | 전투 레벨에서 대화 선택지로 `onSpawnCards`가 연결된 기물이 합류(`SpawnPiece`) | 선택지 선택 | 합류 즉시 스폰 시 효과가 실행 | |

### 6.5 자가강화/자원형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-038 | `ColDamageUpCard`(Cost 1, `ColDamageUp +1`, 비-Base) | 사용 후 전투 승리 | 해당 전투에서만 +1 적용, 다음 전투에는 유지되지 않음 | |
| TC-CARD-039 | `TempColDamageUpCard`(Cost 0, `StrengthenEffect` duration 1, power 2) | 사용 | 실제 `StatusEffect`로 등록되어 1턴 뒤 자동 만료, Cleanse/Dispel 대상이 됨 | |
| TC-CARD-040 | `ShieldBonusUpCard`(Cost 1, `ShieldBonusUp +1`, 비-Base) | 사용 후 전투 승리 | 해당 전투에서만 적용, 영구 저장 안 됨 | |
| TC-CARD-041 | `WarmUpDamageCard`(Cost 3→2→1→0) | 1회 사용 | colDamage +3, Cost가 2로 감소, 아직 추방 안 됨 | |
| TC-CARD-042 | `WarmUpDamageCard` | Cost 0 상태에서 사용 | 사용 후(또는 사용 시점) `ShouldExileOnUse()`가 true가 되어 손패에서 추방 | |
| TC-CARD-043 | `DrawCard`(Cost 1) | 사용 | 카드 2장 드로우(효과 2개가 각각 Draw 1씩) | |

### 6.6 방어형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-044 | `DefenseCard`(Cost 1, 자가 보호막 2) | 사용 | 보호막 +2 | |
| TC-CARD-045 | `HeavyShieldCard`(Cost 2, 보호막 8, `exileOnUse=true`) | 사용 | 보호막 +8, 카드는 버림더미가 아닌 추방더미로 이동 | |
| TC-CARD-046 | `DefensiveStanceCard`(Cost 1, 보호막5 + 자가 `MovementDisabledEffect` 2턴, `requiresCasterNotMoved`, `blocksMovementAfterUse`) | 이동 전 사용 | 보호막 +5, 2턴간 이동 불가 디버프 자가 적용, 해당 턴 이동도 차단 | |
| TC-CARD-047 | `DefensiveStanceCard`를 캐스터가 이미 이동한 뒤 사용 시도 | 사용 시도 | "이동 전에만 사용할 수 있습니다" 거부 | |
| TC-CARD-048 | `PersistentShieldCard`(Cost 2, 즉시 보호막4 + 다음 턴 시작 보호막4) | 사용 후 다음 자기 턴 시작까지 관찰 | 즉시 4, 다음 턴 시작 시 추가 4(합계 8) | |
| TC-CARD-049 | `RecoverShieldCard`(보호막4 + 버림더미에서 1장 덱으로 복귀) | 정상 플레이에서 획득 가능 여부 확인 | 프리팹 없음, 도달 불가 | ⚠ orphan |
| TC-CARD-050 | `SacrificeShieldCard`(Cost 1, 보호막6, 핸드 1장 버리기 선택) | 핸드에 카드 1장 이상 있을 때 사용 | 보호막 +6, 선택 패널에서 1장 버림 | |
| TC-CARD-051 | `SacrificeShieldCard`를 핸드가 이 카드 1장뿐인 상태에서 사용 | 사용 | 버릴 카드가 0장인 선택 패널 처리(즉시 확정 또는 예외 없는 스킵) 확인 | ⚠ 빈 핸드 경계 케이스 |
| TC-CARD-052 | `ShieldCycleCard`(Cost 1, 보호막3, 핸드 랜덤 1장 덱 맨 위로) | 핸드에 카드 여러 장 있을 때 사용 | 보호막 +3, 무작위 1장이 덱 최상단으로 이동(다음 드로우에서 확정 등장) | |

### 6.7 디버프/CC형

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-053 | `StunCard`(Cost 1, LowestHP 적에게 Stun 1턴) | 사용 | 대상이 다음 자기 행동을 스킵(플레이어: 카드 사용 불가 / 적: `StunnedCard`로 대체) | |
| TC-CARD-054 | `ImmobilizeCard`(Cost 1, `MovementDisabledEffect` 2턴, `noRangeLimit=true`) | 사거리 밖 먼 적에게 사용 | 거리 제한 없이 적용 성공, 2턴간 이동 카드 사용 불가 | |
| TC-CARD-055 | `PoisonTestCard`(테스트 전용 카드, Poison 2턴/2뎀, `noRangeLimit=true`) | 사용 | 정상 동작하나, 정식 카드 채택 여부는 플레이테스트 후 결정 예정임을 인지 | ⚠ 코드 주석상 임시/검증용 카드 |
| TC-CARD-056 | `ThornCard`(자가 Thorn, 코드상 duration 2 / power 3) | 카드 설명 텍스트와 실제 지속시간 비교 | 설명 텍스트는 `duration-1`(1턴)로 표기되지만 실제로는 2턴 지속 | ⚠ 툴팁-실제값 불일치, 또한 프리팹 없어 정상 플레이 도달 불가(orphan) |
| TC-CARD-057 | `CleanseCard`(Cost 1, 자가 디버프 전체 제거) | 자신이 Poison+Weaken 동시 보유 시 사용 | 두 디버프 모두 제거, 버프는 영향 없음 | |
| TC-CARD-058 | `DispelCard`(Cost 1, 대상 적 버프 전체 제거, `noRangeLimit=true`) | 적이 Strengthen 보유 시 사용 | 해당 버프 제거(디버프는 영향 없음) | |
| TC-CARD-059 | `DispelCard`를 `ChainMoveAttackBuff`를 보유한 적에게 사용 | 사용 | `ChainMoveAttackBuff`는 `StatusEffect`가 아니므로 제거되지 않음 | ⚠ 디스펠 불가 버프 |
| TC-CARD-069 | `VulnerableTestCard`(Cost 1, 적 대상, `noRangeLimit=true`, Vulnerable 2턴/+1) | 사거리 밖 적에게 사용 | 거리 제한 없이 대상에게 `취약 (+1)` 부여, 디버프 텍스트·파티클 재생. 카드 설명은 "적을 선택해 2턴간 취약(1)을 부여합니다. 공격으로 받는 피해가 1 증가합니다." | ⚠ 검증 전용 카드. 소환사 보상 풀(`Summoner.asset`)에 임시 포함 — 소환사 기물의 전투 보상으로 획득 |

### 6.8 특수/체인

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-060 | `ChainMoveAttackCard` | 아군에게 사용 | 대상에 `ChainMoveAttackBuff` 컴포넌트 부착 | |
| TC-CARD-061 | 이미 `ChainMoveAttackBuff`가 부착된 아군에게 `ChainMoveAttackCard` 재사용 | 재사용 | 중복 부착되지 않는 멱등 처리(스택 없음) | |

### 6.9 적/보스 전용 (플레이어 미사용, AI 전용)

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-062 | `EnemyAttackCard`(Cost 2, dmg 5, NearestEnemy) | 적 AI 턴에 관측 | 최근접 아군 자동 타겟팅 후 5 피해 | |
| TC-CARD-063 | `EnemyMoveAndColDamageUpCard` / `EnemyMoveAndColDamageUp5Card`(이동 후 자가 colDamage 영구 +2 / +5) | 적 AI가 해당 행동 수행 | 이동 후 스탯 영구 상승, 이후 공격력에 반영 | `EnemyMoveAndColDamageUp5Card`는 프리팹 없음(⚠ orphan, 미사용 변형일 가능성) |
| TC-CARD-064 | `EnemyChargeCard`(Cost 0, `Charge` 텔레그래프 전용) | 적 AI 예고 동작 표시 | 실제 피해 없이 예고 포즈만 재생 | ⚠ 프리팹 없음(다른 프리팹에 컴포넌트로만 부착) |
| TC-CARD-065 | `EnemyThornCard`(자가 영구 Thorn power 2), White Knight 4에 부착 | 해당 적 유닛 스폰 | 자동으로 영구 Thorn 보유 상태로 시작 | |
| TC-CARD-066 | `EnemyWideChargedAttackCard`(dmg 10 AoE Fixed, 누적 ColDamageUp에 비례 증가) | 여러 사이클 관측 | 매 사이클 데미지가 이전 ColDamageUp 누적분만큼 커짐(영구 스탯이므로 복리 증가) | |
| TC-CARD-067 | `BossAttack1/2/3Card`(각각 퀸무브형, 바람개비형, 5x5 AoE+보호막) | 보스전에서 각 패턴 순서대로 관측 | 설명된 정확한 패턴/수치로 발동 | |
| TC-CARD-068 | `StunnedCard`(Cost 0, No-op 마커) | 적이 스턴 상태로 자기 턴 도달 | AI 행동이 이 카드로 대체되어 아무 효과 없이 턴 소모 | |

### 6.10 무덤형

무덤 자원 자체의 규칙(적립, 사용 가능 판정, 취소 시 차감 여부)은 [18. 무덤(Grave) 시스템](#18-무덤grave-시스템)에서 다룬다.

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-CARD-084 | `GraveHealCard`(Cost 1, 무덤 1 소모, 회복 2, 아군 대상, `noRangeLimit=true`), 시전자 무덤 1, 멀리 떨어진 부상 아군 | 그 아군에게 드롭 | 대상 2 회복, 시전자 무덤 1 → 0, 에너지 1 차감 | 무덤은 대상이 아니라 카드를 낸 기물의 것을 씀 |
| TC-CARD-085 | `GraveHealCard`, 시전자 무덤 1 | 적 기물 위에 드롭 | 사용 거부(아군 대상 카드), 무덤·에너지 변화 없음 | |
| TC-CARD-086 | `GraveAttackCard`(Cost 2, 3 피해 + 무덤 1 소모 시 같은 대상 3 피해), 시전자 무덤 1, 체력 충분한 적 | 적을 클릭해 사용 | 3 + 3 = 총 6 피해. 두 번째 타격은 다시 클릭하지 않아도 같은 대상에 들어감. 무덤 1 → 0 | |
| TC-CARD-087 | `GraveAttackCard`, 시전자 무덤 0 | 적을 클릭해 사용 | 카드는 사용 가능, 3 피해만 들어가고 두 번째 타격은 건너뜀. 카드 정상 종료 | 첫 효과에는 무덤 비용이 없음 |
| TC-CARD-088 | `GraveAttackCard`, 시전자 무덤 2 | 사용 | 무덤 1만 소모되어 1이 남음 | |
| TC-CARD-089 | `GraveAttackCard`, 시전자 무덤 1, 체력 3 이하 적 | 사용 | 1타에 적 처치, 2타는 빈 칸에 헛스윙(에러 없음) | ⚠ 2타가 헛스윙해도 무덤 1이 소모됨. 의도인지 확인 필요 |
| TC-CARD-090 | `GraveAttackCard`, 시전자 무덤 1, 취약(+1) 적 | 사용 | 4 + 4 = 총 8 피해 | 두 타격 모두 공격 경로라 취약 적용 |
| TC-CARD-091 | `GraveHarvestCard`(Cost 2, 3 피해, 처치 시 무덤 +1) | ① 체력 3 이하 적에게 사용 ② 체력 충분한 적에게 사용 | ① 적 처치, 시전자에게 "무덤 +1" 텍스트, 무덤 +1 ② 무덤 변화 없음 | |
| TC-CARD-092 | 시전자 무덤 0, 손패에 `GraveHarvestCard`와 `GraveHealCard` | `GraveHarvestCard`로 적 처치 후 같은 턴에 `GraveHealCard` 사용 | 처치 직후 `GraveHealCard`가 바로 사용 가능 상태가 되고 정상 사용됨 | 무덤 증가는 연출을 기다리지 않고 즉시 반영 |

---

## 7. 상태이상/버프 시스템

**관련 스크립트**: `StatusEffect.cs`, `StatusEffects.cs`, `TurnEffect.cs`, `ChainMoveAttackBuff.cs`, `Piece.ProcessStatusEffects`, `Piece.ModifyIncomingAttackDamage`, `Board.ApplyAttackDamage`(`Board.Combat.cs`), `Board.PrioritizeTauntTargets`(`Board.CardEffect.cs`)

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-STATUS-001 | 동일 대상에게 Poison(5) + Poison(3) 중첩 부여 | 턴 종료 처리 | 두 인스턴스의 피해가 8로 합산되어 단일 틱/단일 텍스트로 표시(2회로 분리되지 않음) | |
| TC-STATUS-002 | Poison(5, 2턴) + Poison(3, 1턴) 중첩 | 턴 종료 2회 경과 | 각 인스턴스가 독립적으로 카운트다운되어, 1턴 후 Poison(3)만 만료되고 이후 5만 남음 | |
| TC-STATUS-003 | Burning 다중 중첩 | 턴 종료 | Poison과 동일한 합산 규칙 적용(별개 타입이므로 Poison과는 합산 안 됨) | |
| TC-STATUS-004 | Regen 다중 중첩 | 턴 종료 | 회복량 합산 후 단일 회복 텍스트 | |
| TC-STATUS-005 | Strengthen 2회 중첩(+2, +3) | 턴 경과 후 하나만 만료 | 만료된 인스턴스의 델타(+2 또는 +3)만 정확히 회수, 나머지 유지 | |
| TC-STATUS-006 | Weaken 적용 후 Cleanse로 제거 | Cleanse 실행 | Weaken이 적용 당시 실제로 감소시킨(클램프된) 값만큼 정확히 복구 | |
| TC-STATUS-007 | `StunEffect` 적용/제거 | 적용 시 | Animator Stun bool 토글, 적 위협범위 UI(`ShowAllEnemyRanges`) 및 액션 텍스트 즉시 갱신 | |
| TC-STATUS-008 | `ThornEffect` 보유 중 이동공격을 "받음"(자신이 공격받는 게 아니라 이동공격의 피격자가 됨) | 이동공격 피격 | 반격 피해가 공격자에게 적용 | |
| TC-STATUS-009 | `MovementDisabledEffect` 부여 상태 | 이동 포함 카드 사용 시도 | 실제로 카드 큐가 취소되어 이동이 차단됨 | ⚠ 코드 주석("게임플레이 미적용")과 실제 동작 불일치, 문서/주석 정정 필요 |
| TC-STATUS-010 | `duration=-1`(영구) 상태이상 부여(예: `EnemyThornCard`) | 여러 턴 경과 관찰 | 턴 종료 카운트다운으로 감소하지 않고 무한 지속 | |
| TC-STATUS-011 | `TurnEffect`(예: `FlameThrowingCard`)가 `OwnTurnEnd`에 등록된 상태에서 캐스터 사망 | 캐스터 사망 후 턴 진행 | 더 이상 발동하지 않고 정상적으로 정리되는지 확인 | |
| TC-STATUS-012 | 자기피해형 DoT(`TurnDamageStart/End`, `isBuff=false`)와 적 대상 AoE DoT(`isBuff=true`) 동시 보유 | UI에서 버프/디버프 색상 확인 | 각각의 `isBuff` 값에 따라 초록/빨강으로 올바르게 구분 표시 | |
| TC-STATUS-013 | `ChainMoveAttackBuff` 부착 상태에서 Inspect 모드로 상태이상 목록 확인 | 정보창 확인 | `activeEffects` 기반 목록에는 표시되지 않음(별도 MonoBehaviour이므로) | |

### 7.1 취약(Vulnerable)

취약(+N)은 공격 경로(`AttackPiece`, `AreaAttackPiece`, `MoveAttack`)로 받는 피해에만 대상별로 N을 더한다. 공격 경로는 모두 `Board.ApplyAttackDamage`를 거친다. 독·화상 틱, 가시 반격, 자해, 자기 DoT, `DamageAllAllies`는 `GetDamage`를 직접 호출하므로 증가하지 않는다. 아래 케이스는 캐스터에게 이동공격력 보정(강화/약화/영구 보너스)이 없다고 가정한다.

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-STATUS-014 | 적에게 `VulnerableTestCard`로 취약(+1) 부여 | `AttackCard`(dmg 3)로 해당 적 공격 | 피해 텍스트 4, 실제 체력 감소 4 | |
| TC-STATUS-015 | 취약(+1) 적, 캐스터 colDamage N | 해당 적에게 이동공격 | 주 타겟 피해 N+1(텍스트/체력 모두) | |
| TC-STATUS-016 | 스플래시 이동공격 범위를 가진 기물, 스플래시 대상 2기 중 1기만 취약(+1) | 이동공격 실행 | 취약인 스플래시 대상만 +1, 다른 대상은 기본 피해 | 대상별로 개별 적용 |
| TC-STATUS-017 | 범위 안에 취약(+1) 적 1기 + 일반 적 1기 | `ZoneAttackCard`(dmg 2) 시전, 또는 `FlameThrowingCard` 사용 후 턴 종료 | 취약 적 3, 일반 적 2 피해. 턴 효과 광역 피해(FlameThrowing)도 공격으로 취급되어 증가 | |
| TC-STATUS-018 | 취약(+1) 적(체력 충분) | `DoubleAttackCard`(dmg 4, hitCount 2) 사용 | 타격마다 +1 → 5 + 5 = 총 10 | |
| TC-STATUS-019 | 같은 적에게 `VulnerableTestCard`를 다른 턴에 2회 사용(취약(+1) 2개, 남은 턴 다름) | `AttackCard`(dmg 3)로 공격 후 턴 경과 | 둘 다 걸린 동안 3 + 1 + 1 = 5. 먼저 건 인스턴스가 만료되면 4. 두 인스턴스는 각자의 지속시간으로 독립 만료 | |
| TC-STATUS-020 | 취약(+1) 적, 보호막 3 | `AttackCard`(dmg 3)로 공격 | 보정이 보호막보다 먼저 적용: 4 중 3은 보호막이 흡수, 체력 1 감소, 피해 텍스트 4 | |
| TC-STATUS-021 | 취약(+1) 적, 이동공격력 0인 공격자 | 해당 적에게 이동공격 | 피해 0 유지(0 피해 공격에는 보정이 붙지 않음) | ⚠ 약화를 거는 카드가 없어 정상 플레이로 colDamage 0을 만들기 어려움. 인스펙터에서 colDamage 0으로 설정한 기물로 확인 |
| TC-STATUS-022 | 취약(+1) 적 | ① `PoisonTestCard`로 독(2) 부여 후 적 턴 종료 ② 가시 반격·자해·자기 DoT·`DamageAllAllies` 경로 | ① 독 틱 피해 2 그대로 ② 모두 증가하지 않음 | ⚠ `VulnerableTestCard`는 적에게만 걸 수 있어, 아군 쪽 경로(가시 반격 수신, 자해, `DamageAllAllies`)는 정상 플레이로 재현 불가. 코드 확인 또는 테스트 부트스트랩 필요 |
| TC-STATUS-023 | 취약(+1) 적 1기 + 일반 적 1기 | ① `LifeDrainCard`(dmg 2)로 둘 다 적중 ② `BloodChargeCard`로 취약 적 이동공격 | ① 자힐 = 3 + 2 = 5 ② 자힐 = colDamage + 1 | "입힌 피해" = 대상별 보정 후 피해(보호막 흡수 전). TC-MOVE-014, TC-CARD-022 참고 |
| TC-STATUS-024 | `ChainMoveAttackBuff` 보유 아군, 단일 대상 이동공격 | ① 첫 대상만 취약(+1) ② 체인 대상만 취약(+1) | ① 첫 대상 +1, 체인 대상은 기본 피해 ② 첫 대상 기본, 체인 대상 +1 | 체인에는 보정 전 피해가 넘어가고, 체인 대상 자신의 취약만 붙음 |
| TC-STATUS-025 | 적에게 취약(+1, 2턴) 부여 | 정보창 확인 후 적의 자기 턴 종료를 2회 경과 | 정보창 `취약 (+1)  2턴` → `1턴` → "취약 (+1) 해제" 텍스트와 함께 제거. 이후 공격은 기본 피해 | 적의 자기 턴 종료마다 감소하므로 플레이어 턴 2번 동안 유지 |
| TC-STATUS-026 | `VulnerableTestCard`의 `statusDuration`을 -1로 변경 후 부여 | 여러 턴 경과 | 정보창에 턴 수 표시 없이 계속 유지, 만료되지 않음 | |
| TC-STATUS-027 | 취약(+1) 적 | `DispelCard`(버프 제거) 사용 | 취약은 디버프라 제거되지 않음 | 정화(`CleanseCard`)는 자기 디버프만 제거하므로 아군 취약 재현 수단이 생기면 추가 확인 |

### 7.2 도발(Taunt)

도발은 버프다. 상대 진영이 공격/이동 대상을 고를 때, 사거리 안에 닿는 도발 기물이 있으면 그 기물들만 후보로 남긴다(`Board.PrioritizeTauntTargets`). 후보 중 누구를 노릴지는 카드의 원래 기준(가장 가까움/최저 체력/방향 AoE는 대상 수)이 정한다. 이동 효과는 이동공격 도착 칸이 있는 도발 기물만 "닿는다"(`Board.CanEffectReach`). 닿는 도발 기물이 없으면 원래 로직 그대로다. 지금은 AI(적, 자동행동 아군)에만 적용되며, 적이 도발을 가졌을 때 플레이어 카드 대상을 제한하는 기능은 아직 없다. `TauntCard`(영구 도발, `targeting`+`self`)를 기물 프리팹의 `onSpawnCards`에 연결해 부여한다.

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-STATUS-028 | 아군 프리팹 `onSpawnCards`에 `TauntCard` 연결 | 전투 레벨 진입 | 전투 시작 시 그 아군에 "도발" 버프 텍스트, 정보창에 턴 수 없이 `도발` 표시, 여러 턴이 지나도 유지 | 이벤트 레벨에서는 소환 시 효과가 발동하지 않으므로 부여되지 않음 |
| TC-STATUS-029 | `EnemyAttackCard`(NearestEnemy) 적의 사거리 안에 더 가까운 일반 아군 C와 더 먼 도발 아군 A | 적 턴 진행 | 적이 A를 공격 | |
| TC-STATUS-030 | 도발 아군 A는 적 공격 사거리 밖, 일반 아군 C는 사거리 안 | 적 턴 진행 | 적이 C를 공격(원래 로직) | |
| TC-STATUS-031 | 적 이동 카드(`EnemyMoveCard`, `MoveandAttackCard`류 첫 효과) 이동 사거리 안에 더 가까운 일반 아군과 도발 아군 A | 적 턴 진행 | 적이 A에게 이동공격 | |
| TC-STATUS-032 | 적 이동 사거리 안에 도발 아군 없음 | 적 턴 진행 | 원래 로직: 사거리 안 가장 가까운 아군에게 이동공격, 사거리 안에 아군이 없으면 가장 가까운 아군 쪽 빈 칸으로 이동 | |
| TC-STATUS-033 | 도발 아군 A, B가 모두 적 사거리 안(A가 더 가까움), 일반 아군 C는 A 이하 거리 | 가장 가까움 기준 적 카드로 적 턴 진행 | 적이 A를 노림(도발 기물 중 가장 가까운 기물) | 두 아군 프리팹에 `TauntCard` 연결 |
| TC-STATUS-034 | 최저 체력(`LowestHP`) 적 카드, 사거리 안에 도발 A(HP 9), 도발 B(HP 5), 일반 C(HP 2) | 적 턴 진행 | 적이 B를 노림. 일반 C의 체력이 더 낮아도 무시 | ⚠ 기본 적 구성에 `LowestHP` 적 카드가 없음. 적 프리팹 `actionCards`에 `user=Enemy`인 `AttackCard`를 넣어 확인 |
| TC-STATUS-035 | 방향 AoE 적 카드, 한 방향은 도발 2기, 다른 방향은 도발 1기 + 일반 2기 | 적 턴 진행 | 도발 2기를 맞히는 방향 선택. 도발 수가 같으면 전체 대상 수가 많은 방향 | ⚠ 기본 적 구성에 방향 AoE 적 카드가 없음. 적 프리팹 `actionCards`에 `user=Enemy`인 `DirectionalAttackCard`를 넣어 확인 |
| TC-STATUS-036 | 적 이동 사거리 안에 도발 A(주변 도착 칸이 모두 막힘)와 도발 B(도착 칸 있음) | 적 턴 진행 | 적이 B에게 이동공격 | |
| TC-STATUS-037 | TC-STATUS-036에서 도발 B를 치움(도발 A만 남고 도착 칸은 막힘) | 적 턴 진행 | 도발이 없는 것처럼 원래 로직으로 행동. 가장 가까운 아군이 막힌 A면 지금처럼 A를 노리다 이동공격 실패, 제자리 | 원래 로직은 도달 여부를 보지 않음 |
| TC-STATUS-038 | 도발 아군이 적 사거리 안 | 적이 자기/아군 대상 카드(`EnemyThornCard`, `EnemyChargeCard` 등) 사용 | 원래대로 자기/아군에게 적용(도발 무관) | |
| TC-STATUS-039 | 적 프리팹 `onSpawnCards`에 `TauntCard` 연결, 자동행동 아군(소환수) 사거리 안에 그 적과 더 가까운 일반 적 | 아군 턴 종료 후 자동행동 아군 행동 | 소환수가 도발 적을 노림. 플레이어 카드는 도발 적 외 대상도 자유롭게 선택 가능(대상 제한은 미구현) | AI 우선순위는 양 진영 공용 |
| TC-STATUS-040 | 도발을 가진 적 | `DispelCard`(버프 제거) 사용 | 도발이 제거되고 "도발 해제" 텍스트 표시. `CleanseCard`(디버프 제거)로는 제거되지 않음 | |

---

## 8. 턴 진행 & 적 AI

**관련 스크립트**: `TurnManager.cs`, `Board.TurnControl.cs`, `AutoPiece.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-TURN-001 | 아군 턴 종료 직후 ~ 다음 아군 턴 시작 전 구간 | 이 구간에 카드/보드 클릭 시도 | `PlayerInputLocked`로 입력 차단, `IsPlayerActionable`이 false | |
| TC-TURN-002 | 자동 아군(소환수) 존재 | 아군 턴 종료 후 관찰 | `PlayAutoAllyTurnCoroutine`이 적 턴 시작 전에 실행되어 자동 행동 수행 | |
| TC-TURN-003 | 적 AI가 스턴 상태로 자기 턴 도달 | 적 턴 진행 | `GetNextMove()`가 `Movenum`을 증가시키지 않고 `StunnedCard` 반환(스턴 해제 후 원래 예고된 행동 그대로 재개) | |
| TC-TURN-004 | 적의 `actionCards` 인덱스가 마지막 항목(`Count-1`)일 때 `ChangeMove()` 호출 | 다음 행동 결정 | `Movenum`이 0으로 정확히 래핑 | |
| TC-TURN-005 | 적 턴 진행 중 스냅샷 순회 도중 다른 적이 사망(예: 반격/연쇄피해) | 적 턴 코루틴 진행 | 죽은 적의 스냅샷 항목은 null 체크로 스킵되어 예외 없이 계속 진행 | |
| TC-TURN-006 | `Board.CombatEnded=true`가 된 시점 | 턴 코루틴이 진행 중이었다면 | 이후 턴 전환 코루틴이 즉시 종료(추가 배너/행동 발생 안 함) | |
| TC-TURN-007 | 첫 전투 시작(`isFirstTurn`) | 전투 진입 | "전투 시작" 배너 후 "플레이어 턴" 배너가 순차 표시(1회성) | |
| TC-TURN-008 | 아군 턴 종료 | `AllyTurnEnd` 처리 | `RestoreThisTurnCosts()`, 전체 아군 핸드 버림, 턴종료 상태이상/유물 처리, 진행 중이던 카드 강제 종료가 모두 수행 | |

---

## 9. 유물 시스템

**관련 스크립트**: `Relic.cs`, `RelicDatabase.cs`, `Relics/ShieldRelic.cs`, `Relics/VampiricFangRelic.cs`, `Board.Relics.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-RELIC-001 | `ShieldRelic`(CombatStart, 자가 보호막+3) 보유 | 새 전투 레벨(비이벤트) 진입 | 전투 시작 시 자동으로 보호막 +3 | |
| TC-RELIC-002 | `ShieldRelic` 보유, 이벤트 레벨 진입 | 이벤트 레벨 진입 | `TriggerRelicsOnCombatStart` 미발동(이벤트 레벨은 스킵) | |
| TC-RELIC-003 | `VampiricFangRelic`(OnKill, 자가 회복+2) 보유 | 적 처치 | 처치 즉시 2 회복 | |
| TC-RELIC-004 | 존재하지 않는 클래스명으로 유물 생성 시도(데이터 오류) | `RelicDatabase.CreateRelic("Typo")` 호출 | `Debug.LogError` 출력, `null` 반환, 크래시 없음 | |
| TC-RELIC-005 | 유물 클래스명과 아이콘 프리팹명이 불일치하는 경우 | `SpawnIcon` 호출 | 아이콘 표시 실패(에러 로그), 효과 자체는 정상 동작 | ⚠ 클래스명-아이콘명 매칭 무결성 별도 점검 필요 |
| TC-RELIC-006 | `CardUsed` 타이밍 유물, `TargetsCardTarget=true` | 아군이 적 대상 카드 사용 | 유물 효과가 캐스터가 아닌 카드의 실제 대상에 적용 | |
| TC-RELIC-007 | 여러 레벨 연속 진행(2번째 전투 이상) | 새 레벨 진입마다 관찰 | `LoadOwnedRelics()`가 매 레벨마다 재호출되어 `CombatStart` 유물이 매번 재적용됨(1회성 아님) | |
| TC-RELIC-008 | 유물 On-Hit 타이밍(`TriggerRelicsOnHit`) | 아군이 공격으로 적 타격 | 정상 발동, DoT/자기피해로는 발동 안 함(섹션 4 TC-DMG-006 참고) | |

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

**관련 스크립트**: `ShopCanvas.cs`, `ShopCardSlot.cs`, `ShopRelicSlot.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-SHOP-001 | 상점 진입 | 카드/유물 가격 확인 | `cardPrice`/`removePrice`/`relicPrice`가 0이 아닌 의도된 값으로 설정되어 있음 | ⚠ 코드 기본값 0. `MainScene.unity` 기준 cardPrice 30, removePrice 0, relicPrice 0이라 카드 제거와 유물 구매가 무료 |
| TC-SHOP-002 | 골드 부족 상태에서 카드/유물 구매 시도 | 구매 클릭 | "골드가 부족합니다" 안내, 골드/보유 카드 변화 없음 | |
| TC-SHOP-003 | 카드 구매(골드 충분) | 구매 클릭 | 골드 차감 후 `PieceTargetPickerUI`로 대상 기물 선택 강제, 취소 불가 | ⚠ 구매 확정 후 대상 지정을 취소할 방법 없음 |
| TC-SHOP-004 | 카드 제거 구매, 대상 기물의 덱이 매우 적음(1장) | 제거 진행 | 골드 차감 후 선택 패널에서 정상 처리(제거 가능 카드가 충분히 있는지 확인) | |
| TC-SHOP-005 | 상점에 카드/유물 슬롯 8/4개 노출 | 슬롯 채우기 | `CardDatabase.PickRandomDistinct`/`RelicDatabase.PickRandomDistinct`로 중복 없이 채워짐(가중치/희귀도 없음) | |
| TC-SHOP-006 | 상점 진입 직후 | 나가기(Exit) 버튼 상태 | 아무 것도 안 사도 즉시 나가기 가능(구매 게이팅 없음) | ⚠ 코드 주석상 임시 처리로 명시됨 |

---

## 12. 보상/결과 화면

**관련 스크립트**: `ResultCanvas.cs`, `RewardSlot.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-RESULT-001 | 전투 승리, 생존 아군 2명, `rewardGoldMin/Max` 설정됨 | 결과 화면 진입 | 골드 슬롯 1개(범위 내 무작위) + 생존 아군 수만큼 카드 보상 슬롯 | |
| TC-RESULT-002 | `rewardGoldMin=0, rewardGoldMax=0`인 레벨 | 결과 화면 진입 | 골드 보상 슬롯 자체가 생성되지 않음 | |
| TC-RESULT-003 | 생존 아군의 `PieceInfo.Job`이 설정됨(Warrior/Summoner) | 카드 선택 오픈 | 해당 직업의 `RewardCardPool`에서 3장 무작위 제시 | |
| TC-RESULT-004 | 생존 아군에 `Job`이 없거나 `RewardCardPool`이 비어있음 | 카드 선택 오픈 | `CardDatabase.GetAllCardNames()` 전체에서 폴백 선택(크래시 없음) | |
| TC-RESULT-005 | 보상 후보 풀에 `AttackCard`/`DefenseCard`/`MoveCard`/`SummonCard`가 포함될 수 있는 상황 | 카드 선택 오픈 | 4종 기본 카드는 `ExcludedFromRewards`로 절대 제시되지 않음 | |
| TC-RESULT-006 | 생존 아군 3명 | 각 아군별 카드 보상 개별 수령 | 한 아군이 보상을 골라도 다른 아군의 보상 슬롯은 독립적으로 유지 | |
| TC-RESULT-007 | 마지막 층 클리어 | 결과 화면 이후 흐름 | `MapCanvas.ShowRunComplete()` 호출(일반 층 클리어와 다른 화면) | |
| TC-RESULT-008 | 일반 층 클리어, `RewardType=PieceUpgrade` | 결과 화면 대신 | `Board.GrantLevelReward()`가 선택형 영구 스탯 강화 플로우로 분기(카드/골드 보상 화면 생략) | |

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

---

## 14. UI/입력/애니메이션 타이밍

**관련 스크립트**: `Board.InputHandler.cs`, `Board.Animation.cs`, `EndTurnButton.cs`, `AnnouncementUI.cs`, `CardDragArrow.cs`, `ISelectable.cs`, `CameraFX.cs`, `GameSpeedManager.cs`, `PieceGaugeListUI.cs`, `ButtonInfo.cs`, `Assets/CardsPanel.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-UI-001 | 공격 애니메이션 클립에 Animation Event가 설정되지 않음 | 해당 애니메이션 재생 | 1.2초 타임아웃 폴백으로 다음 리액션이 정상 진행(무한 대기 없음) | |
| TC-UI-002 | 공격 트리거에 대응하는 Animator 상태가 없는 기물(애니메이터 미구성) | 공격 실행 | DOTween 기반 "펀치" 폴백 연출 재생 후 콜백으로 `OnAnimationEvent()` 수동 발화 | |
| TC-UI-003 | `CameraFX.HitStop` 진행 중 배속 변경 버튼 클릭 | 배속 변경 | 즉시 반영되지 않고, 히트스탑 종료 시점에 새 배속이 적용됨 | |
| TC-UI-004 | 히트스탑 A(0.05s) 진행 중 더 짧은 히트스탑 B 요청 | B 요청 | 무시됨(끝나는 시점이 더 이른 요청은 연장하지 않음) | |
| TC-UI-005 | 종료 턴 버튼 5개 조건(플레이어 턴/큐 미진행/전투 미종료/카드 미처리/대기 애니메이션 없음) 중 하나라도 거짓 | 매 조건별 개별 검증 | 해당 조건이 거짓인 동안 버튼이 비활성 상태 유지 | |
| TC-UI-006 | 마우스 없는 환경(터치 전용 디바이스) | `CardDragArrow` 활성 상태에서 프레임 진행 | `Mouse.current`가 null이어도 예외 발생하지 않음 | ⚠ 현재 null 가드 미비, 크래시 가능성 점검 |
| TC-UI-007 | 카드/버튼/유물 아이콘에 마우스를 빠르게 반복 호버(in/out) | 반복 호버 | `ScaleAnimator`가 이전 트윈을 kill하고 새로 시작해 스케일이 누적/꼬이지 않음 | |
| TC-UI-008 | 서로 다른 소스(호버 + 선택)가 같은 칸의 범위 하이라이트를 동시에 요청 | 하나의 소스가 해제 | 참조 카운트 방식으로 다른 소스가 남아있으면 하이라이트 유지, 모두 해제되면 꺼짐 | |
| TC-UI-009 | `AnnouncementUI`가 메시지 표시 중 | 새 메시지 발생 | 기존 표시를 즉시 중단하고 새 메시지로 갱신(경고음은 `isWarning=true`인 경우만) | |
| TC-UI-010 | `PieceGaugeListUI` 표시 중 기물 스폰/사망 발생 | 매 프레임 확인 | 게이지 목록이 프레임마다 보드 전체를 스캔해 항상 최신 상태 반영 | ⚠ 매 프레임 전수 스캔이라 보드가 커질 경우 성능 확인 필요 |
| TC-UI-011 | 카드 덱이 없는 오브젝트(RestObject/ShopObject/NPC)를 게이지 목록에서 확인 | 확인 | 핸드/덱 카운트 텍스트가 숨김 처리(예외 없음) | |
| TC-UI-012 | `CardsPanel`에서 동일한 뷰 모드(예: RuntimeDeck) 버튼을 연속 두 번 클릭 | 두 번째 클릭 | 패널이 갱신되지 않고 닫힘(토글-닫기 동작) | ⚠ 의도된 UX인지 확인 |
| TC-UI-013 | `CardsPanel`이 RuntimeDeck/Discard 보기 상태에서 전투 중 카드 드로우/버림 발생 | 실시간 관찰 | `CardCanvas.OnPileChanged` 이벤트로 즉시 갱신 | |
| TC-UI-014 | `CardsPanel`이 SavedDeck 보기 상태에서 전투 중 카드 변화 발생 | 관찰 | 자동 갱신되지 않음(명시적 재호출 전까지 정적 스냅샷 유지) | |
| TC-UI-015 | `CardsPanel`에서 카드 확대 미리보기 애니메이션 진행 중 같은 카드 재클릭 | 재클릭 | 애니메이션 진행 중이면 무시(중복 미리보기 방지) | |
| TC-UI-016 | 로스터가 1명뿐인 상태에서 `CardsPanel`의 SavedDeck 보기 | 확인 | 이전/다음 기물 전환 버튼(switcher)이 숨김 처리 | |
| TC-UI-017 | 정보창(`ButtonInfo`)에서 `RestObject`/`ShopObject` 클릭 | 확인 | 각각 "휴식 지점"/"상점" 전용 placeholder 텍스트 표시, 일반 기물 스탯 UI로 빠지지 않음 | |
| TC-UI-018 | 일반 기물 정보창에서 버프/디버프 상태 확인 | 확인 | 버프는 초록, 디버프는 빨강, 영구 효과(`duration<0`)는 지속시간 텍스트 미표시 | |

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

**관련 스크립트**: `JobInfo.cs`, `Assets/SO/Jobs/Warrior.asset`, `Assets/SO/Jobs/Summoner.asset`, `전사_소환사_카드목록.txt`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-JOB-001 | Summoner 기물 보상 오픈 | 보상 풀 확인 | `Summoner.asset`의 17개 카드(SummonCard, GreaterSummonCard, TauntSummonCard, EmpowerAllyCard, SummonMasteryCard, AreaHealCard, SummonGrowthCard, MagicAttackCard, ZoneAttackCard, FetchAttackCard, ImmobilizeCard, MoveAndDrawCard, SafeMoveCard, GraveHealCard, GraveAttackCard, GraveHarvestCard, VulnerableTestCard) 전부 실제 프리팹으로 존재 확인 | ⚠ `VulnerableTestCard`는 검증 전용 카드로 임시 포함 — 플레이테스트 후 제거 여부 결정 |
| TC-JOB-002 | Warrior 기물 보상을 반복 오픈(예: 50회) | 카드명 수집 | `FinalAttackCard`라는 이름은 절대 제시되지 않음(스크립트/프리팹/DB 엔트리 없어 `PickRandomDistinctFrom`에서 자동 스킵) | ⚠ Warrior.asset에 존재하지 않는 카드 참조가 있음, 기획 의도(원래 다른 카드였는지) 확인 필요 |
| TC-JOB-003 | Warrior 보상 풀의 나머지 26개 카드 각각 | 보상으로 제시될 때 수치 확인 | `전사_소환사_카드목록.txt`에 명시된 코스트·수치(예: HeavyAttackCard 코스트 2·6뎀/2자해, DoubleAttackCard 코스트 3·4뎀×2)와 실제 구현이 일치 | |
| TC-JOB-004 | `Warrior.asset`(27장)·`Summoner.asset`(17장)과 `전사_소환사_카드목록.txt` | 두 목록을 대조 | 카드 구성·개수가 일치하고, 텍스트 파일의 설명이 게임 내 카드 설명(`EffectDescription`)과 같음 | 카드를 보상 풀에 추가·제거할 때마다 텍스트 파일도 함께 갱신 |
| TC-JOB-005 | Job이 설정되지 않은 기물 | 보상 오픈 | `ResolveRewardPoolFor`가 전체 카드 목록으로 폴백 | |

---

## 17. 교차 시스템 회귀 테스트

과거 수정 이력이 코드 주석으로 남아있어, 회귀 가능성이 높은 항목들이다.

**관련 스크립트**: `Board.Combat.cs`, `CardCanvas.cs`, `Board.TurnEffects.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-REG-001 | 단일 셀 방향성 이동공격 범위(예: Warrior류)를 가진 `ChainMoveAttackBuff` 보유 기물 | 방향 회전된 이동공격으로 정확히 1명만 타격 | 회전된 범위가 "1칸짜리 특수 패턴"이라는 이유로 체인이 잘못 차단되지 않고 정상 발동(`hitMultipleTargets`는 실제 스플래시 적중 수 기준, `isAreaAttack` 플래그 기준 아님) | 과거 버그 수정 사항 회귀 테스트 |
| TC-REG-002 | 자가 비용 감소 효과(`WarmUpDamageCard`)를 가진 카드 시전 | 시전 시점의 에너지 차감량 확인 | 이번 시전에 비용 감소가 소급 적용되지 않고, 차감은 원래 비용 기준으로 발생(에너지 차감 로직이 `FinishUseCard`가 아닌 `ExecuteEffect` 시점에 있음) | 과거 버그 수정 사항 회귀 테스트 |
| TC-REG-003 | `EnqueueCardEffectOnPiece`(온킬/영구버프 등에서 사용하는 좁은 경로)로 새로운 `EffectType`을 처리해야 하는 카드 추가 시 | 신규 EffectType 추가 후 해당 경로 호출 | 지원되지 않는 EffectType은 조용히 무시되지 않고 개발자가 인지할 수 있도록 처리되는지 확인(신규 기능은 `EnqueueScheduledEffect` 경로 사용 권장) | ⚠ 좁은 레거시 경로, 신규 카드 추가 시 회귀 취약 지점 |
| TC-REG-004 | 여러 카드 효과가 동시에 `motionQueue`에 쌓이는 상황(예: 이동공격 스플래시 + 체인 + DoT 동시 트리거) | 빠르게 연쇄 발동 | `awaitingFlyOut`/`pendingCardFlights`의 FIFO 순서 가정이 깨지지 않고 카드별로 올바른 완료 신호가 매칭됨 | ⚠ 동시다발 상황 스트레스 테스트 권장 |
| TC-REG-005 | 취약이 없는 상태 | 단일 공격, 광역 공격, 이동공격(스플래시 포함), `healOnHit` 카드(`LifeDrainCard`, `BloodChargeCard`), 체인 이동공격을 각각 실행 | 피해 텍스트, 체력 감소, 사망 판정, 자힐량, On-Hit 유물 발동이 공격 피해 공통화(`ApplyAttackDamage`) 이전과 동일 | 공격 피해 경로 공통화 회귀 테스트 |
| TC-REG-006 | 공격으로 피해를 주는 새 경로(카드/효과) 추가 시 | 해당 경로로 취약 대상 공격 | 취약 보정이 적용됨(`Board.ApplyAttackDamage`를 거침) | ⚠ 새 경로가 `GetDamage`를 직접 호출하면 취약이 조용히 누락됨. 신규 공격 경로 추가 시 회귀 취약 지점 |
| TC-REG-007 | 턴 효과 만료 규칙이 `StatusEffect.OnTurnEnd` 공용 규칙으로 바뀜(음수 지속시간 = 영구) | `FlameThrowingCard`(3턴), `PersistentShieldCard`(다음 턴 1회) 사용 후 턴 진행 | `FlameThrowingCard`는 정확히 3회 발동 후 만료, `PersistentShieldCard`는 다음 턴 시작에 1회 발동 후 만료(변경 전과 동일) | 턴 효과 만료 규칙 공통화 회귀 테스트 |
| TC-REG-008 | `Piece.onSummonCards`가 `onSpawnCards`로 이름 변경(`FormerlySerializedAs`). `autoally.prefab`은 옛 이름으로 저장됨 | `SummonCard`로 autoally 소환 | 스폰 시 효과(`DefenseCard`) 연결이 유지되어 방어도 2 획득 | ⚠ `FormerlySerializedAs`를 지우면 연결이 끊김. 프리팹을 다시 저장해 새 필드명으로 갱신 권장 |
| TC-REG-009 | `lockedCaster`/`lockCasterForNext`가 제거되고 효과마다 시전자(`CardEffect.caster`)를 기록하는 방식으로 바뀜 | `MoveandAttackCard`, `MoveAndDrawCard`, `SummonGrowthCard`, `SummonMasteryCard`, `HeavyAttackCard`(자해) 각각 사용 | 두 번째 이후 효과가 카드를 쓴 기물의 현재 위치 기준으로 실행되고, 다른 아군에게 효과가 새지 않음 | 시전자 처리 방식 변경 회귀 테스트. TC-ENGINE-026~028 참고 |
| TC-REG-010 | 전투 시작 유물이 즉시 적용에서 효과 큐 처리로 바뀜 | `ShieldRelic` 보유 상태로 아군 2명 이상 전투 진입 | 아군마다 방어도 +3, 첫 턴 입력·기물 전환 정상 | TC-CARD-075, TC-RELIC-001 참고 |

---

## 18. 무덤(Grave) 시스템

무덤은 기물마다 따로 쌓이는 전투 한정 자원이다(`Piece.grave`). 같은 팀 기물이 죽으면 그 순간 살아 있는 같은 팀 기물 전원이 무덤 +1을 얻는다(소환수 포함). 저장되지 않으므로 전투마다 0에서 시작한다. 카드 효과의 `graveCost`로 소모하며, 무덤은 카드를 낸 기물(시전자)의 것을 쓴다. 첫 효과의 무덤이 부족하면 카드를 쓸 수 없고, 두 번째 이후 효과의 무덤이 부족하면 그 효과만 건너뛴다. 무덤은 효과가 실제로 실행될 때 차감된다. 카드별 케이스는 [6.10 무덤형](#610-무덤형)에 있다.

**관련 스크립트**: `Piece.cs`(`AddGrave`/`HasGrave`/`ConsumeGrave`), `Board.Occupancy.cs`(`AddGraveToTeammates`), `Board.CardEffect.cs`(`PayGrave`, `ProcessNextCardEffectStep`), `Card.HasGraveForFirstEffect`, `CardCanvas.cs`

| ID | 사전조건 | 테스트 절차 | 기대 결과 | 비고 |
|---|---|---|---|---|
| TC-GRAVE-001 | 이전 전투에서 무덤을 쌓은 채 승리 | 다음 전투 레벨 진입 | 모든 기물의 무덤이 0에서 시작(이월되지 않음) | |
| TC-GRAVE-002 | 아군 A, B와 소환수 C 생존 | 소환수 C가 사망 | A, B 위에 각각 "무덤 +1", 무덤 1. 소환수 사망도 집계됨 | |
| TC-GRAVE-003 | 아군 A, 적 2기 | 적 1기 처치 | 아군 A의 무덤은 변화 없음. 남은 적 위에 "무덤 +1" 텍스트 | ⚠ 적은 무덤을 쓰는 카드가 없어 적 쪽 "무덤 +1" 연출은 의미 없는 표시. 의도 확인 필요 |
| TC-GRAVE-004 | 아군 3기 중 2기가 체력 2 이하, `ZoneAttackCard`(팀 무관 2 피해) 범위 안 | 두 기물이 같은 공격에 함께 사망하도록 시전 | 살아남은 아군만 무덤 +2(사망 1기당 +1). 함께 죽은 두 기물은 서로의 사망으로 무덤을 받지 않음 | |
| TC-GRAVE-005 | 사망 처리 중인 기물에 추가 피해가 겹치는 상황(예: 광역 공격 + 턴 종료 DoT) | 같은 기물이 연달아 사망 판정 | 그 기물의 사망은 무덤에 한 번만 집계 | |
| TC-GRAVE-006 | 활성 기물의 무덤 0, 손패에 `GraveHealCard`(첫 효과 무덤 1) | 카드 상태 확인 후 사용 시도 | 카드가 사용 불가 상태로 표시되고, 사용을 시도하면 "무덤이 부족합니다" 안내와 함께 거부 | |
| TC-GRAVE-007 | TC-GRAVE-006 상태 | 다른 아군이 사망 | 손패의 `GraveHealCard`가 즉시 사용 가능 상태로 바뀜 | |
| TC-GRAVE-008 | 아군 A 무덤 1, 아군 B 무덤 0, 두 기물 손패에 모두 `GraveHealCard` | A, B로 활성 기물을 바꿔 가며 확인 | A의 카드만 사용 가능. 무덤은 기물별로 따로 관리됨 | |
| TC-GRAVE-009 | 시전자 무덤 1, `GraveHealCard` | 카드를 집어 대상 선택 단계에서 취소 | 무덤 1 유지, 에너지 변화 없음 | |
| TC-GRAVE-010 | 무덤이 있는 기물 | 정보창(`ButtonInfo`)에서 그 기물 확인 | 현재 무덤 수를 확인할 수 있음 | ⚠ 현재는 무덤 수를 표시하는 UI가 없어 실패. 늘어날 때의 "무덤 +N" 텍스트로만 알 수 있음 |
