# 🛡️ HidHide 관리자 권한 승격 & 물리 패드 은닉(Cloaking) 실효성 보장 계획서

형님! `joy.cpl`에 물리 패드가 남아있던 근본적인 원인은 **HidHide 커널 드라이버의 관리자 권한(Access Denied, Error Code 5) 보안 정책** 때문이었습니다.
이를 완벽히 해결하고 `joy.cpl`에 물리 패드를 완전히 숨겨 단일 가상 패드로 동작시키기 위한 구현 계획서입니다.

---

## ⚠️ 원인 분석 및 발견된 이슈

1. **HidHide 커널 드라이버 권한 검사 (Error 5 Access Denied)**:
   - HidHide 커널 드라이버는 관리자 권한(Elevated Privileges)이 없는 프로세스의 설정 변경 요청(`IOCTL_SET_BLACKLIST`, `IOCTL_SET_WHITELIST`, `IOCTL_SET_ACTIVE`)을 거부(Access Denied)합니다.
   - 따라서 일반 권한으로 앱 실행 시 은닉 명령이 적용되지 않아 `joy.cpl`에 물리 패드가 그대로 떴던 것입니다.
2. **`app.manifest` 미적용**:
   - 기존 빌드 환경에 `requestedExecutionLevel level="requireAdministrator"` 설정이 포함되지 않아 앱이 일반 사용자 권한으로 구동되고 있었습니다.

---

## 💡 제안하는 해결 방안 (Proposed Changes)

### 1. [NEW] [app.manifest](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/app.manifest)
- C# 프로젝트용 `app.manifest` 신규 생성.
- `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />` 구성을 통해 앱 실행 시 자동으로 UAC 승격 창이 뜨고 관리자 권한으로 실행되도록 설정.

### 2. [MODIFY] [build.ps1](file:///k:/Antigravity_Projects/gitbase/happyhelper/build.ps1)
- `csc.exe` 컴파일 시 `-win32manifest:src-csharp/app.manifest` 옵션을 추가하여 빌드되는 `happyhelper.exe`에 관리자 권한 요구 매니페스트가 확실하게 내장되도록 수정.

### 3. [MODIFY] [HidHideManager.cs](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/HidHideManager.cs)
- `IsAdministrator()` 헬퍼 함수를 추가하여 권한 상태 진단.
- `DeviceIoControl` 호출 시 실패하면 `Marshal.GetLastWin32Error()`를 디버그 로그에 상세히 기록하여 진단 투명성 확보.
- 화이트리스트 및 블랙리스트 등록 시 정확한 `MULTI_SZ` 바이트 패딩과 장치 인스턴스 ID(대문자 `HID\VID_...`) 정규화 보장.

### 4. [MODIFY] [DeviceManager.cs](file:///k:/Antigravity_Projects/gitbase/happyhelper/src-csharp/DeviceManager.cs)
- `EnsureVirtualPadIsSlot0()` 실행 시 `HidHideManager.AutoCloakConnectedGamepads()` 호출 결과를 검증하고 실패 시 로그로 투명하게 리포팅.

---

## 🧪 검증 계획 (Verification Plan)

### 자동 단위 테스트 (Automated Tests)
- `powershell.exe -NoProfile -File build.ps1 -test` 실행으로 기존 27개 단위 테스트 100% 통과 확인.
- 매니페스트 내장 빌드 정상 완료 확인 (`dist-csharp/happyhelper.exe`).

### 실효성 수동 검증 (Manual Verification)
1. 물리 게임패드가 PC에 연결된 상태에서 `dist-csharp/happyhelper.exe` 실행 (UAC 관리자 승인 팝업 확인).
2. 앱 실행 후 `joy.cpl` (게임 컨트롤러 설정)을 실행하여 **물리 패드가 완전히 숨겨지고 오직 가상 Xbox 360 패드 1개만 뜨는지** 확인.
3. `HidHide Configuration Client`를 열어서 `Applications` 탭에 `happyhelper.exe`가 활성화되어 있고 `Devices` 탭에 물리 패드가 체크/은닉 상태인지 최종 확인.
