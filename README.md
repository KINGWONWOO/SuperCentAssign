# SuperCent Assignment

Unity URP(Universal Render Pipeline) 기반 프로젝트입니다.

## 프로젝트 정보

| 항목 | 내용 |
|------|------|
| 엔진 | Unity (URP) |
| 렌더 파이프라인 | Universal Render Pipeline |
| 플랫폼 | Windows |

## 게임 개요

**장르:** 3D 아케이드 아이들 (Arcade Idle / Hybrid Casual)
**타겟 해상도:** 720 x 1280 (Portrait)

**핵심 루프:**
1. **채집** — 광석 노드 근처 진입 시 자동 채집 → 등에 쌓임
2. **가공** — 광석을 컨버터 기계에 납품 → 수갑으로 변환
3. **체포** — 수갑 소모 → 죄수 체포 → 감방 수감
4. **보상/업그레이드** — 현금으로 도구(곡괭이→드릴→불도저) 업그레이드

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameEnums.cs          # 전역 열거형
│   │   ├── GameSettings.cs       # ScriptableObject - 게임 수치 데이터
│   │   ├── GameManager.cs        # 게임 상태 관리 (싱글톤)
│   │   └── CurrencyManager.cs    # 현금/수갑 재화 관리 (싱글톤)
│   ├── UI/
│   │   ├── UIManager.cs          # UI 업데이트
│   │   └── VirtualJoystick.cs    # 터치 조이스틱
│   ├── Player/
│   │   ├── PlayerController.cs   # 이동 (CharacterController)
│   │   ├── PlayerAnimation.cs    # Animator 파라미터 제어
│   │   ├── StackItem.cs          # 아이템 타입 식별자
│   │   ├── PlayerStackManager.cs # 등에 아이템 쌓기/빼기
│   │   ├── PlayerToolManager.cs  # 도구 레벨 및 채집속도 관리
│   │   └── PlayerInteraction.cs  # 트리거 감지 및 자동 상호작용
│   ├── Interactables/
│   │   ├── MiningNode.cs         # 광석 노드 (HP, 리스폰)
│   │   ├── ConverterMachine.cs   # 광석→수갑 변환기
│   │   ├── DropZone.cs           # 아이템 납품/수령 존
│   │   ├── UpgradeZone.cs        # 도구 업그레이드 발판
│   │   └── ArrestZone.cs         # 죄수 체포 존
│   └── NPC/
│       ├── PrisonerAI.cs         # NavMeshAgent 기반 죄수 AI
│       ├── CellManager.cs        # 감방 수용 관리
│       └── PrisonerSpawner.cs    # 죄수 스폰
├── Scenes/
│   └── SampleScene
├── Prefabs/
│   ├── OrePrefab.prefab          # 광석 아이템 (구 모양, 노란색)
│   ├── HandcuffPrefab.prefab     # 수갑 아이템 (캡슐, 은색)
│   ├── CashPrefab.prefab         # 현금 아이템 (사각, 초록)
│   └── PrisonerPrefab.prefab     # 죄수 NPC (캡슐, 주황)
├── Scenes/
│   └── GameScene.unity           # 완성된 게임 씬
└── Settings/                     # URP 렌더 설정
```

## 씬 레이아웃 (sample.mp4 기준)

| 오브젝트 | 위치 | 설명 |
|---------|------|------|
| MiningNode | (0, 0, 10) | 상단 광석 노드, BoxCollider 트리거 |
| ConverterArea | (5, 0, 6) | 우상단 변환기, 광석→수갑 |
| PoliceDeskArea | (0, 0, 2) | 중앙 경찰 책상, 체포 존 |
| PrisonCell | (6, 0, -3) | 우하단 감옥, 철창 5개 |
| UpgradeZone_0 | (-5, 0, 2) | 좌측 1차 업그레이드 (곡괭이→드릴) |
| UpgradeZone_1 | (-5, 0, -2) | 좌측 2차 업그레이드 (드릴→불도저) |
| Player | (0, 0, 5) | CharacterController + Rigidbody(kinematic) + SphereCollider(trigger) |

## 시작하기

1. 이 레포지토리를 클론합니다.
   ```bash
   git clone https://github.com/KINGWONWOO/SuperCentAssign.git
   ```
2. Unity Hub에서 프로젝트를 엽니다.
3. `Assets/Scenes/GameScene`을 열어 시작합니다.
4. **NavMesh Bake 필요**: Window > AI > Navigation > Bake 실행
5. **죄수 프리팹에 NavMeshAgent 추가**: PrisonerPrefab에 NavMeshAgent 컴포넌트 추가 후 재저장

## 변경 이력

### 2026-03-21
- 프로젝트 최초 생성 및 초기 셋업
- Unity URP 기본 씬 및 렌더 설정 포함
- `.gitignore` 추가 (Library, Temp, Logs 등 제외)
- `.vsconfig` 추가 (Visual Studio Unity 워크로드 지정)
- `main` 브랜치를 `201924407` 브랜치에 통합 (머지)
- `conversation_export.txt` 추가 (Claude Code 대화 기록 export)

### 프로토타입 스크립트 작성 (2026-03-21)
- 전체 20개 C# 스크립트 작성 완료 (Core 4 / UI 2 / Player 6 / Interactables 5 / NPC 3)
- 단일 책임 원칙(SRP) 적용, 즉시 Unity에서 사용 가능한 완성 코드

### update.txt 기반 전체 시스템 오버홀 (2026-03-21)
- **MiningGrid** (NEW): 7열×20행 RockNode 그리드, 6초 리스폰, MineAt(width) 다중 채굴
- **RockNode** (NEW): 스택 가득 찼어도 돌 사라짐, 워커 NPC용 MineForWorker()
- **DeskZone + DeskManager** (NEW): ArrestZone 대체, 수갑→돈 변환, 수감자별 4개 처리
- **MoneySpawner** (NEW): 수갑 판매마다 돈 스택 생성, 플레이어 픽업 지원
- **SpeechBubble** (NEW): TMP 3D 빌보드 말풍선 (수갑 잔량 / No Cell!)
- **WorkerNPC** (NEW): 행 왕복 자동 채굴, 광석→Converter 직접 전달
- **AutoSellNPC** (NEW): 수갑 10개 배치 자동 판매 NPC
- `GameEnums`: DrillCar 추가, PrisonerState 큐 시스템, MoneyPickup, UpgradeType 5종
- `GameSettings`: 채굴 폭/스택(레벨별), 수갑 판매, 수감자 큐, 업그레이드 비용 등 전면 재편
- `PlayerStackManager`: Ore 레벨별 제한(10/20/30), Handcuff/Cash 무제한
- `PlayerToolManager`: GetMiningWidth() 추가, SetLevel()로 스택 한도 자동 갱신
- `PlayerInteraction`: MiningGrid+DeskZone 대응, DrillCar 탑승 애니메이션
- `ConverterMachine`: 1:1 비율(광석 1→수갑 1), 2초 처리
- `DropZone`: MoneyPickup 타입 추가, 수갑 픽업 무제한화
- `UpgradeZone`: 5종 업그레이드 트리 (Drill/DrillCar/WorkerHire/AutoSell/PrisonExpand)
- `PrisonerAI`: 코루틴 이동, Desk→수갑4개→감방 상태머신, No Cell! 대기
- `PrisonerSpawner`: 수요 기반 최대 2인 큐 시스템
- `CellManager`: 기본 20명, ExpandCapacity(80) 업그레이드 지원
- `CurrencyManager`: 수갑 재화 제거 (Cash만 관리)

### sample.mp4 기반 수정 및 씬 완성 (2026-03-21)
- `PlayerStackManager`: MAX 인디케이터 오브젝트 필드 추가, 스택 가득 찰 때 표시
- `CellManager`: TextMeshPro 3D 카운터 텍스트 추가 (X/Y 형식)
- `UpgradeZone`: 현금 소비 시 fillBar 높이 채움 (pivot 보정 포함), PlayerStackManager 연동
- `ArrestZone`: 체포 후 플레이어 등에 현금 물리 아이템 추가 (cashPrefab 필드)
- `PlayerInteraction`: UpgradeZone에 stackManager 전달하도록 수정
- **GameScene 완성**: sample.mp4 기반 레이아웃으로 씬 재구성
  - Player: CharacterController + Rigidbody(isKinematic) + SphereCollider(isTrigger) 트리거 감지 아키텍처
  - 광석/수갑/현금/죄수 프리팹 4종 생성 (Assets/Prefabs/)
  - 모든 Inspector 참조 SerializedObject API로 자동 연결
  - 감옥 철창(Cylinder x5), 침대(비활성 시작), 업그레이드 fillBar 비주얼 구성
