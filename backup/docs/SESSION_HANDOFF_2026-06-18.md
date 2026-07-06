# M-Body 저사양·용량·RAM 개선 정리 (세션 핸드오프)

> **작성일:** 2026-06-18  
> **프로젝트:** `F:\PROJECT\Unity_Projects\MBody_Revised_Clean`  
> **Unity:** 6000.3.13f1  
> **대상 기기:** Galaxy Tab A8 (2019, 2GB RAM, Android 11)  
> **서버:** `http://hudit.cafe24.com:8080/mbody` (`MBody_Revised_server` :21250)  
> **다음 세션:** 이 문서 → 실기 logcat 또는 우선순위 1 작업부터

---

## 0. 이번 세션 요약

M-Body가 저사양 태블릿에서 **용량·속도** 문제로 보인다는 이슈를 분석했다. 결론은 다음과 같다.

| 문제 | 실제 원인 |
|------|-----------|
| **용량 3GB+** | OBB ≈ **DanceVideo MP4 42개 (~3.0 GB)** |
| **느림 / 실행 불가** | BlazePose + 웹캠 + 녹화 + (재생 시) 고해상도 MP4 디코딩 |
| **시작 RAM 폭탄** | MP4 전량 디코딩이 **아님** → **단일 거대 씬 + 오디오 Decompress On Load + 씬 참조 에셋 일괄 로드** |

코드 측 `PerformanceManager`(2026-06) 최적화는 이미 적용됨. **실기 E2E·logcat 검증은 미완료.**

---

## 1. 현재 빌드·에셋 스냅샷

| 항목 | 값 |
|------|-----|
| APK | `Build/Android/MBody-latest-stable.apk` (~21 MB) |
| OBB | `Build/Android/MBody-latest-stable.main.obb` (~3.2 GB) |
| 설치 | **APK + OBB 필수** (`install-multiple`) |
| 아키텍처 | **ARM64 only** (`AutomatedAndroidBuild.cs`) |
| 메인 씬 | `Assets/Scenes/MBody.unity` (+ Android `AndroidBootstrap.unity`) |
| 패키지 | `com.CAU.MBody`, bundleVersion **1** |

### 에셋 용량 (Assets 기준)

| 종류 | 개수 | 디스크 |
|------|------|--------|
| `.mp4` (DanceVideo) | 42 | ~3.0 GB |
| `.mp4` (기타 Shader 등) | 3 | ~51 MB |
| `.png` | 212 | ~96 MB |
| `.mp3` | 70 | ~87 MB |
| `.wav` | 33 | ~23 MB |

개별 춤 MP4: **~100–120 MB / 90초** (1080p·고비트레이트 추정).

---

## 2. MP4 로딩 방식 (재인코딩 전 확인 사항)

### 결론: 시작 시 RAM에 3GB를 읽지 **않음**

- `Resources.Load` / `StreamingAssets` 일괄 로드 / 시작 시 `Prepare()` 루프 **없음**
- `VideoPlayer.clip`은 **사용자가 해당 화면에 들어가 재생할 때 1개씩** 할당

| 컴포넌트 | `clips[]` 수 | `vp.clip` 설정 시점 |
|----------|-------------|---------------------|
| `DancePractice` | 12 | 페이지 **OnEnable** (`ChangeMode`) |
| `DancePreviewIcon` | 6+6 | **미리보기 버튼** |
| `LV2Left` | 18 | **재생 버튼** (`PlaySource`) |

- 씬 YAML에 **VideoClip 참조 45개** (춤 42 + 기타 3) — 씬 로드 시 **메타데이터·의존성 등록**만, 전체 디코딩 아님
- 녹화·리뷰(`BodyReview`, `VideoReview`, `latestVideoPlay`)는 빌드 MP4가 아니라 **`vp.url`** (로컬/서버 경로)

### 재인코딩이 여전히 의미 있는 이유

| 효과 | 설명 |
|------|------|
| OBB 용량 | 3 GB → 0.5~1 GB (설치·배포) |
| 재생 시 CPU/GPU | 디코더 부담 감소 (Tab A8 체감) |
| **시작 RAM** | **거의 변화 없음** |

---

## 3. 앱 시작 시 RAM — 무엇이 올라가나

### 로그인 직후 vs Body/춤 진입 후

| 항목 | 로그인 직후 | Body/춤 진입 후 |
|------|-------------|-----------------|
| MP4 3GB 디코딩 | ❌ | 재생 시 1개만 |
| 단일 씬 `MBody.unity` 전체 | ✅ | 동일 |
| 씬 참조 **AudioClip** (`loadType: 0`) | ✅ PCM으로 풀림 | 동일 |
| 씬 참조 **Sprite/Texture** | ✅ | 동일 |
| **BlazePose 에셋** (씬 SerializeField) | ✅ 모델·셰이더 로드 | 추론 시작 |
| WebCam + RenderTexture | ❌ | ✅ |
| `PoseVisuallizer` | ❌ (`m_Enabled: 0`) | 활성화 시 |

### 웹캠은 로그인에서 바로 안 켜짐

- `WebCamInput`은 `InputImage` 자식
- `InputImage`는 씬에서 **`m_IsActive: 0`**
- `FlowManager.ResetPage()`에서 `WebCamObject.SetActive(false)` 유지
- Body 게임 `OnEnable` 등에서 `InputImage.SetActive(true)` 시 웹캠 `Start()` 실행

### 시작 RAM 폭탄의 실제 원인

1. **거대 단일 씬** — Body + Dance + 로그인 UI 전부 한 씬 (`MBody.unity` 6만+ 줄)
2. **오디오 Decompress On Load** — `.meta`에 `loadType: 0` 다수 → 씬 참조 시 **전부 RAM에 PCM** (디스크 ~110MB → RAM 수백 MB 가능)
3. **씬 직렬화 참조** — 비활성 UI 페이지의 AudioClip·Sprite·BlazePoseResource도 **씬 의존성으로 로드**
4. **BlazePose** — `PoseVisuallizer`가 비활성이어도 `blazePoseResource` 참조가 씬에 있으면 **NN 가중치 로드**
5. Unity IL2CPP 런타임 기본 footprint

**MP4는 시작 RAM 폭탄의 주원인이 아님.**

---

## 4. 개선안 전체 목록

### 4-A. 용량 (OBB / 디스크)

| 우선순위 | 항목 | 예상 효과 | 난이도 |
|----------|------|-----------|--------|
| **1** | Dance MP4 **720p H.264 재인코딩** (1.5–2 Mbps) | OBB **~70%↓** (~500–750 MB) | 낮음 |
| 2 | 540p 저사양 전용 빌드 변형 | OBB ~250–420 MB | 낮음 |
| 3 | **온디맨드 다운로드** (기본 OBB에 1단계만, 나머지 CDN) | 설치 ~200–400 MB | 중~높음 |
| 4 | 커리큘럼별 OBB 분할 (1단계 / 2단계) | 배포 유연성 | 중간 |
| 5 | WAV→MP3/AAC, PNG ASTC | 부가 (~수십 MB) | 낮음 |

**재인코딩 목표 (참고)**

| 프로필 | 곡당 | OBB 전체 |
|--------|------|----------|
| 현재 | ~70–120 MB | ~3.0 GB |
| 720p 1.5–2 Mbps | ~12–18 MB | ~500–750 MB |
| 540p 1 Mbps | ~6–10 MB | ~250–420 MB |

### 4-B. 시작 RAM

| 우선순위 | 항목 | 예상 효과 | 난이도 |
|----------|------|-----------|--------|
| **1** | **오디오 Import → Streaming** (`loadType: 2`) | 시작 RAM **수백 MB↓** | 낮음 |
| **2** | **씬 분리** — `Login.unity` + Body/Dance Additive Load | 로그인 후 **~150–250 MB** 목표 | 중간 |
| **3** | **BlazePose 지연 로드** — 씬 참조 제거, Body 진입 시 `Resources`/Addressables | ML 블록 제거 | 중간 |
| 4 | Addressables — BodyPack / DancePack | 모드별 로드 | 중~높음 |
| 5 | PNG ASTC + maxTextureSize 1024 | GPU RAM | 낮음 |
| 6 | Pose/웹캠 종료 시 `Dispose()` + `Stop()` + RT 해제 | Body 이후 RAM 회수 | 낮음 |

### 4-C. 런타임 속도 (이미 일부 적용 + 추가)

**이미 적용 (`PERFORMANCE_OPTIMIZATION.md`)**

- `PerformanceManager` — Low/Medium/High, RAM&lt;3GB → Low
- `PoseVisuallizer` — AsyncGPUReadback, interval 스킵
- `VideoCaptureCam` — AsyncTextureInput
- Tab A8: Low = 720p 카메라, pose interval 2, 녹화 720p@20fps

**추가 제안**

| 항목 | 내용 |
|------|------|
| **Ultra-Low 티어** | RAM&lt;2.5GB → 640×480, pose interval 3–4, 녹화 480p 또는 생략 |
| **모드별 Pose OFF** | `DancePractice` 등 영상만 보는 화면에서 BlazePose·웹캠 끄기 |
| **녹화 부하** | Low에서 녹화 중 interval +1 → 손 추적 체감 저하; 세션 끝 1회 녹화 검토 |
| BlazePose trim | segmentation/world landmark 제거 (문서 §10 후속) |
| MP4 재인코딩 | 재생 시 디코더 부하↓ |

---

## 5. 권장 로드맵

```
Phase 0  Tab A8 logcat (병목 분류: OOM vs 설치 vs 재생)
    │
    ├─ 설치/OBB 문제 → Phase 1 재인코딩 + 분할 OBB
    └─ RAM/OOM 문제  → 오디오 Streaming + 씬 분리 + Pose 지연

Phase 1  MP4 720p 일괄 재인코딩 (ffmpeg 스크립트)
Phase 2  오디오 Streaming + BlazePose 지연 로드 (코드 소규모)
Phase 3  Login 씬 분리 + Additive Load
Phase 4  온디맨드 Dance 다운로드 (M-Social VideoCache 패턴 참고)
```

### Tab A8 권장 즉시 조합

1. 오디오 Streaming 전환  
2. BlazePose / `PoseVisuallizer` 지연 로드  
3. Login 씬 분리  

---

## 6. 내일 시작 시 체크리스트

### A. 원인 분류 (가장 먼저)

증상을 사용자에게 확인하거나 logcat으로 판별:

- [ ] **설치 실패** — OBB 누락, 저장공간 부족
- [ ] **실행 직후 종료 / OOM** — 시작 RAM (이 문서 §3)
- [ ] **로그인은 되는데 Body만 느림** — BlazePose + 웹캠
- [ ] **춤 영상만 끊김** — MP4 디코딩 (재인코딩 대상)

```powershell
cd F:\PROJECT\Unity_Projects\MBody_Revised_Clean
.\scripts\android-perf-logcat.ps1 -Install -DurationSec 120
```

확인 태그: `[PerformanceManager]`, `[PerfStats]`, `lowmemorykiller`

### B. 구현 후보 (우선순위순)

- [ ] **오디오 `.meta` 일괄 Streaming** — `Assets/BodyMusics/**`, 씬 참조 클립
- [ ] **`PoseVisuallizer` 지연 초기화** — `Assets/Script/PoseVisuallizer.cs`
- [ ] **MP4 ffmpeg 재인코딩 스크립트** — `scripts/reencode-dance-mp4.ps1` (신규)
- [ ] **`PerformanceManager` Ultra-Low** — `Assets/Script/PerformanceManager.cs`
- [ ] **Login 씬 분리** — `Assets/Scenes/Login.unity` (신규) + Bootstrap 수정

### C. 미완료 (기존 문서)

- [ ] 실기 E2E (Body 게임, 녹화, 업로드, 춤 재생)
- [ ] `bundleVersion` / versionCode 정리
- [ ] CAFE24 `/mbody` 프로덕션 배포 검토

---

## 7. 핵심 파일 경로

```
MBody_Revised_Clean/
  Assets/Scenes/MBody.unity              # 단일 거대 씬 (VideoClip 45참조)
  Assets/Scenes/AndroidBootstrap.unity   # 벤치 → MBody 로드
  Assets/AndroidBootstrapLoader.cs
  Assets/FlowManager.cs                  # ResetPage, WebCamObject 비활성
  Assets/Script/WebCamInput.cs           # InputImage 활성 시 카메라 Start
  Assets/Script/PoseVisuallizer.cs       # BlazePose (씬에서 m_Enabled: 0)
  Assets/Script/PerformanceManager.cs
  Assets/DancePractice.cs                # clips[] OnEnable
  Assets/LV2Left.cs                      # clips[] PlaySource
  Assets/DancePreviewIcon.cs
  Assets/BodyManager.cs                  # bgms[], preSounds, AudioClip 다수
  Assets/DanceVideo/                     # MP4 ~3 GB
  Build/Android/MBody-latest-stable.apk + .main.obb
  docs/PERFORMANCE_OPTIMIZATION.md       # 2026-06 코드 최적화
  docs/ANDROID_DEVICE_PROFILING.md       # 설치·logcat 가이드
  docs/PROJECT_STATUS.md                 # → MBody_Revised/docs/PROJECT_STATUS.md
```

---

## 8. M-Social과 비교 (참고)

| | M-Social_Clean | M-Body |
|--|----------------|--------|
| APK | ~264 MB (영상 내장) | 21 MB + **3.2 GB OBB** |
| ML | 없음 | BlazePose 실시간 |
| 저사양 대응 | VideoCache, Prepare, fb 재인코딩 | PerformanceManager (실기 미검증) |
| 시작 RAM 이슈 | 상대적으로 작음 | **단일 씬 + 오디오 풀 로드** |

M-Social 쪽 미반영 작업: Login `TestAutoLogin` 제거 → **v1.39 APK** 필요 (`M-Social_Clean`).

---

## 9. 관련 문서

| 문서 | 내용 |
|------|------|
| [`PERFORMANCE_OPTIMIZATION.md`](./PERFORMANCE_OPTIMIZATION.md) | PerformanceManager, GetData 제거, 녹화 경로 |
| [`ANDROID_DEVICE_PROFILING.md`](./ANDROID_DEVICE_PROFILING.md) | APK+OBB 설치, logcat |
| [`PROJECT_STATUS.md`](./PROJECT_STATUS.md) | 통합 상태 (서버 API 14/14 등) |
| [`../../MBody_Revised/docs/PROJECT_STATUS.md`](../../MBody_Revised/docs/PROJECT_STATUS.md) | 전체 아키텍처 |

---

## 10. 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-06-18 | 저사양·용량·MP4 로딩·시작 RAM 분석 및 개선안 정리 (코드 변경 없음) |
| 2026-06-19 | ChangeCam→WebCamInput.deviceChange, Validate-SceneFlow.ps1, PlayMode FlowDiag |
