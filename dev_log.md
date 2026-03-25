# SuperCent Assignment — 개발 기록 (통합본)

> 시간 순서대로 정리한 전체 개발 히스토리. 반복 내용 제거 후 통합.

---

## 2026-03-21 · 프로젝트 초기 설정 & 씬 기초 구축

### 프로젝트 초기화
- Unity URP 프로젝트 생성, `.gitignore` / `.vsconfig` / `README.md` 추가
- 20개 C# 프로토타입 스크립트 작성 (GameManager, PlayerController, MiningGrid 등)

### 게임 시스템 전체 설계 (update.txt 기반)
전체 게임플레이 루프 구현:
- **채굴** : 7×20 돌 그리드, 채굴 시 사라짐 → 6초 뒤 리스폰
- **스택** : 돌 10개 MAX, 가득 차도 채굴 계속 (스택만 안 쌓임)
- **변환기** : 돌 → 수갑 변환 (돌 빨려가듯 입력 → ~2초 뒤 1:1 출력)
- **Desk** : 수감자에게 수갑 판매, 수갑 1개 = 10원 생성
- **수감자 AI** : 스포너 생성 → Desk 앞 대기(최대 2명) → 수갑 4개 받으면 감옥 이동
- **감옥** : 기본 20명, 업그레이드 시 100명
- **업그레이드 목록** :
  - 드릴 (20원) : 가로 3칸, 딜레이 없음, 스택 20개
  - 드릴 차 (50원) : 가로 5칸, 스택 30개
  - 인력 고용 (40원) : 자동 채굴 NPC 3명 (각 1열 왕복)
  - 자동 판매 (50원) : 수갑 10개씩 자동 판매 NPC
  - 감옥 확장 (50원) : 수용 20 → 100명

### SceneBuilder EditorScript 구축
- `place.png` 기반 씬 자동 배치 EditorScript 작성
- 채석장 세로 배치 / 감옥 왼쪽 입구 / 펜스 그룹화 / 확장 감옥 배치
- `renderer.material` → `sharedMaterial` 수정 (material leak 경고 제거)
- `MiningGridGroup` 90° 회전, 입구 펜스 제거
- `CameraFollow` 컴포넌트 추가, Player에 연결

### 씬 세부 조정
- 돌 그리드 방향 수정 + 아이소메트릭 카메라 뷰 적용
- Visual Grid 마커 + FIFO 감옥 밖 대기 줄 구현
- 외곽 경계 벽 추가
- 카메라 줌, 빌보드 라벨, 머니 HUD 추가
- 수갑/돈 픽업존에 스택 쌓기 구현
- 플레이어 바닥 침몰 버그 수정, 돌 생성 위치/타이밍 수정

---

## 2026-03-22 · UI 디자인 & 비주얼 개선

### UI 에셋 적용
- 업그레이드 이미지, HUD 타이틀/머니/사운드 버튼 적용
- UpgradeImage Quad: 빌보드 → 바닥 방향으로 변경
- 업그레이드 존 플랫폼/화살표 제거, FillBar + CostText 레이아웃 수정
- `getmoney.png` → MoneyPickupZone 바닥 Quad에 적용

### 한국어 폰트
- Malgun Gothic TMP 폰트 적용 (한글 렌더링 수정)

### 존 아웃라인
- OreDropZone / DeskZone에 점선 바닥 아웃라인 추가 (`ZoneDashedOutline.cs`)

### 버그 수정
- 돈 날아가는 애니메이션 수정
- MarkerVis 기반 경로 시스템 (씬에서 위치를 직접 드래그해서 조정 가능)
- 수감자 감방 스택 위치 수정

---

## 2026-03-23 · FBX 모델 임포트 & 캐릭터 설정

### FBX 모델 임포트
- 돌, 수갑, 돈, 감옥(jail1) 모델 임포트 + URP Lit 머티리얼 적용
- `RockPrefab` : 큐브 메쉬 → 돌 FBX로 교체
- `jail1.fbx` → `Prison_20`에 배치

### 스택 아이템 비주얼 조정
- 돌 × 1.2, 스택 아이템 × 2 스케일 업
- 수갑/돈 비주얼 조정, 돈 수평 스태킹으로 변경

### 펜스 콜라이더 & 플레이어 모델
- 외곽 펜스 콜라이더 추가
- `playcharacter` FBX를 Player Visual에 적용

### FBX 캐릭터 모델 → NPC 프리팹 적용
- AutoSellNPC, WorkerNPC, PrisonerAI(before/after) 프리팹에 Mixamo 캐릭터 모델 적용
- `prisoner_after` 모델 스왑 구현 (수갑 4개 받으면 비주얼 교체)

### 캐릭터 회전 수정
- 모델 X 회전 -90° 적용해 똑바로 세우기 (처음엔 뒤집혀 있었음)
- `PlayerCharacter_Visual`의 구 MeshRenderer 제거
- Visual 자식 오브젝트의 scale/rotation 정리

### 플레이어 애니메이션
- 플레이어 idle/walk/run 애니메이션 적용
- `main_run2` 클립을 run 상태에 연결
- 이동 방향으로 즉시 스냅 회전 구현 (부드러운 회전 → 즉시 회전)

---

## 2026-03-23 · 드릴 비주얼 & 곡괭이 애니메이션 & 사운드

### 드릴 비주얼
- `DrillToolPrefab` 을 `PlayerToolManager.toolModels[1]`에 연결 (미연결로 업그레이드 후 드릴 안 보이던 버그 수정)
- `BulldozerModePrefab` 연결

### 곡괭이 타이밍 & 본 부착
- `MiningAnimNotifier` 스크립트 추가: 애니메이션 ~69% 시점에 `OnMiningImpact` 이벤트 발생
- 곡괭이 모델을 `mixamorig:RightHand` 본에 런타임 부착 (채굴 중에만 표시)
- 채굴 타이밍을 애니메이션 임팩트 프레임에 맞춤

### UpgradeZone Cost UI 버그 수정
- `requiredCost=0` 버그: `Start()`에서 GameManager가 미초기화 → Lazy init으로 수정
- `SpendCash()` 반환값 무시로 돈 없어도 소모되던 버그 수정

### RockNode 사운드 & 파티클
- 채굴 사운드 재생 + 파티클(나이아가라) 지원 추가

### 수감자 Y 위치 수정
- `PRISONER_Y_OFFSET = 0.85f` → `0f` (pivot이 발 위치이므로 바닥에서 떠있던 버그 수정)

---

## 2026-03-23 · AutoSell NPC & WorkerNPC 애니메이션 & 감옥 그리드

### AutoSellNPC 플레이어 모델 교체
- `AutoSellNPCPrefab`: `main_idle.fbx` 리깅 캐릭터 + `PlayerAnimController` 적용
- `AutoSellNPC.cs`: `WalkTo()` 중 Speed 파라미터로 walk/idle 구동
- `PixaPickaxe` 플레이어와 동일한 transform으로 부착

### WorkerNPC 애니메이션
- `miner_mining.fbx`, `miner_Walk.fbx` → `Assets/Models/Animations/`에 임포트
- `WorkerAnimController` 생성: Idle/Walk/Mining 상태, Speed/IsMining 파라미터
- 채굴 중 `IsMining=true`, 이동 중 `Speed=1`, 정지 시 `Speed=0`
- `PixaPickaxe` 동일 transform으로 부착

### 감옥 수감자 그리드 — 씬에서 직접 조정 가능
- `CellManager.cs`: `OnDrawGizmos` → `OnDrawGizmosSelected` (선택 시에만 표시)
- `stackPoints[]` 배열: Inspector에서 Transform 마커를 이동하면 실제 스택 위치 반영
- `CellManagerEditor`: "스택 포인트 마커 생성" 버튼으로 씬에 실물 마커 생성

---

## 2026-03-25 · 비주얼 & 애니메이션 오버홀 (이전 세션 기록 포함)

### T-포즈 / 애니메이션 미작동 근본 원인 해결
**원인**: Animator 컴포넌트가 Visual(부모)에 있고 본은 `main_idle`(자식) 하위에 있을 때,
애니메이션 클립의 본 경로(`mixamorig:Hips/...`)가 Animator 기준으로 매핑되지 않아 T-포즈 발생.
**해결**: Animator를 본이 직속 자식인 FBX 루트(`main_idle`)로 이동.

#### AutoSellNPCPrefab 수정
- Animator를 `Visual/main_idle`로 이동
- `tripo_node_ac6c3113` SMR 활성화
- `main_idle` scale=(2,2,2), euler=(270,0,0)

#### WorkerNPCPrefab 수정
- `main_idle.fbx`(경찰 FBX) 인스턴스 → `miner_Walk.fbx`(실제 마이너) 인스턴스로 교체
- Animator를 `main_idle`(FBX 루트)로 이동
- `tripo_node_5282d6c8` SMR 활성화 (마이너 메쉬)
- `MinerVisual` 정적 메쉬 비활성화
- `PixaModel` (scale=35,35,35) 부착 유지

### 불도저 채굴 범위 확장
- `MineAt()` 메서드에 `rearDepth` 파라미터 추가
- `GameSettings`: `bulldozerRearDepth = 2` 추가
- 불도저 채굴 시 앞방향 기준 뒤쪽 2행 추가 채굴

### 수감자 스폰 타이밍 수정
- 기존: 2명 동시 스폰 → 수정: 1명 즉시 스폰 + 3초 후 2번째 스폰
- `PrisonerSpawner`: `TrySpawn()` + `StartCoroutine(SpawnWithDelay())` 패턴
- `GameSettings`: `prisonerSpawnInterval = 3f` 추가

### WorkerNPC WorkRoutine 재작성
- 돌 확인 먼저 → stopPos 계산 → 이동 → 채굴 순서로 재구성
- 빈 행: 이동 없이 row 카운터만 증가 (뒷걸음 방지)
- stopPos = `rockPos - approachDir * (spacing * 1.8f)` (대형 캐릭터 스케일 고려)

### miner_Walk 루프 수정
- `miner_Walk.fbx` 임포트 설정: `loopTime=false` → `true`
- 이동 중 걷기 애니메이션 끊김 없이 루프 재생

### 채굴 애니메이션 전체 1회 재생
- `MiningStateHash`(shortNameHash) 도입: Mining 상태 실제 진입 후 normalizedTime 측정
- `normalizedTime >= 0.69f`에서 임팩트 발생, `>= 0.97f`에서 `IsMining=false`
- (이전: 69% 시점 즉시 종료 → 수정: 전체 1회 재생 후 종료)

### 말풍선 텍스트 수정
- `"No Cell!"` → `"no\ncell!"` (두 줄, 소문자, 폰트 사이즈 30 → 18)

### 수감자 외부 대기 스폰 제한
- `CellManager`: `OutsideQueueCount` 프로퍼티 추가
- `PrisonerSpawner`: 외부 대기 4명 이상이면 추가 스폰 차단

### WorkerNPC `animator.speed`
- `2f` → `1f` (애니메이션 자연 속도 재생)

---

## 2026-03-25 · 감옥 시스템 & UI & 환경

### 감옥 꽉 찼을 때 jail2 교체 비주얼
- `jail2` 모델 (`modeling/object/jail2/base.fbx`) → `Assets/Models/jail2/`로 임포트
- `Jail2_Visual` 씬에 생성: `Jail1_Visual`과 동일 transform (pos/rot/scale)
- `CellManager.RefreshJailVisual()`: 만석 시 jail1 OFF / jail2 ON, 확장 시 다시 jail1으로 복귀

### 감옥 만석 → PrisonExpand 업그레이드 활성화 + 카메라 팬
- `CellManager`: `prisonExpandUpgrade` (UpgPrison 연결), `cameraFollow` 레퍼런스 추가
- 20명 만석 최초 1회: `UpgPrison` 활성화 + 카메라가 업그레이드 존으로 0.6초 이동 → 2초 유지 → 플레이어 복귀
- `CameraFollow`: `PanToAndReturn(Vector3, float)` 코루틴 추가
- `UpgDrillCar.objectsToActivateOnComplete`에서 `UpgPrison` 제거 (자동 활성화 차단)
- `UpgradeZone.MeetsPrerequisite`: `PrisonExpand`의 `level >= 2` 조건 → `true` (조건 없음)

### CellManager 격자 방향 Inspector 조정
- `stackGridRotationY = 90f` (Y축 기준 격자 회전)
- `stackGridColumns`, `stackGridSpacing` Inspector 노출
- Gizmo: 열 방향(녹색 화살표), 행 방향(청록 화살표), 슬롯 번호 표시
- `CellManagerEditor`: 마커 생성 시 `stackGridRotationY` 반영

### 다이나믹 조이스틱 (터치 위치에 생성)
- `VirtualJoystick.cs` 재작성: 터치 위치에 `joystickBackground` 이동 후 활성화, 손 떼면 숨김
- `JoystickTouchArea`: Canvas 하위 전체화면 투명 Image + VirtualJoystick 컴포넌트 부착
- `JoystickBackground`: 기본 비활성, 터치 시 해당 위치에 표시

### 사막 바닥 머티리얼
- `desert_floor_mat.mat` 생성: Standard / RGB(210,180,120) / Smoothness=0.1
- `outsidefloor` 오브젝트에 적용

### 해상도 & 오리엔테이션
- `PlayerSettings`: 기본 해상도 720×1280 설정
- Game 뷰 테스트: Window → Game → 해상도 드롭다운 → "+" → 720×1280 Fixed Resolution 추가

---

## 주요 아키텍처 패턴

| 패턴 | 설명 |
|---|---|
| **MarkerVis** | 씬 오브젝트에 `MarkerVis` 자식 추가 → 디자이너가 드래그해서 경로 조정 |
| **MiningAnimNotifier** | Animator 같은 GO에 부착, normalizedTime 폴링으로 임팩트 타이밍 이벤트 발생 |
| **DropZone.WorkerDeliverOre** | WorkerNPC가 채굴 시 OreDropZone에 직접 전달 |
| **CellManager.stackPoints[]** | Inspector Transform 배열로 수감자 위치 씬에서 직접 조정 |
| **Animator 경로 규칙** | Animator 컴포넌트는 반드시 본이 직속 자식인 FBX 루트 노드에 위치 |
| **PrisonerSpawner FIFO** | 데스크 대기 큐 + 외부 감옥 대기 큐 분리, 각각 최대 인원 제한 |

---

## 씬 주요 오브젝트 위치 (런타임)

| 오브젝트 | 위치 |
|---|---|
| MiningGrid | (4.51, 0, 3.94) 근처 |
| Prison | (0, 0, 0) |
| Prison_20 (jail) | (3, 0, -7) |
| UpgDrill | (5.02, 0, 6.24) |
| UpgPrison | (2.00, 0, -0.05) |
| outsidefloor | (24.60, -0.55, 5.00) |
| OutsideQueueStart | (-2.50, 0, -7.00) |
