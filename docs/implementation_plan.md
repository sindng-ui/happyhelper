# 🛠️ 스킬 순서 변경, Mini HUD 스킬 칩 제거 및 README 일시정지 안내 구현 계획서

형님, 요청하신 3가지 피드백(스킬 순서 조정, Mini HUD 스킬 칩 제거, README.md 일시정지 꿀팁 설명) 반영 계획서입니다! 🐧

---

## 🔍 변경 상세 내용

### 1. 🔄 스킬 슬롯 순서 변경 (Skill Order TO-BE)
- **AS-IS**: 기본 기술(좌클릭) / 핵심 기술(우클릭) -> 스킬 1, 2, 3, 4
- **TO-BE**: **스킬 1, 2, 3, 4가 상단에 위치하고, 기본 기술 / 핵심 기술이 그 아래에 위치**
- **수정 위치**:
  - `src-csharp/ConfigManager.cs`: `AppConfig.CreateDefault()`에서 스킬 1, 2, 3, 4를 먼저 추가하고 기본/핵심 기술을 5번째, 6번째 슬롯으로 배치.
  - `renderer/app.js`: 렌더링 시 등록된 슬롯 순서대로 상단부터 차례대로 출력.

---

### 2. 🗗 Mini HUD 스킬 칩 제거
- **수정 요공**: Mini HUD 모드에 표시되던 하단 스킬 칩(`miniSkillsRow`: 1 1s, 2 1s ...)을 완전히 숨김/제공하여, **상단 컨트롤러 패널(`● 대기 [▶] [⏹] 🗖 ✕`)만 나오는 극슬림 HUD**로 변경.
- **수정 위치**:
  - `renderer/miniMode.js`: `renderMiniSkills()`에서 `miniSkillsRow.style.display = 'none';` 처리 및 칩 렌더링 억제.
  - `renderer/index.html`: `miniSkillsRow` 요소 감춤 처리.

---

### 3. 📝 README.md 일시정지(Disable Keys) 활용법 설명 추가
- **추가 내용**:
  - 디아블로4 플레이 중 포털 열기(`T`), 캐릭터/소지품 창 열기(`I`), 지도 열기(`M`), 메인 메뉴(`Esc`), 채팅(`Enter`) 등을 누를 때 **자동으로 스킬 연사가 일시정지**되도록 일시정지 키(Disable Keys)를 등록해 두는 꿀팁 수록.
  - 마을 이동 시나 소지품 정리 시 스킬이 엉뚱하게 나가지 않는 안심 기능으로 유저분들께 소개.

---

## 🚀 진행 여부 확인 (User Approval Required)

형님, 위 수정 계획으로 진행할까요? 승인해 주시면 즉시 코드 및 README.md 반영 후 빌드하여 검증해 드리겠습니다!

- **[Proceed]** (계획 승인 및 수정 작업 시작)
