# M-Social_Clean 작업 이력

> **최종 갱신:** 2026-06-18 (코드 v1.38 + 로그인 정리, APK v1.38)  
> **프로젝트:** `F:\PROJECT\Unity_Projects\M-Social_Clean`  
> **Unity:** 6000.3.13f1  
> **서버:** `http://hudit.cafe24.com:8080/msocial`  
> **테스트 계정:** `hudit` / `hudit` (수동 입력 — `TestAutoLogin` 제거됨)  
> **대상 기기:** Galaxy Tab A8 (2019, 2GB RAM, `armeabi-v7a`, Android 11)

---

## 0. 최근 업데이트 (2026-06-18)

### 0-a. 로그인 — `TestAutoLogin` 제거 (코드만, APK 미반영)

**변경:** `Login.cs`에서 테스트용 `hudit`/`hudit` 강제 입력(`TestAutoLogin`) 삭제.

- 로그인 버튼 → `SyncCredentials()` → ID/PW **빈 값 검증** → 서버 `sign-in`
- **다음 APK 빌드 시 반영 필요** (현재 배포 APK v1.38은 이전 빌드일 수 있음)

**관리자 vs 사용자 로그인 (코드 분석):**

| 항목 | 내용 |
|------|------|
| UI | 로그인 화면에 사용자/관리자 구분 **표시는 있을 수 있음** |
| 코드 | **동일** `loginWithID()` → `AdminLogin()` 코루틴 (전 사용자 공통) |
| `Login.isAdmin` | 씬에 `true` 직렬화만 있고 **코드에서 미사용** (죽은 필드) |
| `ID == "admin"` 만 | `playNum=8`, CSV `Upload()` **생략**, Stage1 씬 마스크 `prv=2` |
| 그 외 | Stage1/2 영상·피드백·씬 전환 **동일** |

내부 API용 `super-admin`은 신규 회원 **자동 가입** 시에만 사용(UI 관리자 로그인과 무관).

---

### 0-b. Stage1 피드백 MP4 한글 → `fb_XXX` (v1.38)

**증상:** 심호흡(Breath) 등 피드백에서 `Prepare failed:` + 긴 한글 URL 표시 후 재생 — 한글 파일명·캐시·`Uri` 타임아웃.

**조치:**

| 항목 | 내용 |
|------|------|
| 파일 rename | StreamingAssets 루트 MP4 **15개** → `fb_001.mp4`~`fb_015.mp4` |
| 매핑 JSON | `Assets/StreamingAssets/feedback_names.json` (asis `legacyFileName` / tobe `newFileName` / `feedbackPage`) |
| 씬 `vcs[]` | `MSocial_Stage1.unity`, `MainScene.unity` 전부 `fb_###.mp4` |
| 스크립트 | `scripts/rename_feedback_mp4.py` (Unity YAML `\uXXXX` **대문자** hex 치환) |
| 래퍼 | `scripts/Rename-FeedbackMp4.ps1` |

**fb_XXX 매핑 요약:**

| ID | 구 한글명 (요약) | FeedbackPage |
|----|------------------|--------------|
| fb_001 | 숫자세기 트레이닝 영상 | CountNumber |
| fb_002~005 | 연주해보기_바디 1~4 | PlayInstrument |
| fb_006~009 | 연주해보기_사운드 1~4 | PlayInstrument |
| fb_010~013 | 춤추기 1~4 | Dance |
| fb_014~015 | 호흡 트레이닝_균일/박스 | Breath |

---

### 0-c. 피드백 영상 Prepare 안정화 (v1.37 코드 → v1.38 APK 포함)

**파일:** `MSocialStreamingAssetPaths.cs`, `MSocialVideoPlayerHelper.cs`

| 개선 | 설명 |
|------|------|
| ASCII 캐시명 | `fb_###.mp4`는 `GetCacheFileName()` 그대로 사용 (한글만 `cache_<md5>.mp4`) |
| Android URL 순서 | `ToLocalFileUrlVariants()` — **raw path 우선**, `file://`, `Uri.AbsoluteUri` |
| 피드백 Prepare | `PrepareFeedbackClip()` — 타임아웃 **12초**, 중간 URL 실패 **화면 알림 없음** |
| 최종 실패만 | 모든 URL 실패 시 `Prepare ALL failed:` 표시 |

---

### 0-d. 실기기 테스트 전 정적 검증 + APK 빌드 (v1.38)

**검증 스크립트 (전부 FAIL=0):**

| 스크립트 | 검증 내용 |
|----------|-----------|
| `scripts/Validate-Stage1Flow.ps1` | FeedbackPage 순서, Stage1Negative→Breath 호출 체인, nextButton/s1m/vp 배선 |
| `scripts/Validate-PreDevice.ps1` | fb MP4 디스크·씬·Prepare 경로·ProgressDone 흐름 |
| `scripts/Validate-FeedbackMp4.ps1` | vcs[] ↔ StreamingAssets 15개 일치 |

**슬퍼요 → 심호흡 호출 순서 (검증됨):**

```
ScenePlayer (시나리오 MP4) → EmotionSelect (슬퍼요)
  → Stage1Negative (심호흡 선택 + 확인)
  → HideWatchScene + fbm.startFeedback(7) → Breath
  → 안내 음성 → PrepareFeedbackClip(fb_014|015) → vp.Play
  → 확인 → Stage1Count.ClickNextButton → ProgressDone
  → fbm off, sceneselect off → Stage1 카드 허브 복귀
```

**APK (v1.38):**

```
F:\PROJECT\Unity_Projects\M-Social_Clean\Build\Android\MSocial-latest-stable.apk
```

| 항목 | 값 |
|------|-----|
| bundleVersion | **1.38** |
| versionCode | **26** |
| 용량 | **263.86 MB** |
| 빌드 시각 | **2026-06-17 22:35** |
| 로그 | `Build/Android/build-v1.38.log` |

**첫 설치 시 체감 지연:** 피드백 MP4는 APK→`persistentDataPath/Movies/` 전체 복사 후 재생. `fb_002~005`(8~9MB)는 안내 음성 후 멈춤처럼 보일 수 있음. `fb_014/015`(2.4~2.9MB)는 상대적으로 짧음.

---

## 0-1. 이전 업데이트 (2026-06-18, v1.36)

### Stage1 트레이닝 「확인」 버튼 무반응 수정

**증상:** 슬퍼요 → 다독이기 선택(심호흡·노래·악기 등) → **확인** 눌러도 아무 동작 없음.

**근본 원인:** `Stage1Negative.userdata`가 씬에서 `{fileID: 0}`(null). `ClickNextButton()`이 `userdata.logUpload()` 호출 시 **NullReferenceException** → 피드백 페이지(`9-3. Stage1Feedback`)까지 도달하지 못함.

**검증 결과 (피드백 페이지·영상):**

| 항목 | 결과 |
|------|------|
| 페이지 존재 | ✅ `MSocial_Stage1` 내 `Breath`, `PlayInstrument`, `Sing`, `ListenMusic` 등 모두 존재 |
| BOM | ✅ 피드백 MP4는 `movie_index.json` 미사용 — BOM 이슈 없음 |
| 한글 파일명 MP4 | ✅ StreamingAssets 15개 전부 디스크·씬 `vcs[]` 일치 (`scripts/Validate-FeedbackMp4.ps1`) |
| Sing / ListenMusic | ⚠️ 원본부터 `vcs: []` — **오디오+UI 전용**(영상 없음, 설계상) |
| Breath / PlayInstrument | ✅ 한글 MP4 + `MSocialVideoCache` 경로 (`Stage1Count`) |

**수정:**
- `MSocialStageWireup` — `Stage1Negative.userdata` 복구
- `Stage1Negative` — OnEnable 시 userdata resolve, logUpload null-safe, fbm 활성화 순서 수정, 진단 로그
- `Stage1FeedbackManager` — 페이지 활성화 로그

**APK:** **1.36 (versionCode 24)**

---

## 0-1. 이전 업데이트 (2026-06-17, v1.35)

### Stage1 부정 감정 → 트레이닝 화면 전환 수정

**증상:** 집 → 엄마가 청소하래요 → 슬퍼요 → 「슬픈 마음을 잘 다독일 수 있을까요?」에서 **심호흡 해보기**(또는 다른 선택) 후 **이전 화면(감정 선택)** 으로 돌아가 보임.

**원인:** 별도 앱/씬 부재가 아님. `MSocial_Stage1` 씬 안에 `9-3. Stage1Feedback` → `Breath` 페이지가 이미 존재함. `Stage1Negative`를 끌 때 아래에 남아 있던 `EmotionSelect`(감정 선택 UI)가 다시 보여 전환이 실패한 것처럼 보였음.

**의도된 흐름 (같은 앱, 같은 Stage1 씬):**

| 단계 | GameObject | 설명 |
|------|------------|------|
| 영상 | `8. Stage1WatchScene` | 시나리오 MP4 |
| 감정 | `EmotionSelect` | 슬퍼요 등 선택 |
| 다독이기 | `9-2. Stage1Negative` | 3가지 대처 선택 |
| 트레이닝 | `9-3. Stage1Feedback` | Sing / Imagine / **Breath** 등 |

슬픈 감정(`mainEmotion=1`) 선택지 코드: `24568` → 노래(5), 상상(2), 심호흡(8) 등. 심호흡은 `FeedbackPage[7]` = `Breath` (`Stage1Count` + StreamingAssets 호흡 MP4).

**수정 파일:**

| 파일 | 변경 |
|------|------|
| `EmotionSelect.cs` | 다독이기(neg/pos)로 넘어갈 때 감정 선택 UI 숨김 |
| `Stage1Negative.cs` | 트레이닝 진입 시 WatchScene·EmotionSelect 숨김, `selectIndex`/`fbm` 가드 |
| `Stage1FeedbackManager.cs` | `FeedbackPage` 인덱스 범위 검사 + 로그 |
| `Stage1Count.cs` | Breath 등 `ac` null 시 `PlayOneShot` 스킵 |

**APK:** **1.35 (versionCode 23)** — `Build/Android/MSocial-latest-stable.apk` (~264 MB)

---

## 0-1. 이전 업데이트 (2026-06-17, v1.34)

- Stage1 영상 재생 검은 화면 이슈를 추적하며 `movie_index.json` BOM 문제를 제거했고, 로드 안정성(Strip BOM + timeout/fallback)을 보강했습니다.
- 테스트용으로 넣었던 Camera Far Plane 진단 코드와 상시 온스크린 상태 텍스트를 제거해 기본 `RenderTexture + RawImage` 경로로 복원했습니다.
- Stage1 훈련 버튼 흐름 점검 결과, 씬 직렬화 `onClick` 61개를 전수 검사했고 누락 2개를 보완했습니다.
  - `Stage1Count.set_buttonPressed(bool)`
  - `Stage1Listen.set_pressbutton(bool)`
- 위 setter 보완 시 C# 자동 프로퍼티 setter 이름 충돌이 있어, 두 스크립트의 bool 상태를 프로퍼티에서 필드로 변경했습니다.
- 최신 빌드 기준 APK 버전: **1.34 (versionCode 22)**.

---

## 목차

1. [작업 요약](#1-작업-요약)
2. [최신 APK](#2-최신-apk)
3. [APK 빌드 이력](#3-apk-빌드-이력)
4. [씬 분리 (3-Scene)](#4-씬-분리-3-scene)
5. [Tab A8 화면·UI 이슈 (v1.19–v1.24)](#5-tab-a8-화면ui-이슈-v119v124)
6. [영상 재생 (v1.25–v1.27)](#6-영상-재생-v125v127)
7. [영상 재생 아키텍처](#7-영상-재생-아키텍처)
8. [시나리오 코드·카테고리 매핑](#8-시나리오-코드카테고리-매핑)
9. [소스 수정 내역 (전체)](#9-소스-수정-내역-전체)
10. [MP4·StreamingAssets](#10-mp4streamingassets)
11. [Galaxy Tab A8 대응](#11-galaxy-tab-a8-대응)
12. [검증 스크립트](#12-검증-스크립트)
13. [실기기 테스트·로그](#13-실기기-테스트로그)
14. [다음 단계](#14-다음-단계)
15. [참고 문서](#15-참고-문서)

---

## 1. 작업 요약

M-Social_Clean을 CAFE24 서버 배포·Galaxy Tab A8 실기기 대응까지 진행했습니다.

| 단계 | 내용 | 결과 |
|------|------|------|
| API·로그인 | `MSocialApiClient`, Login/FlowManager 리팩터 | ✅ |
| MP4 압축 | 83개 → 123 MB, APK ~270 MB | ✅ |
| Tab A 크래시 | 저사양 프로필, GLES3, ARMv7 only | ✅ |
| 지연 로딩 | `movie_index.json` + StreamingAssets URL | ✅ |
| **씬 분리** | Bootstrap / Stage1 / Stage2 Additive | ✅ |
| **로그인·흰/검은 화면** | DDOL 제거, Wireup, Canvas 2800×1752 | ✅ (v1.19–v1.24) |
| **Stage1 영상 재생** | URL 인코딩, 캐시, Prepare/Play 통합 | ✅ v1.33–v1.34 실기기 재생 확인 |
| **Stage1 트레이닝 UI** | 부정 감정 → Breath/Sing/Imagine 전환 | ✅ v1.35 |
| **확인 버튼 무반응** | Stage1Negative userdata null | ✅ v1.36 |
| **피드백 MP4 영문화** | `fb_001`~`fb_015`, Prepare 안정화 | ✅ v1.38 APK |
| **로그인 정리** | TestAutoLogin 제거, 관리자 분기 분석 | ✅ 코드 (APK 재빌드 대기) |
| **실기기 E2E** | v1.38 심호흡·Prepare·ProgressDone | **대기** |

---

## 2. 최신 APK

```
F:\PROJECT\Unity_Projects\M-Social_Clean\Build\Android\MSocial-latest-stable.apk
```

| 항목 | 값 |
|------|-----|
| 버전 | **1.38** (versionCode **26**) |
| 패키지 | `com.Spike.MSocial` |
| minSdk | 25 |
| targetSdk | 33 |
| ABI | **`armeabi-v7a` only** (Tab A8 32-bit) |
| Graphics API | OpenGLES3 |
| 용량 | **263.86 MB** |
| 빌드 시각 | 2026-06-17 22:35 |

> **참고:** `Login.TestAutoLogin` 제거는 **코드에만 반영**. 배포용 최신 동작은 **v1.39 재빌드** 후 적용.

빌드 명령:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "F:\PROJECT\Unity_Projects\M-Social_Clean" `
  -executeMethod AutomatedAndroidBuild.BuildLatestStableAndroidApk `
  -logFile "F:\PROJECT\Unity_Projects\M-Social_Clean\Build\Android\build-v1.38.log"
```

---

## 3. APK 빌드 이력

| 빌드 | 일시 | 버전 | 비고 |
|------|------|------|------|
| 초기 | 2026-06-14 | 1.13–1.14 | SDK33, MP4 압축 전 |
| MP4 압축 후 | 2026-06-14 | 1.14 / code 2 | ~269 MB |
| Tab A fix | 2026-06-14 | 1.15 / code 3 | 저사양·GLES3 |
| 지연 로딩·ARMv7 | 2026-06-15 | 1.16 / code 4 | on-demand MP4 |
| 씬 분리·로그인 | 2026-06-15 | 1.17–1.21 | Bootstrap 3씬 |
| 흰/검은 화면 | 2026-06-15 | 1.22–1.23 | Wireup, Canvas |
| UI 잘림 | 2026-06-15 | 1.24 | CanvasScaler 2800×1752 |
| 영상 1차 | 2026-06-15 | 1.25–1.26 | URL 인코딩, PlayPrepared |
| **영상 캐시** | **2026-06-15** | **1.27 / code 15** | **persistentDataPath 캐시** |
| **경로/진단 보강** | **2026-06-16** | **1.30~1.33** | ASCII 경로, 인덱스/BOM/fallback 점검 |
| **정리/안정화** | **2026-06-17** | **1.34 / code 22** | 진단 UI 제거, 버튼 바인딩 보완 |
| **트레이닝 화면 전환** | **2026-06-17** | **1.35 / code 23** | 심호흡·피드백 페이지 노출 수정 |
| **확인 버튼 무반응** | **2026-06-18** | **1.36 / code 24** | Stage1Negative userdata null 수정 |
| **피드백 Prepare·캐시** | **2026-06-18** | **1.37 / code 25** | ASCII 캐시, raw path 우선, 피드백 12s 타임아웃 |
| **피드백 MP4 fb_XXX** | **2026-06-17** | **1.38 / code 26** | 한글 15개 rename, `feedback_names.json`, 정적 검증+APK |

---

## 4. 씬 분리 (3-Scene)

`MainScene.unity` 단일 씬 → Additive 3씬 구조로 전환 완료.

| Index | 씬 | 내용 |
|-------|-----|------|
| 0 | `MSocial_Bootstrap.unity` | IntroLogo, Login, IntroMovie, Canvas, FlowManager |
| 1 | `MSocial_Stage1.unity` (additive) | Stage1Start, Stage1Intro, Stage1SelectMap, Stage1WatchScene, 피드백 등 |
| 2 | `MSocial_Stage2.unity` (additive) | Stage2 전체 |

### 핵심 런타임

| 파일 | 역할 |
|------|------|
| `MSocialSceneRouter.cs` | Additive 로드/언로드, Page 재부착 |
| `MSocialPageRegistry.cs` | PageId → GameObject |
| `MSocialAppServices.cs` | Login·FlowManager 바인딩 (Login DDOL **금지**) |
| `MSocialStageWireup.cs` | `userdata`, `EmotionSelect.scp` 등 크로스 씬 참조 복구 |
| `MSocialPageMoviePlayer.cs` | Stage1Intro / Stage2Intro URL 재생 |
| `MSocialIntroMoviePlayer.cs` | Bootstrap IntroMovie |

상세 설계: [SCENE_SPLIT_DESIGN.md](./SCENE_SPLIT_DESIGN.md)

### 로그인 후 플로우 (현재)

```
IntroLogo → Login (ID/PW 수동) → (IntroMovie 생략) → Stage1Start → Stage1SelectMap
  → 카테고리 선택 → SceneSelect → Stage1WatchScene (영상) → EmotionSelect
  → (부정 감정) Stage1Negative → Stage1Feedback (Breath 등) → ProgressDone → 카드 허브
```

### 로그인 구현 (`Login.cs`)

| 항목 | 동작 |
|------|------|
| 진입 | `loginWithID()` → `AdminLogin()` 코루틴 (이름만 Admin, 전 사용자 동일) |
| 신규 가입 | `super-admin`으로 sign-in 후 `/api/users/accounts` 생성 |
| 일반 사용자 | 로그인 후 `Upload()` → 서버 CSV로 `playNum`·`ItemString` 복원 |
| `ID == "admin"` | `playNum=8`, `Upload()` 생략, Stage1 마스크 `prv=2` |
| `Login.isAdmin` | **미사용** (UI 탭과 코드 미연결) |
| v1.38 이후 | `TestAutoLogin` 제거 — **v1.39 APK 빌드 필요** |

---

## 5. Tab A8 화면·UI 이슈 (v1.19–v1.24)

| 버전 | 증상 | 원인 | 수정 |
|------|------|------|------|
| v1.19 | 스플래시 후 흰 화면 | Login `DontDestroyOnLoad`로 Canvas 밖 이탈 | DDOL 제거, Bootstrap Show |
| v1.20–v1.21 | 로그인 후 흰 화면·해상도 낮음 | IntroMovie NextPage 깨짐, Intro RT 제거 | Login→Stage1Start→SelectMap, PageMoviePlayer |
| v1.22–v1.23 | 잠깐 보이다 검은 화면 | `Stage1Manager.userdata` null, Page Canvas 미부착 | `MSocialStageWireup`, Page flatten |
| v1.24 | 「학교」 등 라벨 잘림 | CanvasScaler 1920×1200 축소 | **2800×1752**, `matchWidthOrHeight=0` |

---

## 6. 영상 재생 (v1.25–v1.27)

### 증상

- Stage1 시나리오 선택 후 **안내 문구는 나오나 영상 미재생**
- 화면 **하얗게**만 표시 (배경 Image 흰색 + RawImage 텍스처 없음)
- 감정 선택(EmotionSelect)은 영상 없이 나타남

### 원인 분석

| # | 원인 | 설명 |
|---|------|------|
| 1 | **Android `jar:` URL** | `Application.streamingAssetsPath` + 한글 파일명 → `VideoPlayer.Prepare()` 실패 |
| 2 | **준비 타이밍** | 7초 대기 전 Prepare 후 상태 소실, 또는 대기 후에만 Prepare |
| 3 | **RT 최적화** | `MSocialVideoRtOptimizer`가 씬 1080p RT 제거 → 재생 실패 시 흰 배경만 노출 |
| 4 | **재생 확인 없음** | `vp.Play()` 후 `isPlaying` 미확인 |

### 버전별 수정

| 버전 | 수정 |
|------|------|
| v1.25 | `Prepare()` 후 `Stop()` 제거, `MSocialVideoPaths` 한글 URL 인코딩 |
| v1.26 | `PlayPrepared()`, URL fallback, 7초 **후** Prepare, `SceneIndex` 검증 로그 |
| **v1.27** | **`MSocialVideoCache`** — APK→`persistentDataPath` 복사 후 `file://` 재생, 7초와 **병렬** Prepare, ScenePlayer RT 제거 제외, 검은 화면 대기 |

### v1.27 핵심: `MSocialVideoCache`

Android에서 `UnityWebRequest`로 APK 내 MP4를 읽어 캐시:

```
persistentDataPath/Movies/{파일명}.mp4  →  file:///... 재생
```

- **시나리오 MP4:** 한글 legacy는 `cache_<md5>.mp4` (v1.37+)
- **피드백 MP4 (v1.38):** `fb_014.mp4` 등 **ASCII 파일명 그대로** 캐시
- 캐시 히트 시 즉시 `file://` / raw path 사용
- 실패 시 jar URL fallback (시나리오 경로)

### v1.37–v1.38 피드백 추가 (2026-06-18)

| 항목 | 내용 |
|------|------|
| `PrepareFeedbackClip` | 12초 타임아웃, 중간 URL 실패 알림 숨김 |
| `ToLocalFileUrlVariants` | Tab A8: raw path → `file://` → `Uri` 순 |
| `feedback_names.json` | 15개 asis/tobe 매핑 |
| 첫 설치 지연 | 8MB급(`fb_002~005`)은 안내 음성 **이후** 복사 대기 체감 큼 |

---

## 7. 영상 재생 아키텍처

| 컴포넌트 | 역할 | 경로 소스 |
|----------|------|-----------|
| `MSocialIntroMoviePlayer` | Bootstrap IntroMovie | `movie_index.intro[2]` |
| `MSocialPageMoviePlayer` | Stage1Intro(idx 1), Stage2Intro(idx 3) | `movie_index.intro` |
| **`ScenePlayer`** | **Stage1 시나리오 시청** | `MSocialMovieLibrary.GetStage1Path(SceneIndex)` |
| `Stage2ScenePlay` | Stage2 시나리오 | `GetStage2Path` |
| `Stage1Count/Listen/Dance` 등 | 피드백 클립 | `PrepareFeedbackClip` + `vcs[]` (`fb_###.mp4`) |

### `ScenePlayer` 흐름 (v1.27)

```
OnEnable
  → Wireup (userdata)
  → movie_index 로드 대기
  → code = sceneCategory*100 + (sceneNumber + 2*nowPage)
  → VideoCodes[] 매칭 → SceneIndex
  → RawImage 검은색
  → [병렬] PrepareUrl (캐시→file://) + 7초 대기
  → PlayPrepared() (isPlaying·종료 대기)
  → EmotionSelect 표시
```

### 공통 헬퍼

| 파일 | 역할 |
|------|------|
| `MSocialMovieLibrary.cs` | `movie_index.json` 지연 로드 |
| `MSocialVideoPaths.cs` | StreamingAssets URL (raw + UTF-8 인코딩) |
| `MSocialVideoCache.cs` | **Android persistentDataPath 캐시** |
| `MSocialVideoPlayerHelper.cs` | PrepareUrl, PlayPrepared, RT 바인딩 |
| `MSocialVideoRtOptimizer.cs` | 저사양 씬 RT 제거 (**ScenePlayer 제외**) |
| `MSocialVideoGuard.cs` | Bootstrap 씬에서만 VideoPlayer Disarm |

### `movie_index.json`

- `Assets/StreamingAssets/Movie/movie_index.json`
- stage1: 32개, stage2: 32개, intro: 4개
- StreamingAssets MP4: **84개** (빌드에 포함)

---

## 8. 시나리오 코드·카테고리 매핑

6개 지도 카드 = `sceneCategory` 1~6.  
표시·재생 코드: `code = category * 100 + (sceneNumber + 2 * nowPage)`

### 「실수를 했어요」 위치 (참고)

| 항목 | 값 |
|------|-----|
| titles 인덱스 | 2 |
| VideoCode | **501** |
| 카테고리 | **5 (상점)** — 학교(1) 아님 |
| movie_index | `Movie/Stage1/1-16/교실-마이크.mp4` |

### 학교(카테고리 1) 영상

| VideoCode | titles 인덱스 | 대표 제목 |
|-----------|---------------|-----------|
| 101 | 8 | 받은 선물이 마음에 안 들어요 |
| 102 | 18 | 엄마가 청소하래요 |
| 103 | 20 | 아쉽지만 그만 놀아야 해요 |
| 104 | 31 | 누리 어머니를 처음으로 만났어요 |

에디터 검증: 메뉴 **MSocial → Validate Video Index**

---

## 9. 소스 수정 내역 (전체)

### 9.1 API·로그인

| 파일 | 변경 |
|------|------|
| `MSocialApiClient.cs` | HTTP 클라이언트, sign-in, 세션 |
| `MSocialLoginAlert.cs` | Toast·오류 UI |
| `Login.cs` | API 클라이언트, `loginBusy`, CSV 가드 |
| `FlowManager.cs` | SceneRouter, HomePage, bootstrap Show |

### 9.2 씬 분리

| 파일 | 변경 |
|------|------|
| `MSocialSceneRouter.cs` | Additive 로드, Reparent, PageMovie 바인딩 |
| `MSocialPageRegistry.cs` | PageId 레지스트리 |
| `MSocialAppServices.cs` | 서비스 로케이터 |
| `MSocialStageWireup.cs` | 크로스 씬 참조 |
| `MSocialSceneBootstrap.cs` | 부트 초기화 |
| `MSocial_Stage1.unity` / `MSocial_Stage2.unity` | 분리 씬 |
| `Editor/MSocialSceneMigrationEditor.cs` | 마이그레이션 |

### 9.3 영상 (v1.25–v1.27)

| 파일 | 변경 |
|------|------|
| `MSocialVideoCache.cs` | **신규** — Android file:// 캐시 |
| `MSocialVideoPaths.cs` | GetPlayUrls, UTF-8 세그먼트 인코딩 |
| `MSocialVideoPlayerHelper.cs` | PrepareUrl, PlayPrepared, 오디오 Direct |
| `ScenePlayer.cs` | 병렬 Prepare, SceneIndex 검증, 로그 |
| `Stage2ScenePlay.cs` | 동일 재생 패턴 |
| `MSocialPageMoviePlayer.cs` | Intro URL 재생 |
| `MSocialIntroMoviePlayer.cs` | IntroMovie URL 재생 |
| `MSocialVideoRtOptimizer.cs` | ScenePlayer VP RT 유지 |
| `MSocialVideoGuard.cs` | Bootstrap에서만 Disarm |
| `Stage1Count/Listen/Dance.cs` | Prepare 성공 후 Play |
| `Editor/MSocialVideoIndexValidator.cs` | **신규** — index·파일 검증 |

### 9.4 트레이닝 피드백 UI (v1.35)

| 파일 | 변경 |
|------|------|
| `EmotionSelect.cs` | `ClickNextButton` 시 `gameObject.SetActive(false)` — neg/pos 전환 시 감정 UI 숨김 |
| `Stage1Negative.cs` | `HideWatchScene()`, 트레이닝 전 WatchScene·EmotionSelect 비활성화 |
| `Stage1FeedbackManager.cs` | `startFeedback` 범위 검사 |
| `Stage1Count.cs` | `ac != null` 가드 (Breath·PlayInstrument) |

**FeedbackPage 배열 순서 (`MSocial_Stage1`):**  
0=CountNumber, 1=Imagine, 2=Dance, 3=PlayInstrument, 4=Sing, 5=ListenMusic, 6=Think, 7=**Breath**

**Breath 영상 (StreamingAssets, v1.38):**
- `fb_014.mp4` (호흡 트레이닝_균일)
- `fb_015.mp4` (호흡 트레이닝_박스)

### 9.6 v1.36–v1.38 추가 수정

| 파일 | 변경 |
|------|------|
| `MSocialStageWireup.cs` | `Stage1Negative.userdata` 런타임 복구 |
| `Stage1Negative.cs` | `HideWatchScene()`, null-safe `logUpload`, fbm 활성 순서 |
| `Stage1FeedbackManager.cs` | `startFeedback` 범위·로그 |
| `MSocialStreamingAssetPaths.cs` | ASCII `GetCacheFileName`, Android raw path 우선 |
| `MSocialVideoPlayerHelper.cs` | `PrepareFeedbackClip`, 피드백 12s·알림 제어 |
| `Login.cs` | `TestAutoLogin` 제거, ID/PW 필수 검증 |
| `AutomatedAndroidBuild.cs` | 1.38 / code 26 |
| `scripts/rename_feedback_mp4.py` | 피드백 rename + 씬 치환 + `feedback_names.json` |
| `scripts/Validate-*.ps1` | PreDevice, Stage1Flow, FeedbackMp4 검증 추가 |

### 9.5 저사양·Android

| 파일 | 변경 |
|------|------|
| `MSocialDeviceProfile.cs` | RAM&lt;3.5GB, 960×540/1280×720 RT, Canvas 2800×1752 |
| `MSocialStartupLog.cs` | `persistentDataPath/startup.log` |
| `AutomatedAndroidBuild.cs` | ARMv7, GLES3, targetSdk 33 |
| `Plugins/Android/AndroidManifest.xml` | largeHeap, hardwareAccelerated |

---

## 10. MP4·StreamingAssets

### 백업

| 경로 | 내용 |
|------|------|
| `msocial_mp4/` | 원본 백업 (~166 MP4) |
| `Assets/StreamingAssets/` | 빌드용 MP4 **84개** + `movie_index.json` + **`feedback_names.json`** + **`fb_001`~`fb_015`** |

### 재인코딩 (2026-06-14)

H.264 1280×720, CRF 23–26, APK ~270 MB 수준.

```powershell
powershell -File "F:\PROJECT\Unity_Projects\M-Social_Clean\scripts\Reencode-MsocialMp4.ps1"
```

---

## 11. Galaxy Tab A8 대응

### 기기 프로필 (`MSocialDeviceProfile`)

```text
RAM < 3500 MB  → IsLowMemoryDevice = true
RAM < 2800 MB  → Video RT 960×540
그 외 저사양    → Video RT 1280×720
CanvasScaler   → 2800×1752, matchWidthOrHeight = 0
masterTextureLimit = 0  (UI 스프라이트 잘림 방지)
```

### Android 11

`targetSdkVersion 33`, `armeabi-v7a` 단일 ABI APK.

---

## 12. 검증 스크립트

| 스크립트 | 용도 |
|----------|------|
| `scripts/Validate-Stage1Flow.ps1` | Stage1Negative→피드백→ProgressDone 호출·씬 배선 |
| `scripts/Validate-PreDevice.ps1` | fb MP4·Prepare 경로·사후 씬 흐름 |
| `scripts/Validate-FeedbackMp4.ps1` | vcs[] ↔ StreamingAssets 15개 |
| `scripts/rename_feedback_mp4.py` | 피드백 MP4 rename + 씬 + JSON (dry-run: 인자 `1`) |
| `scripts/Rename-FeedbackMp4.ps1` | 위 Python 래퍼 |
| `scripts/Test-MSocialPreBuild.ps1` | 빌드 전 검증 (API, 씬, v1.27 캐시) |
| `scripts/Test-MSocialFullVerification.ps1` | 전체 검증 |
| `scripts/Test-MSocialLoginApi.ps1` | 로그인 API |
| `scripts/Capture-MSocialLogcat.ps1` | logcat + startup.log |
| `scripts/Reencode-MsocialMp4.ps1` | MP4 재인코딩 |

**2026-06-18 정적 검증:** Validate-Stage1Flow / PreDevice / FeedbackMp4 — **FAIL=0**

---

## 13. 실기기 테스트·로그

### 설치 (v1.38)

1. 기존 앱 **데이터 삭제** 또는 삭제 후 **v1.38** APK 설치
2. 로그인: **ID/PW 수동 입력** (`hudit` / `hudit` 등 — 자동 입력 없음, v1.39 빌드 전 APK는 구버전일 수 있음)
3. 지도 **집** → **엄마가 청소하래요** → 영상 재생 확인
4. **슬퍼요** → 「슬픈 마음을 잘 다독일 수 있을까요?」
5. **호흡 트레이닝에서 배운 심호흡 해보기** → **확인** → Breath 안내·`fb_014`/`fb_015` 재생
6. `Prepare failed:` **없음** 확인 (우측 상단 디버그 알림)
7. 영상 종료 → **확인** → **Stage1 카드 선택 화면** 복귀
8. (선택) 같은 트레이닝 2회째 — `VideoCache hit`로 첫 설치보다 빠른지 확인

### 진단 로그

기기 경로: `Android/data/com.Spike.MSocial/files/startup.log`

| 로그 | 의미 |
|------|------|
| `ScenePlayer code=... path=...` | 시나리오·경로 정상 |
| `VideoCache saved ...` | APK→캐시 복사 성공 |
| `Video prepared: file://...` | Prepare 성공 |
| `Video playing url=...` | 재생 시작 |
| `VideoCache hit ...` | 2회째 이후 캐시 재사용 (빠름) |
| `VideoCache failed` / `Prepare ALL failed` | 실패 — APK 내 `fb_###.mp4`·경로 확인 |

```powershell
powershell -File "F:\PROJECT\Unity_Projects\M-Social_Clean\scripts\Capture-MSocialLogcat.ps1"
```

---

## 14. 다음 단계

| # | 작업 | 상태 |
|---|------|------|
| 1 | Tab A8 **v1.38** 심호흡 E2E (Prepare·재생·ProgressDone) | **대기** |
| 2 | **v1.39** APK — `TestAutoLogin` 제거 반영 빌드 | **대기** |
| 3 | 피드백 `fb_002~005` 2차 압축 또는 음성 중 프리로드 (선택) | 선택 |
| 4 | Intro 한글 MP4 (`movie_index` intro 3개) 영문화 | 선택 |
| 5 | 관리자 로그인 실권한 분기 (`isAdmin` 또는 서버 level) | 선택 |
| 6 | 미사용 PNG/MP3 정리 | 선택 |

---

## 15. 참고 문서

- [PROJECT_ANALYSIS.md](./PROJECT_ANALYSIS.md) — 프로젝트 구조·스크립트
- [VIDEO_PLAYBACK.md](./VIDEO_PLAYBACK.md) — 영상 재생·캐시·진단 로그
- [MSOCIAL_LOCAL_API_VERIFICATION_LATEST.md](./MSOCIAL_LOCAL_API_VERIFICATION_LATEST.md) — API 검증
- [UNUSED_RESOURCES_REPORT.md](./UNUSED_RESOURCES_REPORT.md) — 미사용 자원
- [../msocial_mp4/README.md](../msocial_mp4/README.md) — MP4 백업
