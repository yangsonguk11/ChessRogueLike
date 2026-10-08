# 기술·완성도 개선 로드맵

작성: 2026-10-07 · 기준: `ManyJobCards` 브랜치 작업 트리

에셋 관리·성능·조작감·셰이더·애니메이션·배경·개발 과정 전반에서 현업 기법을 적용할 곳을 조사해 우선순위로 정리한 문서다. 항목마다 코드·설정에서 확인한 근거가 있다.

- **제약**: 새 패키지는 추가하지 않는다. Git LFS는 쓰지 않는다. Jira에는 등록하지 않았다. 📦 표시는 패키지가 필요해 보류한 부분이다(대안은 4절).
- **읽는 법**: 2절 우선순위를 위에서부터 진행한다. 괄호 안 ID(A1, H5 등)는 5절 카탈로그의 상세 설명이다. 줄 번호는 작성 시점 기준이라 코드가 바뀌면 어긋날 수 있다.

---

## 1. 핵심 발견 — 코드·설정에서 확인한 사실

1. **타격감 이펙트가 만들어져만 있고 꺼져 있다** — `CameraFX.Shake/HitStop/Flash` 호출 0건 ([CameraFX.cs:79-119](../Assets/Scripts/CameraFX.cs#L79-L119)).
2. **손패가 순간이동한다** — `AlignCards`/`ExcludeAlignCards`가 위치를 트윈 없이 바로 대입한다. 카드를 쓰거나 뽑을 때 나머지 카드가 툭 튀고, 취소하면 카드가 손패 자리로 순간이동한다 ([CardCanvas.cs:479-495](../Assets/Scripts/CardCanvas.cs#L479-L495), [CardCanvas.cs:1272-1332](../Assets/Scripts/CardCanvas.cs#L1272-L1332)).
3. **호버한 카드가 오른쪽 카드에 가려진다** — 호버 시 그리기 순서를 올리지 않는다(20px 리프트·1.1배 스케일만). CRL-50과 같은 문제다 ([Card.cs:275-285](../Assets/Scripts/Card.cs#L275-L285)).
4. **데미지 텍스트마다 머티리얼 인스턴스를 만든다** — `tmp.fontMaterials`에 renderQueue를 쓰는 방식이라 텍스트마다 배칭이 깨지고 인스턴스가 정리되지 않을 수 있다. 텍스트도 매번 Instantiate/Destroy한다 ([PieceCanvas.cs:38-80](../Assets/Scripts/PieceCanvas.cs#L38-L80)).
5. **프레임 제한이 없다** — 두 품질 단계 모두 `vSyncCount: 0`이고 `targetFrameRate`도 없다. 턴제 게임인데 GPU를 최대로 돌린다.
6. **안티앨리어싱이 없다** — PC_RPAsset `m_MSAA: 1`(꺼짐), 카메라 AA None.
7. **카드 일러스트가 압축되지 않는다** — 912×1179 등 가로·세로가 4의 배수가 아니라 BC/DXT 압축을 못 해 장당 약 4.3MB 비압축으로 올라간다. 스프라이트 아틀라스도 없다.
8. **한글 폰트 에셋이 39MB다** — 4096² 정적 SDF에 글리프 약 1.18만 개를 git에 그대로 커밋했다. TMP 기본 폰트(LiberationSans)에는 한글 폴백이 없다.
9. **Assets 안에 게임과 무관한 334MB 폴더가 있다** — `Assets/Casual Game Sounds U6/CasualGameSounds/gakumas-local`(파일 약 2,800개, git 미추적)을 Unity가 임포트하고 있다(Editor.log 확인). `_Recovery` 씬 2개는 커밋돼 있다.
10. **저장소 위생** — `.gitattributes`가 없고, 느슨한 git 오브젝트가 4,808개(130MB, gc 안 됨)다. 프로젝트가 OneDrive 동기화 폴더 안에 있다(Library 동기화·파일 잠금 위험).
11. **범위 셰이더가 구형 문법이다** — [RangeHatch.shader](../Assets/Shaders/RangeHatch.shader)가 CGPROGRAM이고 CBUFFER가 없어 SRP Batcher와 호환되지 않는다. 칸마다 테두리(0.06)를 그려 붙은 칸 사이에 내부선이 생기고, 텍스트를 위에 그리려고 renderQueue 3002로 땜질했다.
12. **이동이 기계적이다** — 이동 트윈이 `Ease.Linear`이고 들어올림·착지가 없다 ([Board.Animation.cs:76](../Assets/Scripts/Board.Animation.cs#L76)). Animator의 Die/Charge/Stun 상태에는 클립이 없다.
13. **턴 배너가 템포를 막는다** — "플레이어 턴"/"적 턴" 배너가 매번 1.6초(첫 턴 3.2초) 동안 진행을 막는다 ([TurnManager.cs:77-90](../Assets/Scripts/TurnManager.cs#L77-L90)).
14. **사운드 기반이 부족하다** — BGM과 AudioMixer가 없고, 효과음은 AudioSource 1개로 `PlayOneShot`만 한다.
15. **입력이 하드코딩돼 있다** — `Mouse.current`를 직접 참조하는 곳이 4곳이다. 프로젝트 전역 Input Actions는 등록만 되고 쓰이지 않는다.
16. **씬 전환·세이브·턴 호출** — 동기 `LoadScene` 4곳(페이드·로딩 없음). 세이브는 `File.WriteAllText`로 바로 덮어써서 쓰는 도중 실패하면 손상된다. TurnManager→Board 호출 4곳이 문자열 `SendMessage`다.
17. **이름 기반 세이브가 취약하다** — 프리팹·클래스 이름이 곧 ID라 이름을 바꾸면 세이브가 깨진다(CRL-23).
18. **테스트 기반이 없다** — asmdef가 없고 자동 테스트는 0개, QA 문서 약 400케이스는 수동이다. 다만 "로직은 즉시 확정, 연출은 큐" 구조라 상태 검증은 자동화하기 쉬운 편이다.
19. **진행 중인 이름 변경이 기존 세이브의 카드를 조용히 없앤다** — 작업 트리에서 `VulnerableTestCard`가 `VulnerableCard`로 바뀌었는데, HEAD에선 소환사 보상 풀에 있던 카드라 기존 세이브 덱에 들어 있을 수 있다. 옛 이름은 `SpawnCard`가 null을 돌려 전투에 나오지 않는다 ([CardDatabase.cs:29-34](../Assets/Scripts/CardDatabase.cs#L29-L34)). 또 세이브 덱 선택 창(카드 제거)은 스폰에 실패한 만큼 인덱스가 밀려 **다른 카드를 지울 수 있다** ([CardCanvas.cs:909-919](../Assets/Scripts/CardCanvas.cs#L909-L919), [CardCanvas.cs:1073-1089](../Assets/Scripts/CardCanvas.cs#L1073-L1089)).

---

## 2. 우선순위 목록

기준은 체감 효과 × 비용 × 위험 × 선행 관계다. 위에서부터 진행하기를 권한다.

### 1단계 — 바로 할 것 (각 2시간 이내, 위험 낮음)

1. **작업 폴더를 OneDrive 밖으로 옮기기** (H13) — Unity와 IDE를 닫고 폴더째(Library 포함) `C:\Dev\` 같은 곳으로 옮긴 뒤 Unity Hub에 다시 추가한다. Claude Code 사용자 메모리는 경로 기준 폴더라서 기존 `~/.claude/projects/c--Users-yangs-OneDrive----GitHub-ChessRogueLike/memory`를 새 경로용 폴더로 복사해야 한다. 완료 기준: 새 경로에서 열리고 `git status`가 같다.
2. **정크 정리** (H1) — `gakumas-local`(334MB)을 Assets 밖으로 옮기고, 내용을 확인한 뒤 `_Recovery/`, TutorialInfo, `Readme.asset`, `(old)actIcon.png` 등을 지운다. 완료 기준: Editor.log에 gakumas 임포트가 없다.
3. **이름 변경 별칭 표** (H7 일부, 발견 19) — `DataManager.LoadFromFile`의 `MigrateLegacyDeckIfNeeded` 옆에 옛 이름→새 이름 치환(덱 카드·기물 이름·유물·레벨)을 넣는다. `VulnerableTestCard→VulnerableCard`, `Summoner→SummonerAlly`(CRL-23)부터 등록한다. 세이브 덱 선택 창은 스폰한 카드마다 원래 덱 인덱스를 기록해 지우게 고친다. 완료 기준: 옛 이름이 든 save.json을 불러도 카드가 남아 있고, 카드 제거가 고른 카드를 정확히 지운다.
4. ✅ **프레임 제한** (G1) — QualitySettings의 PC/Mobile 단계 모두 `vSyncCount: 1`. 완료 기준: FPS가 모니터 주사율에 고정된다.
   - 2026-10-07 적용: PC·Mobile 모두 `vSyncCount: 1`.
5. ✅ **렌더 설정** (G2·G8) — PC_RPAsset에서 MSAA 4x, 그림자 캐스케이드 1–2·거리 20–25, Opaque Texture 끔(깊이 텍스처는 나중 DOF용으로 유지). 완료 기준: 외곽 계단이 줄고, Frame Debugger에서 Copy Color 패스가 사라진다.
   - 2026-10-07 적용: `m_MSAA: 4`, `m_RequireOpaqueTexture: 0`, `m_ShadowDistance: 25`, `m_ShadowCascadeCount: 1`. Depth Texture는 유지했다(SSAO가 DepthNormals를 쓴다).
   - 거리 25의 근거: 카메라(높이 7.66, 61.5°로 내려다봄)에서 보이는 지면의 가장 먼 점은 16:9 화면 모서리에서 19.6, 21:9에서 22.5유닛이다. URP는 최대 거리의 약 89%부터 그림자를 흐린다. 기존 4캐스케이드에서는 보드 전체가 두 번째 캐스케이드(1024 타일) 하나에 들어가 있었으므로, 2048 타일 1개로 바꿔도 보드 그림자 해상도는 비슷하거나 더 높다.
6. **CameraFX 연결** (A2) — 이미 있는 함수를 타격 순간에 부른다. `Piece.DamageText`에서 공격 피해(isAttack)일 때 피해/최대HP에 비례한 `Shake`, 아군이 맞았으면 붉은 `Flash`. 처치 순간에 실행되는 `Piece.PieceDeathSound`에서 `HitStop(0.05)`과 더 센 흔들림. 독·화상 틱은 제외한다. 완료 기준: 타격마다 흔들리고 처치 때 멈칫하며, 3배속에서도 과하지 않다.
7. **턴 배너 비차단** (A8) — `TurnManager`가 `AnnouncementUI.currentRoutine`을 기다리지 않고 바로 턴 처리로 넘어가게 하고, 배너 표시 시간을 1.0초에서 0.5초로 줄인다. 완료 기준: 턴 시작 대기가 1.6초에서 0.3초 이하로 준다.
8. **이동 이징·점프** (C2) — `PieceMoveCor`의 `DOMove(…Linear)`를 `DOJump(pos2, 0.3~0.5, 1, duration)`(OutQuad)으로 바꾸고 착지에 `DOPunchScale` 스쿼시를 넣는다. 완료 기준: 기물을 들어 올렸다 놓는 느낌이 나고 이동 시간은 그대로다.
9. **데미지 텍스트 머티리얼** (G3) — renderQueue 3002를 넣은 TMP 머티리얼 프리셋을 `DamageText.prefab`에 지정하고 `BringToFrontOfRangeHatch`를 없앤다. 완료 기준: 연속 피격 때 배치 수가 줄고, 텍스트는 여전히 범위 표시 위에 보인다.
10. **카드 일러스트 압축** (H5) — 원본을 4의 배수 크기로 다시 저장하거나(912×1179 → 912×1180) Sprite Atlas V2(내장)로 묶고, Max Size를 실제 표시 크기(512~1024)에 맞춘다. 완료 기준: Inspector의 포맷이 BC7/DXT5로 표시된다.
11. **세이브 원자적 저장** (I10) — 임시 파일에 쓴 뒤 `File.Replace(tmp, save, save.bak)`로 바꾸고(첫 저장은 Move), 불러오다 실패하면 `.bak`에서 복구한다. 완료 기준: 저장 중 강제 종료해도 세이브가 남는다.

### 2단계 — 타격감·조작감 핵심 (각 0.5~2일)

12. **카드 슬롯/비주얼 분리 + 스프링 추종** (B1·B3) — 현업에서 쓰는 구조다(Mix and Jam의 Balatro 재현과 같음). 카드 루트는 지금처럼 슬롯으로 즉시 이동하고(로직), 그래픽을 묶은 `Visual` 자식은 월드 위치를 유지했다가 `SmoothDamp`로 따라간다. 여기에 가로 속도에 비례한 기울기와 그림자를 더한다. 프로젝트의 "로직은 즉시, 연출은 나중" 원칙과 같은 방식이라 `AlignCards`는 고치지 않아도 된다. 카드 프리팹 62개의 구조를 바꿔야 한다(템플릿 `AttackCard.prefab` 수정 후 일괄 변환, 또는 Awake에서 런타임으로 감싸기). 완료 기준: 사용·취소·드로우 때 순간이동이 없다.
13. **호버 포커스** (B2, CRL-50) — 비주얼만 카드 높이의 절반만큼 올리고, 회전을 0으로, 1.25배로 키운다. 히트박스(루트)는 제자리에 있으니 포인터가 깜빡이지 않는다. 그리기 순서는 비주얼에 중첩 Canvas `overrideSorting`을 달아 올리고, 이웃 카드는 거리에 비례해 옆으로 민다. 완료 기준: 호버한 카드 전체가 가리지 않고 보인다.
14. **Feedback 레이어** (A1) — 6번의 호출들을 `CombatFeedback.Impact(공격자, 대상, 피해, 처치, 방어막흡수)` 하나로 모으고, 피해 등급별 값은 `FeedbackProfile` ScriptableObject에 둔다. 15·16·17번이 여기에 붙는다. 완료 기준: 연출 세기를 코드 수정 없이 SO에서 조정할 수 있다.
15. **히트 플래시·넉백** (D3·A5) — 맞은 렌더러에 MaterialPropertyBlock으로 `_EmissionColor`를 0.08초 동안 흰색으로 올린다. 머티리얼의 Emission은 켜 두고 기본값은 검정으로 둔다(sin.fbx처럼 모델에 내장된 머티리얼이면 먼저 Extract). 타격 방향으로 `DOPunchPosition`을 준다. 완료 기준: 맞은 기물이 번쩍이고 밀린다.
16. **데미지 숫자·고스트 HP바·풀링** (A6·A7·G4) — 내장 `UnityEngine.Pool.ObjectPool`로 텍스트를 재사용하고, 팝·좌우 흔들림·종류별 색을 넣는다. `PieceGaugeItem`에는 잔상 Image(사용자 배치)를 두고 늦게 줄어들게 한다. 완료 기준: 숫자가 겹치지 않고 Instantiate가 없다.
17. **사운드 기반** (F1~F4) — AudioMixer 에셋(Master/BGM/SFX/UI, 노출 파라미터)을 만들고, AudioManager에 BGM 소스 2개(크로스페이드), SFX 소스 풀, 피치 ±5~10%, 클립별 쿨다운을 넣는다. BGM 클립은 따로 준비한다. 완료 기준: 그룹별로 볼륨을 조절할 수 있고 다중 타격 소리가 뭉개지지 않는다.
18. **결과 미리보기** (B5, CRL-51) — 타겟팅 중 호버한 대상 위에 예상 피해(카드 설명용 `Card.EffectiveDmg` 계산 + `ModifyIncomingAttackDamage` + 방어막 차감)와 처치 아이콘을 띄운다. 이동공격이면 도착 칸을 고스트로 보여 준다(`GetAdjacentLocation`). 완료 기준: 드롭하기 전에 결과가 숫자로 보인다.
19. **베지어 타겟 화살표** (B4) — `CardDragArrow`를 이차 베지어 점 세그먼트로 바꾸고, 대상 유효성(`IsValidDragTarget`/`IsValidDropPos` 재사용)에 따라 색을 바꾸고, 유효 기물에 스냅한다. 완료 기준: 유효·무효가 색으로 구분된다.
20. **애니메이터 정리** (C3·C6·D4, CRL-49) — Die/Charge/Stun 상태에 DoubleL 팩 클립을 연결하고, `Animator.StringToHash`와 Animator 컴포넌트를 캐시한다. Apply Root Motion을 끄거나 InPlace 클립을 쓴다. 사망은 디졸브 Shader Graph로 `DeathCor`의 1초 대기를 대신한다. 완료 기준: 사망·기절이 동작으로 보이고 기물이 칸을 벗어나지 않는다.
21. **단축키·연출 가속** (B6·B9) — 프로젝트 전역으로 등록된 Input Actions에 액션 맵을 추가한다. E/Enter 턴 종료, 숫자키 카드 선택, Tab 기물 전환, Esc 취소, Space를 누르고 있으면 일시 3배속. `Mouse.current` 4곳도 바꾼다. 완료 기준: 마우스 없이 한 턴을 진행할 수 있다.
22. **월드 HP바·상태 아이콘** (B8, CRL-25·CRL-43) — 기물 위에 HP/방어막 바와 상태 아이콘(남은 턴, 무덤 수)을 띄운다. 프리팹은 에디터에서 직접 배치한다. 완료 기준: 정보창을 열지 않고도 상태를 알 수 있다.

### 3단계 — 개발 생산성 기반 (반복 작업 시간 절감)

23. **콘텐츠 검증기** (H11) — 에디터 메뉴 하나로 다음을 검사한다: effectRange가 빈 카드, 클래스명≠프리팹명, Database 미등록, 보상 풀·기본 덱에 있는 없는 이름(CRL-30·31), LevelData 배치 이름 누락, RangeInfo 조회 실패(CRL-36), 공격 클립의 Animation Event 누락(C9), 별칭 표의 옛 이름이 남은 곳. 완료 기준: 위반 목록이 출력된다.
24. **카드 목록 자동 생성** (H10) — 23번과 같은 코드로 Database.prefab의 카드를 직업 풀별로 묶어 `전사_소환사_카드목록.txt`를 만든다. 수치가 Awake에서 정해지므로 에디터에서 프리팹을 잠깐 Instantiate해 읽고 Destroy한다. 완료 기준: txt를 손으로 고칠 일이 없다.
25. **디버그 콘솔** (I9) — `UNITY_EDITOR || DEVELOPMENT_BUILD`에서만 쓰는 IMGUI 패널(씬 배치 불필요): 카드 추가(`AddCardDuringCombat`), 에너지·HP 설정, 적 처치, 층 이동, 유물 지급, 배속. 완료 기준: QA 사전조건을 1분 안에 만든다.
26. **asmdef + 자동 테스트** (I1·I2) — Runtime/Editor/Tests(EditMode·PlayMode) asmdef를 만들고, DOTween은 Utility Panel의 ASMDEF 생성 기능을 쓴다. 피해 계산(콜대미지→취약→방어막)을 순수 함수로 빼서 EditMode에서 테스트하고, PlayMode에서는 기물 배치→효과 실행 직후 상태를 확인한다(QA 4·5·7장부터, 테스트 이름에 TC-ID). CLAUDE.md의 `dotnet build` 경로도 고쳐야 한다. 완료 기준: Test Runner가 모두 통과한다.
27. **임포트 규칙 자동화** (H4) — 내장 Preset Manager로 폴더별 기본값을 두고, `AssetPostprocessor`가 규칙 위반(4의 배수 아님, 스테레오 효과음, 모델 Read/Write 켜짐)을 경고한다. 완료 기준: 새 에셋이 규칙대로 들어온다.
28. **.gitattributes (LFS 없이)** (H12) — `*.unity *.prefab *.asset *.mat *.anim *.controller`에 `merge=unityyamlmerge eol=lf`, 이미지·모델·오디오·폰트에는 `binary`를 지정하고, UnityYAMLMerge를 git mergetool로 등록한 뒤 `git gc`를 돌린다. LFS가 없으니 GitHub 단일 파일 제한(50MB 경고, 100MB 거부)을 염두에 두고 30번으로 폰트 용량을 줄인다. 완료 기준: 프리팹이 충돌할 때 Smart Merge가 동작한다.
29. **코드 위생** (I6·I4·I12) — `SendMessage` 4곳을 직접 호출로 바꾸고, `.editorconfig`를 두고, Unity 객체에 쓴 `?.`(UNT0008)를 점검하고, 릴리스 빌드에서 로그를 뺀다. 완료 기준: 분석기 경고가 준다.
30. **폰트 경량화** (H6) — 한글 2,350자+ASCII+기호만 2048² 정적 아틀라스에 넣고, 나머지는 Dynamic 폴백(Multi Atlas, Clear Dynamic Data On Build)으로 처리한다. TMP Settings에 기본 폰트와 폴백을 지정한다. 완료 기준: 폰트 에셋이 수 MB로 줄고 깨지는 글자가 없다.
31. **폴더·네이밍 정리** (H2·H3) — 병합을 기다리는 브랜치가 없을 때 에디터에서 한꺼번에 옮긴다(GUID 유지). 완료 기준: Assets 루트에 `_Project`와 `ThirdParty`만 남는다.
32. **CI (선택)** (I3) — GitHub Actions + GameCI로 테스트와 검증기를 자동 실행한다(Unity 패키지 추가가 아니며 라이선스 시크릿이 필요).

### 4단계 — 비주얼 완성도

33. **범위 표시 셰이더** (D1→D2, CRL-53) — 먼저 HLSL·SRP Batcher·줄무늬 안티에일리어싱으로 옮기고, 다음에 보드 전체 쿼드 1장 + N×M 데이터 텍스처로 합쳐 바깥 테두리만 그린다(`RangeOn/RangeOff` 참조 카운트를 텍스처 갱신으로). 완료 기준: 붙은 칸 사이 내부선과 renderQueue 땜질이 없어진다.
34. **환경 프로필·디오라마** (E1·E2·E3·E5) — 층별로 스카이·포그·Volume·조명·환경 프리팹·BGM을 묶은 SO, 보드에 초점을 맞춘 Bokeh DOF, 바람 흔들림 Shader Graph, 분위기 파티클. 완료 기준: 층마다 분위기가 달라진다.
35. **툰 셰이딩·외곽선** (D5) — Shader Graph 커스텀 라이팅 툰 셰이더 + 이미 있는 QuickOutline으로 선택·팀 표시. 완료 기준: 체스말과 인간형 모델의 질감이 맞는다.
36. **카메라 연출** (E6·A10·A4) — DOTween 카메라 리그로 행동하는 기물 쪽으로 살짝 이동, 마지막 처치 슬로모·줌. FOV 30~40도 검토한다.
37. **Timeline 연출** (C8, CRL-34) — 보스 등장, 패배 연출.
38. **카드 셰이더·연출** (D6·A9) — 사용 가능 카드 글로우, 희귀 카드 홀로, 소멸 디졸브, 재셔플 연출.
39. **나머지 비주얼** (D7·D8·E4·E7·E8·E9) — 비네트 펄스, VFX 체계, 조명 굽기, 보드 비주얼, 맵 화면, HDR 색 보정.

### 5단계 — 중장기 구조

40. **데이터 ID 정식화** (H7) — 이름 대신 바뀌지 않는 `id` 필드를 쓰고 3번 별칭 표를 확장한다.
41. **시드 RNG** (I11) — 재현 가능한 런과 데일리 런.
42. **런 통계** (J1) — 카드 선택·사용·피해 지표로 밸런싱.
43. **구조 리팩터링** (I15) — CardCanvas Model/View 분리, Board.CardEffect 효과별 핸들러(12·13번 뒤에 자연스럽게).
44. **카드 데이터 주도화** (H9) — ScriptableObject 또는 스프레드시트 임포터.
45. **Enter Play Mode 최적화** (I5) — 26번 테스트를 갖춘 뒤.
46. **기타** — 씬 전환 Async+페이드(G11), `Awaitable`(I7), 도메인 이벤트(I8), Build Profiles(I13), 씬 UI 프리팹 분리(I14), 접근성(J3), 미세 최적화(G5·G6·G7·G9·G10, 프로파일러로 근거를 확인한 뒤).

---

## 3. 의존 관계

- 14(Feedback 레이어) → 15·16, 17의 타격음, 36의 처치 연출
- 12(슬롯/비주얼 분리) → 13(호버), 드래그 기울기
- 23(검증기)과 24(카드 목록)는 같은 코드를 공유 / 23 → 32(CI)
- 26(테스트) → 43(리팩터링)·45(Play Mode 최적화)를 안전하게 만든다
- 33은 D1 → D2 순서
- 34(환경 프로필) → 바이옴별 BGM(17), LUT(39)
- 28(LFS 없이 운영) → 30(폰트 경량화)으로 큰 파일을 줄여야 한다

## 4. 보류 — 새 패키지·LFS가 필요한 것과 대안

| 기법 | 필요한 것 | 지금 쓸 대안 |
|---|---|---|
| 카메라 Impulse·리그 | Cinemachine | trauma 흔들림을 직접 구현 + DOTween 카메라 리그 |
| 대상 바라보기 | Animation Rigging | LateUpdate에서 머리 본을 직접 LookAt |
| 툰 셰이더 | Unity Toon Shader | Shader Graph 커스텀 라이팅(URP에 포함) |
| 메모리·성능 분석 | Memory Profiler·Profile Analyzer·Project Auditor | 내장 Profiler·Frame Debugger·Rendering Debugger |
| async 확장 | UniTask | Unity 6 `Awaitable` |
| 콘텐츠 로딩·다국어·오디오 미들웨어·크래시 리포트 | Addressables·Localization·FMOD/Wwise·Sentry | 필요해질 때 다시 검토 |
| 대용량 파일 관리 | Git LFS | `.gitattributes`로 Smart Merge·줄바꿈만 설정하고 큰 파일은 줄인다 |

---

## 5. 전체 카탈로그 (A~J)

표기: `(난이도·효과)` — 난이도 하/중/상, 효과 ★~★★★. 📦는 새 패키지가 필요해 보류한 부분(4절 대안 참고).

### A. 타격감·연출 조율 (Game Feel)

- **A1 임팩트 이벤트를 한 곳으로 모으기 (Feedback 레이어)** — 지금은 Piece/Board 곳곳에서 AudioManager·PieceCanvas를 직접 부른다. 시전자 Animation Event 순간(`PlayCasterAndTargetReaction`의 targetReaction 시작)을 `Impact(공격자, 대상, 피해, 처치, 방어막흡수)` 이벤트로 발행하고, 카메라·사운드·VFX·히트스탑이 구독한다. 피해 등급별 값은 `FeedbackProfile` SO에 둔다(More Mountains FEEL의 MMF_Player 개념을 가볍게 직접 구현). (중·★★★)
- **A2 CameraFX 연결** — 흔들림 세기는 피해/최대HP 비율, 처치·다중타 마지막 타에 히트스탑 40–80ms, 아군 피격에 붉은 플래시. 흔들림 방식은 trauma²×Perlin 노이즈(위치+회전)로 바꾸는 것을 검토한다. 📦 Cinemachine Impulse 대신 직접 구현. (하·★★★)
- **A3 히트스탑 범위** — 80ms 이하는 지금처럼 전역 timeScale로 충분하다. 더 긴 슬로모는 공격자·대상의 Animator.speed만 멈춰 UI 반응을 유지한다. (하·★)
- **A4 마지막 적 처치 연출** — 0.3배속 0.5초 + 카메라 살짝 줌인(`GameManager.RemoveEnemy`에서 남은 적이 0이 되는 순간). (하·★★)
- **A5 피격 리액션** — 타격 방향 넉백(DOPunchPosition), 흰색 히트 플래시(D3), 스쿼시. 방어막에 막힌 타격은 다른 소리와 파란 숫자로 구분한다. (하·★★)
- **A6 데미지 숫자** — 팝(1.4→1.0 OutBack), 좌우 흔들림·포물선으로 겹침 방지, 종류별 색·크기(피해/독/화상/회복/방어막 흡수), 다중 타격 누적 표시. (하·★★)
- **A7 지연 HP바(고스트 바)** — 빨간 바는 즉시 줄고 흰 잔상 바가 0.3초 뒤 따라 내려온다. (하·★★)
- **A8 턴 전환 템포** — 배너가 진행을 막지 않게 하고 0.8초 안팎으로 줄이며, 클릭하면 건너뛴다. (하·★★★)
- **A9 카드 연출** — 사용 확정 시 펀치 스케일·글로우, 소멸은 불타 사라지는 디졸브(D6), 버림 더미를 덱으로 다시 섞을 때 카드가 모이는 연출(지금은 즉시 처리). (중·★★)
- **A10 적 턴 가독성** — 행동하는 적을 외곽선으로 강조하고 카메라를 살짝 옮긴 뒤 0.15초 예비동작 후 행동한다. (중·★★)

### B. 조작감·UX

- **B1 손패 스프링 레이아웃** — 카드는 목표 슬롯만 정하고, 실제 위치는 감쇠 스프링으로 따라간다. 슬롯/비주얼 분리(우선순위 12번)가 가장 적은 변경으로 구현하는 방법이다. (중·★★★)
- **B2 호버 포커스** — 호버 카드는 회전 0·리프트·1.25배, 이웃 카드는 옆으로 벌린다(Slay the Spire·Balatro). 그리기 순서는 중첩 Canvas(overrideSorting)로 올리고, 히트박스는 제자리에 둬서 포인터 깜빡임을 막는다. (중·★★★)
- **B3 드래그 감각** — 스프링 지연으로 따라오고, 가로 속도에 비례해 기울고, 그림자가 어긋나게 진다. (하·★★)
- **B4 타겟팅 화살표** — 베지어 곡선(점·세그먼트 스프라이트, 접선 방향 화살촉), 상태별 색(기본/유효/무효/사거리 밖), 유효 기물에 스냅, UV 흐름 애니메이션 ([CardDragArrow.cs](../Assets/Scripts/CardDragArrow.cs)). (중·★★)
- **B5 결과 미리보기 (Into the Breach식)** — 타겟팅 중 예상 피해, 고스트 HP 구간, 처치 아이콘, 방어막 흡수량, 이동공격 도착 칸 고스트, 범위 내 피격 기물 강조. (중·★★★)
- **B6 키보드 단축키·Input Actions** — 턴 종료·카드 선택·기물 전환·취소·연출 가속 단축키, 나중에 리바인딩 UI. (중·★★)
- **B7 키워드 툴팁** — 카드 키워드(독·화상·기절·취약·도발·무덤·이동공격)에 TMP `<link>` 태그를 달고 `TMP_TextUtilities.FindIntersectingLink`로 호버를 감지해 설명을 띄운다. 기물 상태 아이콘에도 툴팁. (중·★★)
- **B8 월드 HP바·상태 아이콘** — 기물 위 HP/방어막 바, 상태 아이콘(남은 턴), 무덤 수. (중·★★★)
- **B9 연출 스킵·가속** — 적 턴에 클릭하거나 Space를 누르고 있으면 일시 3배속(GameSpeedManager 확장). (하·★★)
- **B10 커서 상태** — 기본/타겟팅/무효 커서(`Cursor.SetCursor`). (하·★)
- **B11 설정 메뉴** — 볼륨, 흔들림 세기·플래시 끄기, 기본 배속, 창 모드·해상도(지금은 크기 조절 꺼짐, 전체화면 창 고정). (중·★★)

### C. 애니메이션 조합

- **C1 레이어 규약** — 뼈대 애니(Animator: 몸 동작), 절차적 트윈(DOTween: 보드 위 이동·넉백·스쿼시), 셰이더(플래시·디졸브), 파티클, 카메라, UI를 임팩트 이벤트 하나에 맞춘다. README에 규약을 적는다. (하·★★)
- **C2 체스 기물다운 이동** — 예비동작(0.08초 웅크림), 포물선 들어올림(DOJump, 나이트형은 더 높게), 착지 스쿼시와 먼지. (하·★★★)
- **C3 Animator 정리** — 빈 Die/Charge/Stun 상태 채우기, `StringToHash`·Animator 캐시. (하·★★)
- **C4 Animator Override Controller** — 공용 상태머신 하나에 기물별 클립만 바꿔 끼운다. (하·★★)
- **C5 가산(Additive) 피격 레이어** — 상체 Avatar Mask 레이어로 움찔 동작을 덧입힌다. (중·★)
- **C6 루트 모션 정책** — 위치는 절차적 트윈이 맡는다. InPlace 클립 또는 Apply Root Motion 끄기·XZ Bake Into Pose. (하·★★)
- **C7 대상 바라보기** — 공격·타겟팅 중 머리와 상체가 대상을 본다. 📦 Animation Rigging 대신 LateUpdate에서 본 회전. (중·★)
- **C8 Timeline 시퀀스** — 보스 등장, 희귀 카드 컷인, 승리·패배 연출. Signal Track으로 게임 로직과 연결(timeline 패키지는 설치돼 있음). (중·★★)
- **C9 Animation Event 검사** — 공격 클립에 OnAnimationEvent가 빠지면 1.2초 폴백 대기가 생기므로, 빠진 클립을 찾는 에디터 검사. (하·★★)

### D. 셰이더·VFX

- **D1 RangeHatch URP 이식** — HLSLPROGRAM + `CBUFFER_START(UnityPerMaterial)`, `fwidth`/smoothstep 안티에일리어싱. (하·★)
- **D2 보드 오버레이 통합 셰이더** — 보드 전체 쿼드 + N×M 데이터 텍스처, 바깥 테두리만(XCOM·Into the Breach 방식), 겹침 표현. (중·★★★)
- **D3 히트 플래시** — MaterialPropertyBlock으로 흰색 보간. MPB를 쓰면 해당 렌더러는 SRP Batcher에서 빠지지만 기물 수가 적어 괜찮다. (하·★★★)
- **D4 사망 디졸브** — 노이즈 임계값 + 발광 테두리 Shader Graph, 영혼 파티클. (하·★★)
- **D5 툰 셰이딩·림라이트·외곽선** — 로우폴리 체스말(BrokenVector)과 인간형 모델(sin.fbx)+무기 팩의 질감을 공용 툰 셰이더로 맞춘다. 📦 Unity Toon Shader 대신 Shader Graph. 외곽선은 이미 있는 QuickOutline(URP 동작 확인 필요). (중·★★★)
- **D6 카드 셰이더** — 사용 가능 카드 테두리 글로우(지금은 CanvasGroup.interactable만 바뀜), 희귀 카드 홀로·포일(Balatro식), 소멸 디졸브, 호버 광택 스윕. UI 마스킹(stencil) 호환 필요. (중·★★)
- **D7 풀스크린 효과** — 아군 피격·저체력 붉은 비네트 펄스. Volume 가중치 트윈이 가장 간단하고, 아니면 Full Screen Pass Renderer Feature + Fullscreen Shader Graph. Unity 6.3은 Render Graph 전용이라 직접 만드는 패스는 RecordRenderGraph가 필요하므로 되도록 Full Screen Pass를 쓴다. (중·★★)
- **D8 VFX 체계** — 피해 유형별 타격 파티클, 상태별 지속 파티클(독 거품·화상 불씨·기절 별), 방어막은 프레넬 버블 셰이더. 모두 풀링. (중·★★)
- **D9 셰이더 워밍업** — GraphicsStateCollection(Unity 6)이나 ShaderVariantCollection을 미리 불러와 첫 이펙트 끊김을 막고, 쓰지 않는 변형은 뺀다. (중·★)

### E. 배경·조명·카메라

- **E1 환경 프로필 SO** — 스카이박스·포그·Volume 프로필·주광 색과 각도·환경 프리팹·BGM을 층마다 묶어 바이옴 진행(숲→폐허→성). `LevelDatabase.floorPools`와 같은 단위. (중·★★★)
- **E2 디오라마 연출** — 보드를 테이블이나 섬 디오라마처럼, 배경은 포그로 흐리게, Bokeh DOF로 틸트시프트 느낌. (중·★★★)
- **E3 하늘** — 기본 절차적 스카이박스 대신 팔레트에 맞춘 그라디언트 스카이 셰이더. (하·★★)
- **E4 조명** — 정적 배경은 라이트맵 + Adaptive Probe Volumes, 기물만 실시간 그림자, 주광 쿠키로 숲 그늘 얼룩, 바이옴별 색온도. (중·★★)
- **E5 생동감** — 나무·풀 바람 흔들림(정점 셰이더), 반딧불·낙엽·먼지 파티클, 구름 그림자. (하·★★)
- **E6 카메라** — 행동 기물 포커스, 보스 등장 돌리, 약한 대기 흔들림, FOV 30~40으로 보드 왜곡 줄이기 검토. 📦 Cinemachine 대신 DOTween 리그. (중·★★)
- **E7 보드 비주얼** — 보드 테두리 프레임, 월드 좌표 노이즈로 칸 색 변화, 호버 칸 발광. 카메라가 고정이면 Terrain을 정적 메시로 바꾸고 Static 배칭. (중·★★)
- **E8 맵 화면** — 양피지 배경과 패럴랙스, 경로 선 그리기 애니메이션, 선택 가능 노드 펄스. (하·★★)
- **E9 색 보정** — HDR이 켜져 있으니 Color Grading Mode를 HDR로, 바이옴별 LUT와 Volume 블렌딩. (하·★)

### F. 사운드

- **F1 AudioMixer** — Master/BGM/SFX/UI 그룹과 노출 파라미터, 설정 슬라이더·PlayerPrefs 저장, 스냅샷 덕킹. (하·★★★)
- **F2 BGM 매니저** — 맵/전투/상점/휴식/보스 트랙 크로스페이드, 바이옴별 곡. (하·★★★)
- **F3 SFX 재생기** — AudioSource 풀, 변주 배열, 피치·볼륨 랜덤, 동시발음 제한과 쿨다운. (하·★★)
- **F4 임포트 규칙** — 효과음은 Mono·Decompress On Load, BGM은 Streaming·Vorbis. (하·★)
- **F5 레이어드 타격음** — 타격 순간에 트랜지언트+바디+꼬리를 조합, 카드 종류별 소리. (중·★★)
- **F6 오디오 미들웨어** — FMOD/Wwise. 📦 현재 규모에선 Unity 믹서로 충분.

### G. 성능

- **G0 측정 먼저** — 내장 Profiler·Frame Debugger·Rendering Debugger로 개선 전후 기준선을 남긴다. 📦 Memory Profiler·Project Auditor는 보류. (하·★★)
- **G1 프레임 제한** — vSync 1 또는 targetFrameRate. (하·★★★)
- **G2 안티앨리어싱** — URP MSAA 4x(Forward+ 지원) 또는 SMAA. (하·★★)
- **G3 TMP 머티리얼** — renderQueue를 넣은 머티리얼 프리셋 사용, 런타임 `fontMaterials` 수정 제거. (하·★★)
- **G4 오브젝트 풀링** — 데미지 텍스트, 행동 예고 텍스트, 파티클(StopAction=Callback으로 반환) ([PieceEffect.cs:120](../Assets/Scripts/PieceEffect.cs#L120)). (하·★★)
- **G5 조회 캐싱** — `GetButtonScript`가 루프마다 GetComponent → `Button[,]` 캐시, `GetButtonForPiece` 전수 탐색 → Dictionary, 카드 Awake의 `GameObject.Find("CardCanvas")` 제거. (하·★)
- **G6 UI 캔버스 분리** — 자주 바뀌는 손패·에너지·게이지를 하위 Canvas로 나누고 장식 그래픽은 raycastTarget 끄기. (하·★★)
- **G7 빌보드** — `PieceCanvas.Update`의 매 프레임 LookAt을 생성 시 1회 또는 공용 매니저로. (하·★)
- **G8 렌더 설정** — 그림자 4캐스케이드·50m → 1–2캐스케이드·20–25m, 사용처 없는 Opaque Texture 끄기. (하·★★)
- **G9 DOTween 위생** — `SetLink(gameObject)`, 시작 시 `SetTweensCapacity`. (하·★)
- **G10 할당 줄이기** — Fisher–Yates 셔플, 호버마다 생기는 List·LINQ 할당 정리(Profiler GC Alloc 확인 후). (하·★)
- **G11 씬 전환** — `LoadSceneAsync` + 페이드, 장기적으로 노드 이동 시 보드만 다시 초기화. (중·★★)

### H. 에셋 관리

- **H1 정크 정리** — gakumas-local(334MB), `_Recovery` 씬, URP 템플릿 잔재, `(old)actIcon.png`, `NewPieceInfo 1.asset`, 중복 `NewLayer.terrainlayer`, `NewBrush.brush`, 팩별 Demo/Scenes 폴더. 빌드에는 참조된 에셋만 들어가므로 목적은 임포트 시간·저장소 용량·검색 잡음. (하·★★★)
- **H2 폴더 규약** — `Assets/_Project/{Art,Audio,Data,Prefabs,Scenes,Scripts/{Runtime,Editor,Tests},Settings}` + `Assets/ThirdParty/`. 이동은 반드시 에디터 안에서(GUID 유지). (중·★★)
- **H3 네이밍 규약** — `sin.prefab`, `autoally.prefab`, `White Knight 1.prefab`처럼 섞인 규칙을 문서화하고 검증기로 강제. (하·★)
- **H4 임포트 프리셋 + AssetPostprocessor** — 폴더별 Preset과 규칙 위반 경고. (중·★★★)
- **H5 텍스처** — 4의 배수 또는 Sprite Atlas V2로 BC7/DXT5 압축, 표시 크기에 맞춘 Max Size, 아이콘·프레임 아틀라스. (하·★★★)
- **H6 폰트** — 2,350자 정적 + Dynamic 폴백, TMP Settings 기본 폰트·폴백. (중·★★)
- **H7 데이터 ID 안정화** — 바뀌지 않는 `id` 필드 + 이름 변경 별칭 표. (중·★★)
- **H8 Addressables** — 콘텐츠가 커지면 직업·층 단위 로드. 📦 보류. (상·★)
- **H9 카드 데이터 주도화** — ScriptableObject(`[SerializeReference]` 효과 목록) 또는 스프레드시트 임포터. (상·★★)
- **H10 카드 목록 자동 생성** — Database.prefab에서 `전사_소환사_카드목록.txt` 생성. (하·★★★)
- **H11 콘텐츠 검증기** — 프리팹·풀·레벨·이름·Animation Event 일괄 검사, CI에서도 실행. (중·★★★)
- **H12 Git 설정** — `.gitattributes`로 Smart Merge(UnityYAMLMerge)·`eol=lf`·binary 지정, `git gc`. 📦 LFS는 쓰지 않는다. (중·★★★)
- **H13 작업 폴더 이동** — OneDrive 밖으로(Library/Temp 동기화로 생기는 임포트 지연·파일 잠금·.git 손상 위험 제거). (하·★★★)
- **H14 서드파티 라이선스 기록** — 팩별 출처·버전·라이선스를 `THIRD_PARTY.md`에. (하·★)

### I. 개발 파이프라인·코드 품질

- **I1 asmdef 분리** — Runtime/Editor/Tests, DOTween ASMDEF. 컴파일 단축, 테스트 어셈블리 가능. (중·★★★)
- **I2 자동 테스트** — Test Framework(설치돼 있음)로 EditMode 순수 계산 + PlayMode 상태 검증, TC-ID 연결. (상·★★★)
- **I3 CI** — GitHub Actions + GameCI, PR마다 테스트·검증기, 야간 빌드. (중·★★)
- **I4 정적 분석** — `.editorconfig`, IDE에 포함된 Unity 분석기 경고(UNT0008: Unity 객체에 `?.`). (하·★★)
- **I5 Enter Play Mode 최적화** — 도메인 리로드 끄기 + static 20여 곳을 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`로 초기화. 테스트를 갖춘 뒤. (중·★★)
- **I6 SendMessage 제거** — TurnManager→Board 4곳을 인터페이스 호출로. (하·★)
- **I7 비동기** — 새 시스템은 Unity 6 `Awaitable`. 📦 UniTask는 보류. 잘 돌아가는 코루틴 큐는 그대로. (중·★)
- **I8 도메인 이벤트** — `OnDamageDealt/OnPieceDied/OnCardPlayed/OnTurnStarted` 발행, UI·FX·사운드·통계가 구독. (중·★★)
- **I9 디버그 콘솔·치트** — 개발 빌드 전용 런타임 패널. (중·★★★)
- **I10 세이브 견고성** — 원자적 교체, `.bak`, `saveVersion` + 단계별 마이그레이션 ([DataManager.cs:50-55](../Assets/Scripts/DataManager.cs#L50-L55)). (하·★★)
- **I11 시드 RNG** — 런 시드 저장, 맵·상점·보상·셔플·AI별 `System.Random`. (중·★★)
- **I12 로그 관리** — `[Conditional]` 래퍼로 릴리스에서 제거. (하·★)
- **I13 빌드 자동화** — Unity 6 Build Profiles(Dev/Release), companyName·버전 정리, 빌드 스크립트. (하·★)
- **I14 씬 충돌 줄이기** — MainScene(328KB)의 UI를 중첩 프리팹으로. (중·★)
- **I15 구조 리팩터링** — CardCanvas(1,346줄) Model/View, Board.CardEffect(1,201줄) 효과별 핸들러. (상·★★)

### J. 운영·밸런싱

- **J1 런 통계** — 제시·선택 카드, 카드별 사용 횟수·피해, 도달 층, 사망 원인을 로컬 JSON/CSV로. (하·★★)
- **J2 로컬라이제이션** — 📦 Localization 패키지, 해외 출시 계획 시. (상·★)
- **J3 접근성** — 색약 대응 팔레트, 텍스트 크기, 흔들림·플래시 끄기. (하·★★)
- **J4 크래시 리포트** — 📦 출시 단계에서 검토. (하·★)

---

## 6. 참고 자료

- [Mix and Jam의 Balatro 카드 움직임·셰이더 재현 (80.lv)](https://80.lv/articles/balatro-s-card-movements-shaders-recreated-in-unity) — 카드 비주얼 분리·기울기
- [Render Graph Updates in Unity 6.3](https://discussions.unity.com/t/render-graph-updates-in-unity-6-3/1668122)
- [URP 커스텀 후처리 (Full Screen Pass)](https://docs.unity3d.com/Manual/urp/post-processing/post-processing-custom-effect-low-code.html)
- [Project Auditor (6.3은 패키지)](https://docs.unity3d.com/Manual/com.unity.project-auditor.html)
- [Awaitable과 UniTask 비교](https://dev.to/gamedevtoollab/do-we-still-need-unitask-in-the-unity-6-era-choosing-between-awaitable-and-unitask-5d6)
- [텍스처 임포트·압축 (Unity 6.2 Manual)](https://docs.unity3d.com/6000.2/Documentation/Manual/ImportingTextures.html)
- GDC 강연(제목으로 검색): "Juice it or lose it"(2012), "The Art of Screenshake"(2013), "Math for Game Programmers: Juicing Your Cameras With Math"(2016), "'Into the Breach' Design Postmortem"(2019), "Slay the Spire: Metrics Driven Design and Balance"(2019)
