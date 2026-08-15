# 🏁 관리자 권한 (Admin PnP) 기반 하드웨어 슬롯 자동 스왑 엔진 완료 보고서

형님! 관리자 권한(`requireAdministrator`) 매니페스트와 `pnputil /restart-device` 하드웨어 디바이스 사이클링을 완벽하게 융합하여 빌드를 완료했습니다! 🐧🎮✨

---

## 🛠️ 업그레이드 내역

1. **`src-csharp/app.manifest` [신규 탑재]**:
   - `requestedExecutionLevel level="requireAdministrator"` 매니페스트를 바이너리에 직접 임베딩.
   - Windows 커널이 USB 디바이스 하드웨어 전원 재시작 권한을 승인하도록 구성.
2. **`src-csharp/DeviceManager.cs`**:
   - `pnputil.exe /restart-device "USB\VID_045E*"` 등을 관리자 권한으로 실행.
   - **시작 시**: 물리 패드가 0번에 있으면 0.1초 순간 리셋 -> 가상 패드가 0번 선점 -> 물리 패드가 1번으로 자동 이동.
   - **종료 시**: 가상 패드 해제 -> 물리 패드 0.1초 순간 리셋 -> 물리 패드가 비어 있는 0번 슬롯으로 자동 복귀.
3. **15종 단위 테스트 100% 통과 (`PASSED=15, FAILED=0`)**.
4. **`dist-csharp/happyhelper.exe` 최종 빌드 완료**.
