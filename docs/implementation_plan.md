# 🛠️ 레거시 파일 정리, .gitignore 및 GitHub Actions 자동 빌드 워크플로우 구축 계획서

형님, GitHub Push 시 자동으로 C# 실행 파일을 빌드하고 결과물(Zip/Artifacts)을 첨부해 주는 **GitHub Actions 워크플로우** 구축 건까지 포함한 최종 계획서입니다! 🐧

---

## 🔍 작업 범위 상세

### 🗑️ 1. 불필요한 레거시 폴더 및 파일 삭제
- `src-tauri/` (과거 Tauri/Rust 아키텍처 잔재)
- `main/` (과거 Electron 메인 프로세스 코드)
- `tests/` (구 JS 테스트 스크립트)
- `write_testrunner.py` (임시 파이썬 스크립트)

---

### 🙈 2. `.gitignore` 생성
GitHub 업로드 시 제외해야 할 빌드 결과물, 라이브러리 및 임시 데이터 등록:
```gitignore
# Build outputs & Binaries
/dist-csharp/
/build-temp/
*.exe
*.dll
*.pdb
*.sys

# Node.js dependencies
/node_modules/
package-lock.json

# Local runtime user data
/data/
debug_log.txt

# OS & IDE Files
.DS_Store
Thumbs.db
.vscode/
.idea/
*.suo
*.user
```

---

### ⚙️ 3. GitHub Actions 자동 빌드 워크플로우 생성 (`.github/workflows/build.yml`)

GitHub에 `push` 하거나 `release` 태그를 생성할 때 `windows-latest` 러너에서 자동으로 빌드를 수행하고 결과물을 아티팩트로 첨부합니다:

- **트리거 (Trigger)**:
  - `main` 브랜치 `push` / `pull_request`
  - `tags: ['v*']` 태그 릴리즈 시
- **빌드 절차**:
  1. `actions/checkout@v4`로 소스코드 체크아웃
  2. PowerShell에서 `powershell -ExecutionPolicy Bypass -File build.ps1` 실행하여 `dist-csharp/happyhelper.exe` 생성
  3. `dist-csharp` 폴더를 `happyhelper-win-x64.zip`으로 압축
  4. `actions/upload-artifact@v4`를 사용하여 GitHub Actions 결과물(Artifacts) 탭에 90일간 자동 보관 및 제공
  5. (태그 릴리즈 시) `softprops/action-gh-release@v2`를 통해 Release에 zip 바이너리 자동 첨부

---

### 📐 4. `APP_MAP.md` 명세서 최신화
- 레거시 삭제 폴더 반영 및 `.github/workflows/build.yml` 항목 추가.

---

## 🚀 진행 여부 확인 (User Approval Required)

형님, 위 정리 및 GitHub Actions 자동 빌드 구축 계획으로 진행할까요? 승인해 주시면 파일 정리, `.gitignore` 및 GitHub Actions 워크플로우 파일 생성을 일괄 진행하겠습니다!

- **[Proceed]** (계획 승인 및 자동 빌드 워크플로우 구축 시작)
