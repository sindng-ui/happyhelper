# 🛠️ 유저 배포 환경 debug_log.txt 생성 완전 차단 계획서

형님, 유저가 실행할 때 지저분하거나 불안하게 느껴질 수 있는 `debug_log.txt` 파일이 더 이상 생성되지 않도록, **릴리즈/배포 환경에서는 파일 생성을 100% 차단**하고 개발 모드에서만 로그를 활성화하는 수정 계획서입니다! 🐧

---

## 🔍 변경 상세 내용

### 1. `src-csharp/DebugLog.cs` 전처리기 제어 (`#if DEBUG`)
- **AS-IS**: 앱 실행 시 `DebugLog` static 생성자에서 `debug_log.txt` 파일 생성을 무조건 실행하여 로그를 남김.
- **TO-BE**: `#if DEBUG` 조건을 적용하여 일반 릴리즈/유저 배포 실행 시에는 `_enabled = false`로 설정.
  - 앱 구동 시 `debug_log.txt` 파일 생성 0%!
  - `DebugLog.Write()` 호출 시 파일 작성을 수행하지 않고 즉시 리턴하여 CPU 및 파일 IO 성능 향상.
  - 개발자가 디버그 기호(`/define:DEBUG`)를 주고 빌드할 때에만 로그 파일 생성.

---

### 2. 기존 생성된 `debug_log.txt` 정리
- `dist-csharp` 폴더 내 기존 테스트 과정에서 남아있던 `debug_log.txt` 파일 삭제.
- `build.ps1`을 통해 빌드 시 유저 배포 폴더에 `debug_log.txt`가 포함되지 않도록 유지.

---

## 🧪 검증 계획

1. **WSL Bash 빌드 및 실행 검증**:
   - `build.ps1`을 실행하여 `dist-csharp/happyhelper.exe` 생성.
   - `dist-csharp/happyhelper.exe`를 실행해보거나 테스트하여 `debug_log.txt` 파일이 생성되지 않음을 100% 확인!

---

## 🚀 진행 여부 확인 (User Approval Required)

형님, 위 수정 계획으로 진행할까요? 승인해 주시면 즉시 `debug_log.txt` 파일 생성을 차단해 드리겠습니다!

- **[Proceed]** (계획 승인 및 수정 시작)
