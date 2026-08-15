# 🗺️ APP_MAP.md (Diablo IV Auto-Skill Helper)

디아블로4 자동 스킬 실행 헬퍼의 전체 아키텍처, 파일 구조 및 모듈 간 인터페이스 명세서입니다.

---

## 📁 디렉토리 및 파일 구조

```
happyhelper/
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
│       ├── index.html          # [★] Full 뷰/Mini HUD 마크업 및 오픈소스 드라이버(ViGEmBus) 투명 안내 모달 (공식 GitHub URL 포함)
│       ├── index.css           # 메인 스타일 진입점
│       ├── styles/             # base.css, components.css (driverNoticeOverlay z-index: 99999!), mini.css
│       ├── tauriBridge.js      # dragWindow, setWindowMode 및 하이브리드 IPC 지원
│       └── app.js              # [★] updatePadStatusUI (viGEmInstalled || viGEmReady 감지) 및 ESC/마우스 좌클릭 핫키 바인딩 차단

├── renderer/                   # 소스 UI 개발 폴더
├── src-csharp/                 # C# 네이티브 소스
│   ├── MainWindow.xaml.cs      # [★] IsMouseOverAppWindow 필터링, ESC 키(1) & 마우스 좌클릭(1001) 핫키 바인딩 차단 및 2초 주기 _statusPollTimer 실시간 브로드캐스트
│   ├── GlobalHook.cs           # [★] LLKHF_INJECTED 자가 입력 무한루프 필터링, 동적 XInput + WinMM 엔진
│   ├── InputEngine.cs          # [★] 하이브리드 입력 엔진 (가상 패드 + 하드웨어 스캔코드 Dual-Injection 듀얼 모드)
│   ├── ViGEmInstaller.cs       # [★] ViGEmClient 인스턴스 생성 및 커널 디바이스 핸들 실효 검사 기반 100% 명확한 드라이버 감지기
│   ├── WindowHelper.cs         # [★] Win32 윈도우/프로세스 헬퍼
│   ├── LoopRunner.cs           # [★] Stopwatch 기반 1ms 초정밀 주기 타이머 및 멀티스레드 루프 엔진 (스킬 발사 시 SkillTriggered 이벤트 브로드캐스트)
│   ├── GamepadPassthrough.cs   # [★] 120Hz 물리 패드 동적 핫플러그 감지 및 D-Pad 이동 변환, 가상 패드 실시간 합성 엔진
│   ├── DebugLog.cs             # [★] 파일 기반 실시간 디버그 로거 (debug_log.txt)
│   └── ConfigManager.cs        # [★] AppData 폴더 내 설정(config.json) 영속성 관리

└── docs/
    ├── implementation_plan.md  # 구현 계획서
    └── walkthrough.md          # 결과 보고서

```
