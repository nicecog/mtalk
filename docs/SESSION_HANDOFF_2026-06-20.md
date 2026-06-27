# M-Body Android 세션 작업 기록 (2026-06-20)

> **프로젝트:** `F:\PROJECT\Unity_Projects\MBody_Revised_Clean`  
> **Unity:** 6000.3.13f1  
> **패키지:** `com.CAU.MBody` (versionCode 1)  
> **대상 기기:** Galaxy Tab A8 SM-T290 (`R9WR611YAAJ`, armeabi-v7a, Android 11)  
> **서버:** `http://hudit.cafe24.com:8080/mbody`

---

## 0. 오늘 작업 요약

| 구분 | 결과 |
|------|------|
| ARMv7+ARM64 빌드 | ✅ Tab A8 ABI 대응 |
| APK+OBB 설치 (Tab A8) | ✅ APK 설치 성공, OBB 730MB 전송 완료(간헐적 USB EOF) |
| 로그인 화면 겹침 (끝내기·배경 씬) | ✅ 코드·씬 수정 (부분 검증) |
| 로그인 후 Intro 영상 미재생 | ⚠️ 진행 중 — `ScenePageVideoDriver` 추가 |
| 바디트레이닝 버튼 빈 화면 | ✅ `BodyTraining` 루트 비활성 버그 수정 |
| Unity 로고 후 로그인 안 나옴 | ✅ `BodyManager` null 크래시 수정 (빌드 완료, 설치 대기) |
| 진단 로그 (`MBodyDiagLog`) | ✅ 추가 |
| Intro MP4 원본 복원 | ✅ `MBody_Revised`에서 4개 복사 (PC smoke test PASS) |

---

## 1. 빌드·설치

### 산출물

| 파일 | 경로 | 크기 |
|------|------|------|
| APK | `Build/Android/MBody-latest-stable.apk` | ~37.57 MB |
| OBB | `Build/Android/main.1.com.CAU.MBody.obb` | 696.51 MB (730,345,360 bytes) |
| 빌드 로그 | `Build/Android/unity-android-build.log` | |
| 기기 진단 로그 | `Build/Android/diag.log` (pull 후) | |

### 빌드 설정

- IL2CPP, minSdk 25, targetSdk 36
- **ARMv7 \| ARM64** (`AndroidTargetArchitectures: 3`)
- `splitApplicationBinary = true` → **APK + OBB 필수**

### 설치 명령 (PowerShell)

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
$obb = "F:\PROJECT\Unity_Projects\MBody_Revised_Clean\Build\Android\main.1.com.CAU.MBody.obb"
$remote = "/sdcard/Android/obb/com.CAU.MBody/main.1.com.CAU.MBody.obb"

& $adb devices
& $adb -s R9WR611YAAJ shell "mkdir -p /sdcard/Android/obb/com.CAU.MBody"
& $adb -s R9WR611YAAJ install -r "F:\PROJECT\Unity_Projects\MBody_Revised_Clean\Build\Android\MBody-latest-stable.apk"
& $adb -s R9WR611YAAJ push $obb $remote
```

> `adb` 단독 명령은 PATH 미등록 시 실패. 위처럼 **전체 경로** 사용.

---

## 2. 발견한 구조·버그

### 2.1 `IntroVideo` 페이지 3개 (이름 중복)

| 위치 | 용도 | 클립 (GUID) |
|------|------|-------------|
| `Scenes/IntroVideo` | **로그인 후** M-BODY 소개 | `8a13191d…` (M-BODY 소개 영상.mp4) |
| `BodyTraining/IntroVideo` | 바디트레이닝 선택 후 | `9a7ea0b6…` (바디트레이닝소개_v2.mp4) |
| `DanceTraining/IntroVideo` | 댄스트레이닝 선택 후 | `d03dd064…` (댄스 트레이닝 소개 영상.mp4) |

`SceneFlowRegistry`는 이름만 키로 사용 → **중복 경고** 발생.  
로그인 경로는 `SkipButton (2)`가 `NextPage` **직접 참조**로 `Scenes/IntroVideo`에 연결됨 (레지스트리 무관).

### 2.2 로그인 화면 겹침

- **원인:** `BodyTraining` / `DanceTraining`이 Scenes 형제 순서상 뒤에 있고 기본 활성, 로그인 배경 반투명
- **조치:**
  - 씬에서 두 루트 기본 `m_IsActive: 0`
  - `LoginForm`: 배경 불투명, `Quit`/`Quit (1)` 숨김
  - `FlowManager`: 로그인·IntroVideo 시 트레이닝 루트 제어, 페이지 맨 앞으로

### 2.3 바디트레이닝 버튼 → 빈 화면

- **원인:** `IntroVideo` 표시 시 `SetTrainingRootsActive(false)`가 **부모 `BodyTraining`까지 비활성** → 자식 IntroVideo도 숨김
- **조치:** `ConfigureTrainingRootsForIntro()` — 활성 IntroVideo가 어느 루트 아래인지 보고 해당 루트만 켬

### 2.4 로그인 후 영상 미재생

- **diag.log:** `[Flow]` 로그는 있으나 **`[UiVideo]` 로그 0건** → `UiVideoPlayer.OnEnable` 미실행
- **PC 확인:** `IntroVideoSmokeTest.RunBatch` — 클립 `M-BODY 소개 영상` 1920×1080 로드 **PASS** (한글 파일명 문제 아님)
- **조치:** `ScenePageVideoDriver` 추가 — `RawImage + RenderTexture`로 페이지 표시 시 **명시적** Prepare/Play

### 2.5 Unity 로고 후 로그인 안 나옴 (최신 크래시)

- **원인:** `BodyTraining`/`DanceTraining` 비활성화 후 `FindFirstObjectByType<BodyManager>()`가 **null** → `ResetPage()`에서 `NullReferenceException`
- **조치:**
  ```csharp
  var bm = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
  if (bm != null) bm.ResetBodyManager();
  ```
- **상태:** 수정 반영 APK **빌드 완료**, Tab A8 **offline**으로 미설치

### 2.6 바디 IntroVideo Skip 버튼

- **잘못된 연결:** `NextPageName: PoseGuide`, `PackSceneToLoad: MBodyDancePack`
- **수정:** `NextPageName: MusicSelect`, `PackSceneToLoad: MBodyBodyPack`

---

## 3. 코드 변경 목록

| 파일 | 변경 요약 |
|------|-----------|
| `Assets/FlowManager.cs` | 트레이닝 루트 제어, `ScenePageVideoDriver` 연동, null-safe ResetPage, 진단 로그 |
| `Assets/LoginForm.cs` | 로그인 배경 불투명, Quit 숨김, 터치·키보드 설정 |
| `Assets/ButtonFlow.cs` | 씬 전환·에러 로그 |
| `Assets/TimerFlow.cs` | IntroLogo→MBodyLogin 타이머 로그 |
| `Assets/Script/SceneFlowNavigator.cs` | `GameObject` 기준 페이지 전달 |
| `Assets/Script/SceneFlowRegistry.cs` | 중복 SceneFlow 이름 경고 |
| `Assets/Script/ScenePackLoader.cs` | 팩 로드 로그, `FindObjectsInactive.Include` |
| `Assets/Script/UiVideoPlayer.cs` | Prepare 타임아웃·에러 로그, FitDisplay fallback |
| `Assets/Script/ScenePageVideoDriver.cs` | **신규** — Android UI 영상 명시 재생 |
| `Assets/Script/MBodyDiagLog.cs` | **신규** — 단계별 진단 로그 (파일+콘솔) |
| `Assets/Script/SceneFlowVideoPresenter.cs` | IntroVideo UiVideoPlayer 경로 복원 |
| `Assets/Script/SceneFlowVideoInstaller.cs` | 설치 로그 |
| `Assets/Editor/IntroVideoSmokeTest.cs` | **신규** — PC 클립 로드 smoke test |
| `Assets/latestVideoPlay.cs` | `ScenePageVideoDriver` 사용 |
| `Assets/Scenes/MBody.unity` | BodyTraining/DanceTraining 기본 비활성, 바디 Skip→MusicSelect |
| `scripts/restore-intro-videos.ps1` | 와일드카드로 MP4+meta 복원 |

---

## 4. 씬 흐름 (핵심 경로)

```
IntroLogo ─(TimerFlow 3초)→ MBodyLogin ─(로그인 성공)→ Scenes/IntroVideo (M-BODY 소개)
                                              └─ Skip → BodyDanceSelect
BodyDanceSelect ─(BodyTraining + MBodyBodyPack)→ BodyTraining/IntroVideo
                └─ Skip → MusicSelect (수정됨)
BodyDanceSelect ─(DanceTraining + MBodyDancePack)→ DanceTraining/IntroVideo
```

---

## 5. 진단 로그 사용법

### 기기 로그 파일

```
/sdcard/Android/data/com.CAU.MBody/files/MBody/diag.log
```

### Pull

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb pull /sdcard/Android/data/com.CAU.MBody/files/MBody/diag.log `
  F:\PROJECT\Unity_Projects\MBody_Revised_Clean\Build\Android\diag.log
```

### 주요 태그

| 태그 | 내용 |
|------|------|
| `[Flow]` | 페이지 표시, 트레이닝 루트 on/off |
| `[Navigate]` | SceneFlow 전환 |
| `[ButtonFlow]` | 버튼 다음 페이지 |
| `[TimerFlow]` | IntroLogo 타이머 |
| `[VideoDriver]` | 명시적 영상 Prepare/Play |
| `[UiVideo]` | UiVideoPlayer (보조) |
| `[Pack]` | MBodyBodyPack 등 additive 로드 |
| `[Registry]` | 중복 SceneFlow 이름 |

### 2026-06-20 diag.log에서 확인된 사항

- 로그인 → `Scenes/IntroVideo` 전환 정상, 클립 `M-BODY 소개 영상` 1920×1080 인식
- `[UiVideo]` / `[VideoDriver]` 로그 없음 (당시 빌드 기준)
- 바디 IntroVideo Skip 시 `PoseGuide` 조회 실패 반복 → 씬 수정으로 해결

---

## 6. MP4 원본 복원

`scripts/restore-intro-videos.ps1` — `MBody_Revised/Assets/Shader/*.mp4` → Clean 복사

| 파일 | 크기 |
|------|------|
| M-BODY 소개 영상.mp4 | 16.14 MB |
| 댄스 트레이닝 소개 영상.mp4 | 17.59 MB |
| 바디 트레이닝 소개 영상.mp4 | 17.14 MB |
| 바디트레이닝소개_v2.mp4 | 17.14 MB |

PC smoke test: `M-BODY 소개 영상` 1920×1080, 27.59s — **GUID 로드 정상**

---

## 7. 빌드·검증 명령

```powershell
# 빌드 (Unity Editor 닫은 상태)
powershell -File "F:\PROJECT\Unity_Projects\MBody_Revised_Clean\scripts\build-android.ps1" -Variant release

# 설치
powershell -File "F:\PROJECT\Unity_Projects\MBody_Revised_Clean\scripts\install-apk-obb.ps1"

# PC intro 클립 검증
Unity -batchmode -executeMethod IntroVideoSmokeTest.RunBatch -projectPath ...
```

---

## 8. 다음 세션 우선 작업

1. **Tab A8 USB 재연결** → 최신 APK 설치 (`BodyManager` 크래시 수정본)
2. **시작 흐름 확인:** Unity 로고 → IntroLogo(3초) → MBodyLogin
3. **로그인 후 영상:** `[VideoDriver] Playing` 로그·화면 확인
4. **바디트레이닝:** IntroVideo 재생 → Skip → MusicSelect
5. 문제 시 `diag.log` pull 후 `[VideoDriver]` / `[Flow]` 분석

---

## 9. 미완료·알려진 이슈

| 항목 | 상태 |
|------|------|
| 최신 APK Tab A8 설치 | ⏳ 기기 offline |
| 로그인 후 영상 실기 재생 | ⏳ `ScenePageVideoDriver` 빌드 후 재검증 |
| USB 대용량 OBB push EOF | ⚠️ 재시도·케이블 안정화 필요 |
| IntroVideo 이름 중복 (레지스트리) | 💡 장기: 고유 이름 또는 경로 기반 조회 |
| `docs/cleanup-deleted-files.txt`의 삭제 MP4 | 바디 트레이닝 소개 영상.mp4 — 원본에서 복원됨 |

---

## 10. 관련 문서

- `docs/SESSION_HANDOFF_2026-06-18.md`
- `docs/ANDROID_DEVICE_PROFILING.md`
- `docs/PROJECT_STATUS.md`

---

*마지막 빌드: 2026-06-20 — `BodyManager` null 크래시 수정 + `ScenePageVideoDriver` 포함. Tab A8에 설치 후 E2E 재검증 필요.*
