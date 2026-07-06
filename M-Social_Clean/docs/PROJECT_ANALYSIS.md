# M-Social 프로젝트 분석 (Clean 작업본)

> 작성: 2026-06-10  
> **갱신: 2026-06-15 (v1.27)** — 씬 분리, Tab A8 UI/영상 수정 반영  
> 원본: `F:\PROJECT\Unity_Projects\M-Social` (보존, 미변경)  
> 작업본: `F:\PROJECT\Unity_Projects\M-Social_Clean`  
> **작업 이력:** [SESSION_HANDOFF_2026-06-14.md](./SESSION_HANDOFF_2026-06-14.md)

---

## 1. 프로젝트 개요

| 항목 | 값 |
|------|-----|
| 앱명 | M-Social |
| Unity | **6000.3.13f1** |
| 패키지 ID | `com.Spike.MSocial` |
| 버전 | **1.27** (versionCode 15) |
| 장르 | 사회성 기술 훈련 (영상 시나리오 + 감정/행동 선택) |
| ML / 웹캠 | **없음** — 영상 재생 + UI 인터랙션 중심 |
| 최신 APK | `Build/Android/MSocial-latest-stable.apk` (~264 MB) |
| 서버 | `http://hudit.cafe24.com:8080/msocial` |
| 테스트 계정 | `hudit` / `hudit` (seq=23) |

### 앱 패밀리 비교

| | MBody | M-Face | M-Social |
|--|-------|--------|----------|
| Unity | 6000.3 | 6000.3 | **6000.3** |
| 핵심 | BlazePose 댄스 | 표정 훈련 + 서버 감정 | 사회성 시나리오 영상 |
| 웹캠 | ✅ | ✅ | ❌ |
| CSV | Players.csv | `_FaceData.csv` | `_SocialData.csv` |
| APK (2026-06-15) | ~21 MB | ~246 MB | **~264 MB** |

---

## 2. 디렉터리 구조

```
M-Social_Clean/
├── Assets/
│   ├── *.cs                    # 게임 + MSocial* 런타임
│   ├── Scenes/
│   │   ├── MSocial_Bootstrap.unity   # Build 0
│   │   ├── MSocial_Stage1.unity      # Build 1 (additive)
│   │   ├── MSocial_Stage2.unity      # Build 2 (additive)
│   │   ├── MainScene.unity           # 레거시 (빌드 제외)
│   │   └── Legacy/
│   ├── StreamingAssets/
│   │   └── Movie/
│   │       ├── movie_index.json      # stage1/2/intro 경로
│   │       └── **/*.mp4              # 84개
│   ├── Editor/
│   │   ├── AutomatedAndroidBuild.cs
│   │   ├── MSocialSceneMigrationEditor.cs
│   │   └── MSocialVideoIndexValidator.cs
│   └── Page/                   # UI 스프라이트
├── msocial_mp4/                # MP4 원본 백업
├── scripts/                    # 검증·빌드·logcat
├── Build/Android/
└── docs/
```

---

## 3. 씬 아키텍처 (3-Scene)

| Index | 씬 | 주요 Page |
|-------|-----|-----------|
| 0 | `MSocial_Bootstrap` | IntroLogo, Login, IntroMovie |
| 1 | `MSocial_Stage1` | Stage1Start, Stage1Intro, Stage1SelectMap, **Stage1WatchScene**, 피드백 |
| 2 | `MSocial_Stage2` | Stage2Intro, ActSelect, Stage2ScenePlay 등 |

런타임: `MSocialSceneRouter`, `MSocialPageRegistry`, `MSocialAppServices`, `MSocialStageWireup`

설계 상세: [SCENE_SPLIT_DESIGN.md](./SCENE_SPLIT_DESIGN.md)  
영상 재생: [VIDEO_PLAYBACK.md](./VIDEO_PLAYBACK.md)

---

## 4. 게임·런타임 스크립트

### 코어 / 흐름

| 파일 | 역할 |
|------|------|
| `FlowManager.cs` | 페이지 리셋, SceneRouter 연동 |
| `Login.cs` | 로그인, `_SocialData.csv` 동기화 |
| `MSocialApiClient.cs` | HTTP API |
| `MSocialAppServices.cs` | Login·Flow 싱글톤 (Login DDOL 없음) |
| `MSocialSceneRouter.cs` | Additive 씬 로드, Page flatten |
| `MSocialPageRegistry.cs` | PageId → GameObject |
| `MSocialStageWireup.cs` | 크로스 씬 참조 복구 |
| `MSocialDeviceProfile.cs` | Tab A8 저사양 프로필 |
| `MSocialStartupLog.cs` | `startup.log` 진단 |

### 영상 재생 (v1.27)

| 파일 | 역할 |
|------|------|
| `MSocialMovieLibrary.cs` | `movie_index.json` 지연 로드 |
| `MSocialVideoCache.cs` | Android APK→persistentDataPath 캐시 |
| `MSocialVideoPaths.cs` | StreamingAssets URL 생성 |
| `MSocialVideoPlayerHelper.cs` | PrepareUrl, PlayPrepared, RT |
| `MSocialVideoRtOptimizer.cs` | 저사양 씬 RT 제거 (ScenePlayer 제외) |
| `MSocialVideoGuard.cs` | Bootstrap VideoPlayer Disarm |
| `ScenePlayer.cs` | Stage1 시나리오 영상 |
| `Stage2ScenePlay.cs` | Stage2 시나리오 영상 |
| `MSocialPageMoviePlayer.cs` | Stage Intro 영상 |
| `MSocialIntroMoviePlayer.cs` | Bootstrap IntroMovie |

### Stage 1

| 파일 | 역할 |
|------|------|
| `Stage1Manager.cs` | 6카테고리 허브 |
| `SceneSelect.cs` | 시나리오 썸네일 선택 |
| `ScenePlayer.cs` | 영상 재생 + EmotionSelect |
| `EmotionSelect.cs` | 감정 3选 |
| `Stage1Count/Listen/Dance/...` | 피드백 모듈 |

### Stage 2

| 파일 | 역할 |
|------|------|
| `Stage2Manager.cs` | 2단계 순서 |
| `Stage2ScenePlay.cs` | 2단계 영상 |
| `ActSelect.cs` | 행동·대사 선택 |

---

## 5. 앱 플로우

```
IntroLogo → Login (+ SocialData.csv)
  → Stage1Start → Stage1SelectMap
  → 6카테고리 선택 → SceneSelect
  → Stage1WatchScene (ScenePlayer: 7초 안내 + 영상)
  → EmotionSelect → Positive/Negative 피드백
  → (5회 후) Stage2Intro → Stage2 역할극
  → End (logUpload)
```

### 시나리오 코드

```
code = sceneCategory * 100 + (sceneNumber + 2 * nowPage)
```

`ScenePlayer.VideoCodes[]`에서 매칭 → `movie_index.stage1[SceneIndex]`

---

## 6. 서버 API

**Base URL:** `http://hudit.cafe24.com:8080/msocial`

| API | 사용처 |
|-----|--------|
| `GET /api/health` | 연결 확인 |
| `POST /api/users/sign-in` | 로그인 |
| `GET/POST /api/mface-images` | `_SocialData.csv` 동기화 |

상세: [MSOCIAL_LOCAL_API_VERIFICATION_LATEST.md](./MSOCIAL_LOCAL_API_VERIFICATION_LATEST.md)

---

## 7. 빌드 설정 (현재)

| 설정 | 값 |
|------|-----|
| minSdk | 25 |
| targetSdk | 33 |
| Architecture | **ARMv7 only** (`armeabi-v7a`) |
| Graphics API | OpenGLES3 |
| Stripping | Minimal + link.xml |
| APK | ~264 MB |

---

## 8. 발견·해결된 이슈

### ✅ 해결

| # | 이슈 | 해결 버전 |
|---|------|-----------|
| 1 | 로그인 후 화면 꼬임 | v1.14 |
| 2 | Tab A ABI / SDK | v1.14–v1.16 |
| 3 | APK 1.37 GB | MP4 압축 |
| 4 | 스플래시 후 흰 화면 | v1.19 |
| 5 | 로그인 후 흰/검은 화면 | v1.20–v1.23 |
| 6 | 지도 라벨 잘림 | v1.24 |
| 7 | Stage1 영상 미재생 (jar URL) | v1.25–v1.27 |

### 🟡 실기기 재검증 대기

- Tab A8 v1.27 영상 재생 E2E
- `startup.log` VideoCache / Prepare 결과

---

## 9. 타임라인

| 일자 | 작업 |
|------|------|
| 2026-06-10 | Clean 복사, CSV 버그 수정 |
| 2026-06-14 | API, MP4 압축, Tab A 크래시 대응 |
| 2026-06-15 | 씬 분리 3-Scene, 로그인/UI 수정 v1.19–v1.24 |
| 2026-06-15 | 영상 재생 v1.25–v1.27 (VideoCache) |

상세: [SESSION_HANDOFF_2026-06-14.md](./SESSION_HANDOFF_2026-06-14.md)

---

## 10. 복사 정보

```
원본:  F:\PROJECT\Unity_Projects\M-Social
대상:  F:\PROJECT\Unity_Projects\M-Social_Clean
```

원본은 수정하지 않음.
