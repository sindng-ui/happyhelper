# 🗺️ APP_MAP.md (Diablo IV Auto-Skill Helper)

디아블로4 자동 스킬 실행 헬퍼의 전체 아키텍처, 파일 구조 및 모듈 간 인터페이스 명세서입니다.

---

## 📁 디렉토리 및 파일 구조

```
happyhelper/
├── README.md                   # [★] 유저 친화적 한글 사용자 가이드, Human-Like 안심 안전 시스템 안내 및 스크린샷 튜토리얼
├── APP_MAP.md                  # 전체 아키텍처 및 모듈 인터페이스 명세서
├── .gitignore                  # Git 추적 제외 규칙 (빌드 아웃풋, node_modules, 임시 파일)
├── .github/
│   └── workflows/
│       └── build.yml           # [★] GitHub Actions 자동 빌드 및 결과물 (Zip Artifacts) 자동 첨부 CI/CD
├── package.json                # 프로젝트 메타데이터
├── build.ps1                   # [★] C# 콤팩트 빌더 (NuGet 다운로더 및 csc.exe 자동화)
├── dist-csharp/                # [★] 최종 배포용 초경량 폴더 (총합 약 800KB, gitignore 등록)
│   ├── happyhelper.exe         # [★] 순수 C# 네이티브 실행 파일 (키보드 & 마우스 전용 초정밀 엔진 내장)
│   ├── Microsoft.Web.WebView2.Core.dll  # 558KB
│   ├── Microsoft.Web.WebView2.Wpf.dll   # 51KB
│   ├── WebView2Loader.dll      # 162KB
│   └── renderer/               # 초고성능 웹 UI (Full 뷰 420x700px, 극슬림 Mini HUD 195x46px)
│       ├── index.html          # [★] Full 뷰/Mini HUD 마크업 및 키보드·마우스(사이드 키 포함) 키등록 오버레이
│       ├── index.css           # 메인 스타일 진입점
│       ├── styles/             # base.css, components.css, mini.css
│       ├── tauriBridge.js      # dragWindow, setWindowMode 및 하이브리드 IPC 지원
│       ├── components/         # [★] 모듈 분리 UI 컴포넌트 (700줄 규칙 준수)
│       │   ├── keyBindModal.js     # [★ NEW] 키 및 마우스 사이드 키(X1/X2) 브라우저 네비게이션 가로챔 방지 및 캡처 전담 모듈
│       │   ├── presetController.js # [★ NEW] 설정 프리셋 저장/불러오기/삭제 UI 전담 모듈
│       │   └── skillSlot.js        # 스킬 슬롯 UI 렌더러
│       ├── utils/
│       │   └── keyCodes.js     # [★] 마우스4(뒤로가기 X1), 마우스5(앞으로가기 X2) 친화적 명칭 매핑
│       └── app.js              # [★ REFACTORED] 슬림 메인 UI 컨트롤러 (496줄, 700줄 이하 유지)
│
├── renderer/                   # 소스 UI 개발 폴더
├── src-csharp/                 # C# 네이티브 소스
│   ├── MainWindow.xaml.cs      # [★] 슬림 코디네이터: 마우스 오버 창 차단 제거, 브라우저 가속키 비활성화, UI 진입점
│   ├── IpcBridge.cs            # [★] IPC RPC 메시지 라우터: WebView2 웹메시지 파싱, RPC 디스패치 및 응답 처리
│   ├── JsonHelper.cs           # [★] 초경량 JSON 엔진: AppConfig 직렬화/역직렬화 및 IPC 패킷 파서
│   ├── StatusBroadcaster.cs    # [★] 진단 브로드캐스터: 키보드/마우스 모드 경량 상태 브로드캐스트
│   ├── WindowController.cs     # [★] 윈도우 UI 컨트롤러: Win32 드래그, 미니 HUD 크기 전환, 마우스 히트테스트
│   ├── GlobalHook.cs           # [★] LowLevel Keyboard & Mouse Hook: WM_XBUTTONDOWN/WM_NCXBUTTONDOWN 정밀 마스킹, 자가입력 필터링
│   ├── InputEngine.cs          # [★] 순수 키보드 + 마우스 전용 엔진: 디아블로4 직통 PostMessage 듀얼 채널(키/마우스) 주입
│   ├── app.manifest            # [★] asInvoker 매니페스트 (UAC 팝업 없이 더블 클릭 시 즉시 0.1초 실행)
│   ├── WindowHelper.cs         # [★] Win32 윈도우/프로세스 헬퍼: PostKeyToDiablo 및 PostMouseToDiablo (XButton1/2 직통 전송)
│   ├── LoopRunner.cs           # [★] Stopwatch 기반 1ms 초정밀 주기 타이머 및 멀티스레드 루프 엔진 (SkillTriggered 이벤트)
│   ├── TestRunner.cs           # [★] C# 백엔드 핵심 모듈 28종 종합 단위 테스트(UT) 스위트 엔진 (마우스 사이드 키 바인딩 검증 포함)
│   ├── TestCoreSuites.cs       # [★] 단위 테스트용 Config/Input/Loop 핵심 테스트 스위트 분리 모듈
│   ├── TestHelpers.cs          # [★] 단위 테스트용 키보드/패드 키코드 매핑 헬퍼
│   ├── DebugLog.cs             # [★] #if DEBUG 전처리기 도입으로 릴리즈 환경 파일 생성 100% 비활성화
│   └── ConfigManager.cs        # [★] AppData 폴더 내 설정(config.json) 및 프리셋(presets/*.json) 영속성 관리
│
└── docs/
    ├── implementation_plan.md  # 키보드/마우스 전용 전환 및 마우스 사이드 키 구현 계획서
    ├── test_result.txt         # 28종 단위 테스트(UT) 100% 통과 결과 리포트
    ├── history/                # [★] 7일 경과 계획서 보관 폴더
    └── walkthrough.md          # 결과 보고서
```
