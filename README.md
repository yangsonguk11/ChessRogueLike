# ChessRogueLike

체스 기물과 덱빌딩 카드 시스템을 결합한 Unity 로그라이크 게임입니다. 체스판 위에서 카드를 사용해 기물을 움직이고, 공격하고, 버프/디버프를 걸어가며 층을 클리어합니다.

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
- 매니저 싱글턴(`GameManager`, `TurnManager`, `CardDatabase` 등)은 인터페이스(`IGameManager`, `ITurnManager`, `ICardDatabase` 등)를 통해 접근하도록 되어 있습니다.
- 카드 효과(`CardEffect`)는 명명된 필드를 가진 `record`로 정의되어 있어 호출부에서 인자의 의미를 명확히 알 수 있습니다.
- 보드 좌표는 `Vector2Int`로 통일되어 있습니다.

