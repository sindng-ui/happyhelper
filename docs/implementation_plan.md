# 🖱️ 키보드 & 마우스 전용 헬퍼 전환 및 마우스 사이드 키 바인딩 구현 계획서

형님! 패드 삽질하시느라 정말 고생 많으셨습니다.  
컨트롤러 관련 복잡한 드라이버(ViGEmBus, HidHide)와 슬롯 충돌 문제를 과감히 걷어내고, **순수 키보드 & 마우스 전용 초경량·초정밀 헬퍼**로 전격 전환하겠습니다!

또한 형님께서 겪으신 **"마우스 사이드 키(뒤로가기/앞으로가기)가 등록되지 않는 문제"**의 명확한 원인을 찾아내어 완벽히 해결하는 계획을 수립했습니다.

---

## 🚨 700줄 초과 파일 감지 및 리팩토링 안내

- **대상 파일**: [renderer/app.js](file:///k:/Antigravity_Projects/gitbase/happyhelper/renderer/app.js) (**953줄**)
- **규칙 준수**: 한 파일이 700줄을 넘었으므로, 이번 작업에서 패드 관련 레거시 코드를 제거하고 역할을 작고 명확한 모듈로 분리하는 리팩토링을 함께 진행합니다.
  - `keyBindModal.js` (신규): 키/마우스 사이드 버튼 캡처 및 모달 오케스트레이션 분리
  - `presetController.js` (신규): 프리셋 저장/불러오기/삭제 UI 로직 분리
  - `app.js`: 400줄 이하의 깔끔한 메인 컨트롤러로 경량화

---

## 🔍 마우스 사이드 키 등록 실패 원인 정밀 분석

1. **[치명적 버그] 마우스가 헬퍼 창 위에 있을 때 마우스 입력 전면 차단 ([MainWindow.xaml.cs](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/MainWindow.xaml.cs#L203-L206))**:
   ```csharp
   if (_bindingMode)
   {
       if (isMouse && WindowController.IsMouseOverWindow(this))
       {
           return; // 💥 마우스가 앱 창 위에 있으면 마우스 사이드 버튼/휠/우클릭이 무조건 씹힘!
       }
   ```
   키 바인딩 모달이 뜬 상태에서 마우스 사이드 버튼을 누르면 당연히 마우스 커서가 헬퍼 창 안에 있으므로, 이 조건문에 걸려 즉시 `return`되어 입력이 완전히 증발했습니다.
   (좌클릭은 이미 `keyCode == 1001`로 별도 차단하고 있으므로 해당 조건은 버그입니다.)

2. **브라우저(WebView2)의 기본 제스처 가로챔**:
   마우스 사이드 키(XButton1: 마우스4, XButton2: 마우스5)는 브라우저 상에서 **뒤로 가기(Back) / 앞으로 가기(Forward)** 기능으로 예약되어 있습니다.
   WebView2에서 브라우저 단축키 가로챔 방지(`AreBrowserAcceleratorKeysEnabled = false`) 및 웹 이벤트 기본 동작 방지(`e.preventDefault()`)를 하지 않으면 브라우저 엔진이 키 입력을 낚아채서 헬퍼에 전달되지 못합니다.

3. **Win32 저수준 마우스 훅 비트 추출 보강**:
   `GlobalHook.cs`에서 `mhs.mouseData >> 16` 처리 시 마스크 보정(`& 0xFFFF`) 및 `WM_NCXBUTTONDOWN` 등 비클라이언트 영역 메시지도 포착할 수 있도록 안전 장치가 필요합니다.

---

## 💡 제안하는 구현 내용 (Proposed Changes)

### 1. C# 백엔드 수정
- **[MainWindow.xaml.cs](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/MainWindow.xaml.cs)**:
  - `WindowController.IsMouseOverWindow(this)` 마우스 차단 로직 제거.
  - 마우스 좌클릭(`keyCode == 1001`)만 바인딩에서 제외하고, 마우스 사이드 버튼(`1004`, `1005`), 우클릭(`1002`), 휠클릭(`1003`)은 창 위에서도 즉각 바인딩되도록 전면 허용.
  - WebView2 초기화 시 `AreBrowserAcceleratorKeysEnabled = false` 설정하여 마우스 뒤로가기/앞으로가기 브라우저 네비게이션 원천 차단.
  - 패드 백그라운드 스레드 및 HidHide/DeviceManager 호출 완전 제거.
- **[GlobalHook.cs](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/GlobalHook.cs)**:
  - `WM_XBUTTONDOWN` (0x020B) 및 `WM_NCXBUTTONDOWN` (0x00AB) 감지 지원.
  - `int xbtn = (int)((mhs.mouseData >> 16) & 0xFFFF);` 정밀 비트마스킹.
  - 불필요한 XInput 로딩 및 66Hz 패드 폴링 스레드 제거 -> CPU 0%, 반응 속도 즉각 반응.
- **[InputEngine.cs](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/InputEngine.cs)**:
  - 패드 채널 분기 제거, 순수 키보드 & 마우스 전용 엔진으로 최적화.
  - 마우스4(1004), 마우스5(1005) 발사 시 `mouse_event(MOUSEEVENTF_XDOWN/UP)`와 함께 디아블로4 창에 `WM_XBUTTONDOWN/UP` 직통 PostMessage 듀얼 발사 추가 지원.
- **[app.manifest](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/app.manifest)**:
  - 더 이상 커널 드라이버 은닉이 불필요하므로 `asInvoker`로 전환하여 **실행 시 번거로운 UAC 관리자 승인 팝업 없이 더블클릭 즉시 실행**되도록 쾌적성 대폭 개선!

---

### 2. 웹 UI (Renderer) 수정 & 리팩토링
- **[renderer/index.html](file:///k:/Antigravity_Projects/gitbase/happyhelper/renderer/index.html)**:
  - 패드 드라이버 모달(`driverNoticeOverlay`) 및 패드 안내 문구 완전 제거.
  - 키 바인딩 모달 문구를 `⌨️ 키보드 · 🖱️ 마우스 (측면 사이드 버튼 포함)`으로 직관적으로 개편.
  - 상태 표시줄에 패드 대기 대신 깔끔한 "키보드 & 마우스 전용 모드 가동" 표시.
- **[renderer/components/keyBindModal.js](file:///k:/Antigravity_Projects/gitbase/happyhelper/renderer/components/keyBindModal.js)** (신규 분리):
  - 키 바인딩 모달 전담 모듈.
  - 웹 브라우저 내 마우스 사이드 클릭(`auxclick`, `mouseup`, `button === 3 || button === 4`) 감지 시 `e.preventDefault()`로 브라우저 뒤로가기 방지 및 즉각 바인딩 신호 트리거.
- **[renderer/utils/keyCodes.js](file:///k:/Antigravity_Projects/gitbase/happyhelper/renderer/utils/keyCodes.js)**:
  - `1004`: `🖱️ 마우스4 (뒤로가기 X1)`, `1005`: `🖱️ 마우스5 (앞으로가기 X2)`로 유저 친화적 라벨 개선.
- **[renderer/app.js](file:///k:/Antigravity_Projects/gitbase/happyhelper/renderer/app.js)**:
  - 패드 폴러(`startGamepadPoller`), 패드 버튼 매핑(2001~2016) 코드 삭제.
  - 모듈 분리로 400줄 이하로 대폭 슬림화하여 700줄 규칙 준수.

---

## 💡 추가 제안 아이디어 (형님께 드리는 제안)
1. **UAC 관리자 권한 팝업 제거 (asInvoker)**:
   패드 드라이버를 쓰지 않기 때문에 매번 켤 때마다 뜨던 귀찮은 Windows 관리자 권한 팝업을 없앨 수 있습니다. 0.1초 만에 가볍게 실행됩니다.
2. **마우스 사이드 버튼 전용 직관적 뱃지**:
   키 설정 버튼에 `X1 (뒤로)`, `X2 (앞으로)` 표시와 함께 마우스 모양의 세련된 시각 뱃지를 부여하여 어떤 버튼인지 한눈에 알 수 있게 합니다.

---

## 🧪 검증 계획 (Verification Plan)

### 1. 자동 빌드 및 단위 테스트
- `powershell.exe -NoProfile -File build.ps1 -test` 실행하여 핵심 로직 단위 테스트 통과 확인.
- `build.ps1`로 컴파일 에러 없이 `dist-csharp/happyhelper.exe` 빌드 완료 확인.

### 2. 마우스 사이드 키 바인딩 검증
- 헬퍼 실행 후 스킬 슬롯의 '키 변경' 버튼 클릭 -> 키 바인딩 모달 오픈.
- **모달 화면 위에서 마우스 측면 버튼(뒤로가기, 앞으로가기) 클릭**:
  - 브라우저 뒤로가기가 동작하지 않는지 확인.
  - 슬롯에 `🖱️ 마우스4 (뒤로가기 X1)` 또는 `🖱️ 마우스5 (앞으로가기 X2)`로 즉시 정상 등록되는지 확인.
- 시작 핫키(F5) / 정지 핫키(F6) / 일시정지 키 추가에도 마우스 사이드 버튼이 정상 등록되는지 확인.

### 3. 디아블로4 스킬 발동 검증
- 마우스4 또는 마우스5가 등록된 슬롯을 활성화하고 주기를 설정한 뒤 시작(F5).
- 디아블로4 창에서 마우스 사이드 버튼에 할당된 기술이 정확한 주기로 발동되는지 확인.
