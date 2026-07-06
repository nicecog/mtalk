# M-Body 세션 작업 기록 (2026-06-28)

> **프로젝트:** `c:\project\Unity_Projects\MBody_Revised_Clean`
> **Unity:** 6000.3.13f1 (제거됨) → 현재 머신에는 **6000.4.1f1** 만 설치
> **패키지:** `com.CAU.MBody`
> **대상 기기:** Galaxy Tab A8 SM-T290 (`R9WR611YAAJ`, armeabi-v7a, Android 11)
> **이전 세션:** [`SESSION_HANDOFF_2026-06-27.md`](./SESSION_HANDOFF_2026-06-27.md)

---

## 0. 오늘 작업 요약

| 구분 | 결과 |
|------|------|
| 2회차/3회차 게임 종료 후 "내영상 보기" 화면 멈춤 | ✅ 코드 + 씬 연결 수정 완료 |
| 기존 APK + OBB Tab A8 설치 | ✅ 설치 완료 (수정 전/직후 빌드본) |
| 수정분 반영 신규 APK 빌드 | ❌ **미완료** — Unity 라이선스 + Android Build Support 모듈 문제 |

> ⚠️ **중요:** "내영상 보기" 수정 코드는 **소스에는 반영**되어 있으나, 그 코드를 포함한 **신규 APK는 아직 빌드되지 않았다.** 현재 태블릿에 깔린 APK(빌드 시각 06-27 13:16)에 수정이 포함됐는지는 미검증.

---

## 1. "내영상 보기" 화면 멈춤 수정

### 증상
3회차 게임 모두 종료 → `EndSelect` 화면 → **"내영상 보기"** 클릭 → 화면이 그냥 멈춤(빈 화면).

### 근본 원인
`EndSelect`의 "내영상 보기" 버튼(`954577162`)의 `NextPage`가 실제 Review 페이지가 아니라 **비활성 UserPop 안의 숨겨진 오버레이 오브젝트**(`386198821`)를 가리킴.
`SceneFlowNavigator.ShowOnly()`가 모든 SceneFlow 페이지를 끄고 그 오브젝트만 켜면서 빈 화면처럼 멈춤.

올바른 타깃: SceneFlow 태그가 붙은 `Review` 페이지(`525015149`, `BodyReview` 컴포넌트).

### 수정 내용

| 파일 | 변경 |
|------|------|
| `Assets/Script/SceneFlowNavigator.cs` | 비-SceneFlow `Review`/`Result` 이름 오브젝트를 실제 SceneFlow 페이지로 변환(`BodyReview`/`VideoReview`/`CSVScroll` 컴포넌트 기준 해석). 부모 계층 비활성 시 `EnsureHierarchyActive`로 활성화 |
| `Assets/BodyReview.cs` | 녹화 파일 필터를 `BodyThird` + `BodyAlign` + `BodyThirdPlay` 로 확장. 빈 목록·범위 초과 가드 추가. 재생 시 `ScenePageVideoDriver.PlayPlayer` 사용 |
| `Assets/VideoThumbnailGenerator.cs` | 썸네일 생성 대기 루프에 **8초 타임아웃** 추가(무한 대기 방지) |
| `Assets/Scenes/MBody.unity` | EndSelect "내영상 보기" `NextPage`: `386198821` → **`525015149`** (BodyReview 페이지) / "결과 보기" `NextPage`: `1505581889` → **`2131384639`** (Result 페이지) |

### 관련 ID 참고 (MBody.unity)

| fileID | 의미 |
|--------|------|
| `869626634` | `EndSelect (1)` 페이지 (SceneFlow) |
| `954577162` | EndSelect "내영상 보기" 버튼(ButtonFlow) |
| `386198821` | (구) 잘못된 타깃 — UserPop 내부 Review 오버레이 (비활성) |
| `525015149` | ✅ 올바른 Review 페이지 (SceneFlow, `BodyReview` guid `23895a7fe45d7d649b841fab48ed9182`) |
| `2131384639` | ✅ Result 페이지 (SceneFlow, `CSVScroll` guid `d5e17d34a6488ed41b7bbbd881e66250`) |
| `1505581889` | (구) UserPop 내부 Result 오버레이 (비활성) |

### 흐름 참고
```
BodyManager.EndMainGame(): playTimes<3 → MusicSelect / playTimes>=3 → EndSelect (1)
EndSelect → "내영상 보기"(ButtonFlow.ClickNext) → SceneFlowNavigator.ShowOnly → Review(BodyReview)
```
- "내영상 보기"는 `playTimes >= 3` (3회차 모두 종료) 일 때만 EndSelect에서 노출.

---

## 2. APK 빌드 / 설치 상태

### 2-1. 기존 APK 설치 (성공)
- `Build/Android/MBody-latest-stable.apk` (37.6 MB, 빌드 06-27 13:16) → Tab A8 설치 성공
- `Build/Android/main.1.com.CAU.MBody.obb` (696.5 MB) → 기기 푸시 성공
  - 원격 경로: `/sdcard/Android/obb/com.CAU.MBody/main.1.com.CAU.MBody.obb`
- adb: `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`, 시리얼 `R9WR611YAAJ`

### 2-2. 신규 빌드 실패 이력

1. **Unity 라이선스 만료** (1차)
   - `No valid Unity Editor license found` / `LicenseGroupOfflineValidityPeriodIsExpired`
   - → 사용자가 Unity Hub에서 로그인/라이선스 처리 → **해결됨** (`Licensing is initialized` 확인)

2. **Android Build Support 모듈 없음** (2차, 현재 막힌 지점)
   - 빌드 로그: `Error building player because build target was unsupported`
   - 원인: 6000.3.13f1 제거 후 현재 **6000.4.1f1** 만 설치됐는데 Android 모듈 미설치
   - `AndroidPlayer/SDK`, `NDK`는 설치됐으나 **본체 `UnityEditor.Android.Extensions.dll`, `Variations`, `OpenJDK` 누락**
   - Hub CLI 재설치 시도 → `Omitting module android because it's already installed` (modules.json `selected` 플래그만 보고 건너뜀)
   - `modules.json` 의 Android Build Support / OpenJDK `selected` 를 `false` 로 수정 후 재설치 → `Android Build Support failed to install. Error given: Installation Failed` 로 재실패

### 2-3. 환경 메모
- Unity Editor: `C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Unity.exe`
- Unity Hub: `C:\Program Files\Unity Hub\Unity Hub.exe`
- Hub CLI 설치 명령:
  ```
  & "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.4.1f1 -m android android-open-jdk-17.0.9+9 --childModules
  ```
- `modules.json` 백업: `modules.json.bak`, `modules.json.bak2` (Editor 폴더)
- `paused-downloads.json` 초기화함(`%APPDATA%\UnityHub\`, 백업 `.bak`)
- Hub 다운로드 캐시: `%APPDATA%\UnityHub\downloads`
- 설치 로그: `%APPDATA%\UnityHub\logs\install-log.json`
- modules.json은 중복 키(`preSelected`/`preselected`) 때문에 Windows PowerShell `ConvertFrom-Json` 파싱 불가 (pwsh 미설치)

### 2-4. 빌드 스크립트 변경
- `scripts/build-android.ps1`: Unity 경로를 6000.3.13f1 → 없으면 **6000.4.1f1** 자동 선택하도록 수정.

---

## 3. 다음 세션 To-Do (우선순위)

1. **Android Build Support 모듈 정상 설치** (현재 블로커)
   - 방법 A: Unity Hub **GUI**에서 6000.4.1f1 → Add Modules → Android Build Support(+ SDK/NDK/JDK) 설치 (GUI가 CLI보다 검증 안정적)
   - 방법 B: 6000.3.13f1 을 Hub로 재설치(Android 모듈 포함). 프로젝트 원본 버전과 일치 → 가장 안전
   - 검증: `…\AndroidPlayer\UnityEditor.Android.Extensions.dll`, `…\AndroidPlayer\OpenJDK\bin\java.exe` 존재 확인
2. **APK 재빌드**
   ```
   cd c:\project\Unity_Projects\MBody_Revised_Clean
   powershell -File scripts\build-android.ps1
   ```
   - 빌드 로그: `Build\Android\unity-android-build.log` 에서 `Android APK build succeeded` 확인
3. **설치 + 실기 검증**
   ```
   powershell -File scripts\install-apk-obb.ps1
   ```
   - 테스트: 3회차 종료 → EndSelect → "내영상 보기" → Review 페이지 썸네일 표시 + 탭 시 영상 재생
   - "결과 보기" → Result 페이지 정상 표시
4. (선택) 라이선스가 다시 만료되면 Unity Hub 로그인 재처리

---

## 4. 변경 파일 목록 (이번 세션)

| 파일 | 상태 |
|------|------|
| `Assets/Script/SceneFlowNavigator.cs` | 수정 (Review/Result 해석 로직 추가) |
| `Assets/BodyReview.cs` | 수정 (파일 필터 확장, 가드, 재생 경로) |
| `Assets/VideoThumbnailGenerator.cs` | 수정 (타임아웃) |
| `Assets/Scenes/MBody.unity` | 수정 (EndSelect 버튼 NextPage 2건) |
| `scripts/build-android.ps1` | 수정 (Unity 버전 자동 탐색) |
| `docs/SESSION_HANDOFF_2026-06-28.md` | 신규 (본 문서) |

> ⚠️ 위 소스 수정분은 **빌드에 아직 반영되지 않음.** Android 모듈 설치 후 재빌드 필요.
