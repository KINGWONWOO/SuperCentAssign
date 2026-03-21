# SuperCent Assignment

Unity URP(Universal Render Pipeline) 기반 프로젝트입니다.

## 프로젝트 정보

| 항목 | 내용 |
|------|------|
| 엔진 | Unity (URP) |
| 렌더 파이프라인 | Universal Render Pipeline |
| 플랫폼 | Windows |

## 프로젝트 구조

```
Assets/
├── Scenes/              # 씬 파일
│   └── SampleScene      # 기본 샘플 씬
├── Settings/            # URP 렌더 설정
│   ├── URP-Balanced     # 균형 품질 프로파일
│   ├── URP-HighFidelity # 고품질 프로파일
│   └── URP-Performant   # 성능 우선 프로파일
├── TutorialInfo/        # 튜토리얼 관련 리소스
└── UniversalRenderPipelineGlobalSettings.asset

Packages/                # Unity 패키지 목록
ProjectSettings/         # 프로젝트 설정 파일
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
