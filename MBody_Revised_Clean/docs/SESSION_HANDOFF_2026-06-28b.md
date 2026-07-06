# M-Body 세션 작업 기록 (2026-06-28, 오후 — 빌드 성공 & 버튼 작업)

> **프로젝트:** `c:\project\Unity_Projects\MBody_Revised_Clean`
> **Unity:** **6000.3.18f1** (Android Build Support 설치 완료) + 6000.4.1f1
> **패키지:** `com.CAU.MBody`
> **대상 기기:** Galaxy Tab A8 SM-T290 (`R9WR611YAAJ`)
> **이전 세션:** [`SESSION_HANDOFF_2026-06-28.md`](./SESSION_HANDOFF_2026-06-28.md)

---

## 0. 요약

| 구분 | 결과 |
|------|------|
| Android Build Support 설치 (6000.3.18f1) | ✅ 완료 |
| 빌드 블로커 3종 해결 (패키지/Git/경로) | ✅ |
| "내영상 보기" 수정 포함 APK 빌드+설치 | ✅ Tab A8 설치 완료 |
| Result/Review "Home" 버튼 → EndSelect 복귀 수정 | ✅ 씬 수정 + 빌드+설치 완료 |
| Home(메인) vs 이전(EndSelect) 버튼 배치 결정 | ⏳ **사용자 확인 대기 중** (미구현) |

---

## 1. 빌드 환경 확정 (중요)

이전 세션의 빌드 블로커를 모두 해결함. **APK 빌드 시 반드시 아래 조건 충족.**

### 1-1. Unity 버전
- 설치됨: **6000.3.18f1** (Android 모듈 포함), 6000.4.1f1 (Android 모듈 없음)
- 빌드는 **6000.3.18f1** 사용: `C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe`
- `ProjectSettings/ProjectVersion.txt` → `6000.3.18f1`로 변경됨

### 1-2. 패키지 수정 (`Packages/manifest.json`)
- `com.unity.collections`: `6.4.0` → **`2.5.7`** (6000.3 호환)
- `com.coplaydev.unity-mcp`: **제거** (Git 필요, 빌드 불필요)
- `com.unity.barracuda`: Git URL → **`file:com.unity.barracuda`** (로컬 패키지)
  - `Packages/com.unity.barracuda/`에 barracuda-release 3.0.1 압축 해제해 둠
- `packages-lock.json`은 수정 시 삭제하면 재생성됨

### 1-3. Windows 260자 경로 제한 (Gradle/ninja)
- 빌드 전 환경변수 필수 설정 (긴 경로 회피):
  ```powershell
  $env:GRADLE_USER_HOME = "C:\gradle"
  $env:TEMP = "C:\utmp"
  $env:TMP  = "C:\utmp"
  ```
- 미설정 시 `ninja: error ... Filename longer than 260 characters` 로 Gradle 실패

### 1-4. 빌드 명령 (APK까지 만들 때)
```powershell
$env:GRADLE_USER_HOME="C:\gradle"; $env:TEMP="C:\utmp"; $env:TMP="C:\utmp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath "c:\project\Unity_Projects\MBody_Revised_Clean" `
  -executeMethod AutomatedAndroidBuild.BuildLatestStableAndroidApk `
  -logFile "c:\project\Unity_Projects\MBody_Revised_Clean\Build\Android\unity-android-build.log"
```
- 성공 로그: `Android APK build succeeded`
- 빌드 시작 전 **기존 Unity 프로세스 모두 종료** 필요 (Library lock 충돌 시 `return code 1`로 즉시 죽음)

### 1-5. 설치 (서명 불일치 대응)
- 새 서명이라 `INSTALL_FAILED_UPDATE_INCOMPATIBLE` 발생 → 기존 앱 제거 후 설치:
  ```powershell
  $adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
  & $adb -s R9WR611YAAJ uninstall com.CAU.MBody
  & $adb -s R9WR611YAAJ install <apk>
  # OBB: /sdcard/Android/obb/com.CAU.MBody/main.1.com.CAU.MBody.obb
  ```
- 현재 기기 설치본: APK 37.8 MB (06-28 09:28), OBB 696.5 MB

---

## 2. "Home" 버튼 → EndSelect 복귀 수정 (완료, 설치됨)

### 배경
3회 종료 후 점수보기(Result)/영상보기(Review) 화면의 좌상단 "Home" 버튼이
onClick에서 **자기 페이지만 SetActive(false)** 하고 다른 페이지를 켜지 않아 → 빈 화면이 됨.

### 수정 내용 (`Assets/Scenes/MBody.unity`)
두 Home 버튼에 `ButtonFlow` 컴포넌트를 추가하고 onClick을 `ButtonFlow.ClickNext`로 변경:

| 페이지 | Home 버튼 GameObject | 추가한 ButtonFlow fileID | SelfPage | NextPage |
|--------|----------------------|--------------------------|----------|----------|
| Result(`2131384639`) | `1818649564` | `1818649570` | 2131384639 | **869626634 (EndSelect)** |
| Review(`525015149`) | `2106297288` | `2106297294` | 525015149 | **869626634 (EndSelect)** |

- ButtonFlow guid: `0899471f9df43cc4b8da7dc8e66066b7`
- onClick: `m_Target` = 새 ButtonFlow, `ButtonFlow.ClickNext`, `m_Mode: 1`
- → `SceneFlowNavigator.ShowOnly(EndSelect, self)` 호출로 EndSelect 정상 복귀

> 이 수정은 빌드+설치까지 완료됨(09:28 APK). **단, 아래 3장 미해결 이슈로 재논의 필요.**

---

## 3. ⏳ 미해결 — Home(메인) vs 이전(EndSelect) 버튼 배치 (사용자 확인 대기)

### 사용자 추가 요구 (2026-06-28 09:46)
1. 영상보기에도 "이전(EndSelect)" 버튼 있는지 체크 → 양쪽에 추가됨(2장)
2. **점수보기/영상보기에서 "홈 버튼으로 메인 화면 가는 기능"이 안 보임** → 추가 필요
3. 수정 시 **APK는 만들지 말고 빌드(컴파일 검증)까지만**. "최종적으로 APK 만들어달라"고 하기 전까지 빌드까지만.

### 조사 결과
- **메인 화면(홈)** = `IntroLogo` (`1182241363`, FlowManager의 `FirstPage`)
- 표준 "홈" 버튼 동작 = `FlowManager.ResetPage()` (+ `BodyManager.ResetBodyManager()`)
  - 모든 SceneFlow 페이지 끄고 → IntroLogo 활성 + Body/Dance 매니저 리셋
  - 예시 버튼: GameObject `187625002` ("Home", 좌상단 -1325/801, 118x114, FlowManager fileID `763730350`)
- **Result/Review의 좌상단 "Home" 버튼**(`1818649564`, `2106297288`)은
  이름·위치(-1321/805)·크기(118x114)·아이콘(sprite guid `e6a50e48d3822b444a14237e66adbaab`)이
  **표준 홈 버튼과 동일** → 원래 "메인 화면(홈)으로" 가야 했던 버튼.
- 즉 2장에서 내가 이 Home 버튼을 EndSelect로 보냈으나, 사용자 의도는 "홈=메인".
- **EndSelect**(`869626634`)에는 결과보기/내영상보기 버튼만 있고 홈 버튼 없음.

### 결정 필요 사항 (AskQuestion 했으나 사용자가 중지)
- **Q1.** Result/Review의 좌상단 Home 버튼과 '이전' 기능 배치:
  - (A) Home = 메인(홈/ResetPage), '이전(EndSelect)' 버튼 신규 추가 ← 추천
  - (B) Home = 메인(홈)으로만, '이전' 버튼 없음
  - (C) Home = 이전(EndSelect) 유지(현재), 별도 '홈(메인)' 버튼 신규 추가
- **Q2.** EndSelect에도 홈(메인) 버튼을 추가할지

### 다음 작업 시
1. 위 Q1/Q2 결정 받기
2. 결정에 따라 `MBody.unity` 수정
   - "홈" 동작은 `FlowManager.ResetPage()` 호출(FlowManager fileID `763730350`) 패턴 사용
   - 새 버튼 추가가 필요하면 GameObject+RectTransform+Image+Button(+ButtonFlow) YAML 블록 추가, 부모 페이지 `m_Children`에 등록
   - 새 fileID는 기존과 충돌 없는 값 사용(추가 전 Grep로 확인)
3. **빌드(컴파일 검증)까지만** 수행. APK 생성/설치는 사용자가 명시 요청 시에만.
   - 컴파일만 검증하려면 에디터 배치 실행 또는 전체 APK 빌드 중 택1인데,
     사용자 지시는 "빌드 정상처리까지" = APK 산출물 만들지 말고 빌드 성공만 확인.

---

## 4. 이번 세션 변경 파일

| 파일 | 상태 |
|------|------|
| `ProjectSettings/ProjectVersion.txt` | 6000.4.1f1 → 6000.3.18f1 |
| `Packages/manifest.json` | collections 2.5.7, mcp 제거, barracuda 로컬 |
| `Packages/com.unity.barracuda/` | 신규 (로컬 패키지) |
| `Packages/packages-lock.json` | 삭제→재생성 |
| `Assets/Scenes/MBody.unity` | Result/Review Home 버튼 ButtonFlow 추가(→EndSelect) |
| `scripts/build-android.ps1` | Unity 6000.3.18f1 경로 자동 탐색 추가 |
| `docs/SESSION_HANDOFF_2026-06-28b.md` | 신규 (본 문서) |

> ⚠️ 3장(Home/이전 버튼 최종 배치)은 미구현 — 사용자 결정 후 진행.
