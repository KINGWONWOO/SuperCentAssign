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
└── Settings/                     # URP 렌더 설정
```

## 시작하기

1. 이 레포지토리를 클론합니다.
   ```bash
   git clone https://github.com/KINGWONWOO/SuperCentAssign.git
   ```
2. Unity Hub에서 프로젝트를 엽니다.
3. `Assets/Scenes/SampleScene`을 열어 시작합니다.

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
