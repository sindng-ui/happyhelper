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
│   ├── happyhelper.exe         # [★] 순수 C# 네이티브 실행 파일 (ViGEmBus 설치 바이너리 100% 임베딩 융합)
│   ├── Microsoft.Web.WebView2.Core.dll  # 558KB
│   ├── Microsoft.Web.WebView2.Wpf.dll   # 51KB
│   ├── WebView2Loader.dll      # 162KB
│   └── renderer/               # 초고성능 웹 UI (Full 뷰 420x700px, 극슬림 Mini HUD 195x46px)
│       ├── index.html          # [★] Full 뷰/Mini HUD 마크업 및 오픈소스 드라이버 2종(ViGEmBus + HidHide) 투명 안내 모달 (공식 GitHub URL 포함)
│       ├── index.css           # 메인 스타일 진입점
│       ├── styles/             # base.css, components.css (driverNoticeOverlay z-index: 99999!), mini.css
│       ├── tauriBridge.js      # dragWindow, setWindowMode, openExternalUrl 및 하이브리드 IPC 지원
│       └── app.js              # [★] updatePadStatusUI (ViGEmBus & HidHide 듀얼 드라이버 감지) 및 ESC/마우스 좌클릭 핫키 바인딩 차단

├── renderer/                   # 소스 UI 개발 폴더
├── src-csharp/                 # C# 네이티브 소스
│   ├── MainWindow.xaml.cs      # [★] 슬림 코디네이터 (269줄): UI 진입점, 생명주기 및 핫키 오케스트레이션
│   ├── IpcBridge.cs            # [★] IPC RPC 메시지 라우터 (260줄): WebView2 웹메시지 파싱, RPC 디스패치 및 응답 처리
│   ├── JsonHelper.cs           # [★] 초경량 JSON 엔진 (207줄): AppConfig 직렬화/역직렬화 및 IPC 패킷 파서
│   ├── StatusBroadcaster.cs    # [★] 진단 브로드캐스터 (86줄): 2초 주기 XInput/ViGEm/HidHide 상태 실시간 UI 전달
│   ├── WindowController.cs     # [★] 윈도우 UI 컨트롤러 (85줄): Win32 드래그, 미니 HUD 크기 전환, 마우스 히트테스트
│   ├── GlobalHook.cs           # [★] LLKHF_INJECTED 자가 입력 무한루프 필터링, 동적 XInput + WinMM 엔진
│   ├── InputEngine.cs          # [★] 하이브리드 입력 엔진 (가상 패드 + 하드웨어 스캔코드 Dual-Injection 듀얼 모드)
│   ├── app.manifest            # [★] requireAdministrator 매니페스트 (PnP 장치 하드웨어 리셋 권한 획득)
│   ├── HidHideManager.cs       # [★] 순수 Win32 커널 IOCTL 기반 HidHide 필터 드라이버 제어기 (앱 화이트리스트, 장치 블랙리스트 자동 은닉, Cloak 토글)
│   ├── DeviceManager.cs        # [★] HidHide Auto-Cloak 및 Fail-Safe 전역 복원 핸들러 탑재 슬롯 관리자
│   ├── TestRunner.cs           # [★] C# 백엔드 핵심 모듈 27종 종합 단위 테스트(UT) 스위트 엔진 (Passthrough 융합 및 Fail-Safe 검증)
│   ├── TestCoreSuites.cs       # [★] 단위 테스트용 Config/Input/Loop 핵심 테스트 스위트 분리 모듈
│   ├── TestHelpers.cs          # [★] 단위 테스트용 키보드/패드 키코드 매핑 헬퍼
│   ├── ViGEmInstaller.cs       # [★] ViGEmClient 인스턴스 생성 및 커널 디바이스 핸들 실효 검사 기반 100% 명확한 드라이버 감지기
│   ├── WindowHelper.cs         # [★] Win32 윈도우/프로세스 헬퍼
│   ├── LoopRunner.cs           # [★] Stopwatch 기반 1ms 초정밀 주기 타이머 및 멀티스레드 루프 엔진 (스킬 발사 시 SkillTriggered 이벤트 브로드캐스트)
│   ├── GamepadPassthrough.cs   # [★] 120Hz 물리 패드 입력 + 자동 스킬 실시간 Pure-Fusion 합성 엔진
│   ├── DebugLog.cs             # [★] #if DEBUG 전처리기 도입으로 릴리즈 환경 파일 생성 100% 비활성화
│   └── ConfigManager.cs        # [★] AppData 폴더 내 설정(config.json) 및 프리셋(presets/*.json) 영속성 관리 (activePreset, DeletePreset 지원)
│
└── docs/
    ├── implementation_plan.md  # 구현 계획서
    ├── test_result.txt         # 27종 단위 테스트(UT) 100% 통과 결과 리포트
    └── walkthrough.md          # 결과 보고서

```
