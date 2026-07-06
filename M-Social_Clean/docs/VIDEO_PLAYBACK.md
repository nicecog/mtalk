# M-Social 영상 재생 (Stage1/2 + Intro + 피드백 트레이닝)

## 핵심 변경 사항 (v1.38 기준)

Android에서 `VideoPlayer`가 **한글 파일명 + jar 경로** 조합에서 실패할 수 있어,
시나리오 MP4와 **Stage1 피드백 트레이닝 MP4**를 영문+숫자 파일명으로 정리했습니다.

### 시나리오 영상 (Stage1/Stage2)

- `Assets/StreamingAssets/Movie/movie_index.json`
  - `stage1[]`, `stage2[]` 경로를 **영문+숫자 파일명**으로 교체
  - 실제 포함 경로에 맞게 `Movie\\Movie\\Stage1`, `Movie\\Movie\\Stage2` 접두를 반영
- `Assets/StreamingAssets/Movie/movie_names.json`
  - `legacyFileName`(원래 한글 파일명) ↔ `id`(예: `s1_019`, `s2_012`) 매핑 제공
- 파일 위치
  - Stage1: `Assets/StreamingAssets/Movie/Movie/Stage1/.../s1_###.mp4`
  - Stage2: `Assets/StreamingAssets/Movie/Movie/Stage2/.../s2_###.mp4`

### 피드백 트레이닝 영상 (StreamingAssets 루트)

- `Assets/StreamingAssets/feedback_names.json`
  - 한글 `legacyFileName` ↔ `fb_001`~`fb_015` 매핑 (asis/tobe 보관)
- 실제 파일: `Assets/StreamingAssets/fb_###.mp4`
- 씬 `vcs[]`: `MSocial_Stage1.unity`, `MainScene.unity` 모두 `fb_###.mp4`로 갱신
- 일괄 변환: `python scripts/rename_feedback_mp4.py <ProjectRoot> 0` (dry-run은 마지막 인자 `1`)

## 재생 데이터 플로우

1. `MSocialMovieLibrary.cs`가 `StreamingAssets/Movie/movie_index.json`을 로드
2. `ScenePlayer.cs` / `Stage2ScenePlay.cs`가 `movie_index`의 stage1/2 경로를 가져옴
3. `MSocialVideoCache.cs` (Android)는 APK 내부 파일을 `persistentDataPath`로 캐시 후 `file://`로 재생

## 2026-06-18 Prepare 안정화 (v1.37) + 피드백 영문화 (v1.38)

### Prepare failed 원인·대응

| 원인 | v1.38 대응 |
|------|------------|
| 한글 파일명 → 긴 `Uri` 타임아웃 | `fb_###.mp4` ASCII 파일명 |
| 비-ASCII 캐시명 | ASCII면 `fb_014.mp4` 그대로, 한글만 `cache_<md5>.mp4` |
| Android `Uri` 우선 시도 실패 | **raw path** → `file://` → `Uri` 순 |
| 피드백 중간 URL 실패 토스트 | `PrepareFeedbackClip`: `alertOnEachFailure=false` |
| 최종 실패 | `Prepare ALL failed: {path}` 만 표시 |

### 피드백 재생 타이밍 (`Stage1Count` / Breath)

```
OnEnable → 안내 음성(asc) 재생·대기
    → MSocialVideoCache (APK→persistentDataPath, 최대 180s)
    → PrepareFeedbackClip (12s)
    → vp.Play() → nextButton 표시
```

- **첫 설치:** `fb_002~005`(8~9MB)는 음성 **이후** 복사 대기가 길 수 있음
- **2회째:** `VideoCache hit` → Prepare 빠름
- Prepare 실패 시에도 **nextButton 표시** → `ProgressDone`으로 진행 가능

### 검증 스크립트

```powershell
powershell -File "F:\PROJECT\Unity_Projects\M-Social_Clean\scripts\Validate-Stage1Flow.ps1"
powershell -File "F:\PROJECT\Unity_Projects\M-Social_Clean\scripts\Validate-PreDevice.ps1"
powershell -File "F:\PROJECT\Unity_Projects\M-Social_Clean\scripts\Validate-FeedbackMp4.ps1"
```

---

## 2026-06-18 피드백 MP4 영문화 (v1.38)

- **증상:** 심호흡 등 피드백 영상에서 `Prepare failed:`(긴 한글 URL) 후 재생 — 한글 캐시·경로 문제.
- **조치:** StreamingAssets 루트 피드백 MP4 15개 → `fb_001.mp4`~`fb_015.mp4`로 rename.
- **매핑:** `Assets/StreamingAssets/feedback_names.json` (`legacyFileName` / `newFileName` / `feedbackPage`).
- **씬:** `MSocial_Stage1.unity`, `MainScene.unity`의 `vcs[]` 전부 `fb_###.mp4`.
- **검증:** `scripts/Validate-FeedbackMp4.ps1` — 15개 OK.

## 2026-06-18 트레이닝 확인 버튼 (v1.36)

- `Stage1Negative.userdata` null → `ClickNextButton`에서 예외 발생, 피드백 페이지 미진입.
- 피드백 MP4 15개는 StreamingAssets에 존재, BOM 없음. `Sing`/`ListenMusic`은 원본부터 `vcs: []`(오디오 UI).
- 검증: `scripts/Validate-FeedbackMp4.ps1`

## 2026-06-17 트레이닝 피드백 (v1.35)

Stage1 부정 감정 대처(노래·상상·**심호흡** 등)는 **별도 씬이 아니라** `MSocial_Stage1`의 `9-3. Stage1Feedback` 하위 페이지입니다.

| 페이지 | 스크립트 | 영상 소스 |
|--------|----------|-----------|
| Imagine | `Stage1Imagine` | 오디오만 |
| Sing | `Stage1Sing` | StreamingAssets MP4 |
| **Breath** | `Stage1Count` | `fb_014.mp4`, `fb_015.mp4` |
| CountNumber | `Stage1Count` | `fb_001.mp4` |
| Dance / PlayInstrument / ListenMusic | 각 전용 스크립트 | `fb_010`~`fb_013` / `fb_002`~`fb_009` / 오디오 UI |

재생 경로는 시나리오 MP4와 동일: Android에서 `MSocialVideoCache` → `persistentDataPath/Movies/` → `file://`.

**슬퍼요 → 심호흡 후 씬 흐름:**

```
ScenePlayer → EmotionSelect → Stage1Negative → Breath (fb_014|015)
  → 확인 → Stage1Manager.ProgressDone() → Stage1 카드 허브
```

**v1.35 UI 수정:** `Stage1Negative`에서 트레이닝으로 넘어갈 때 `Stage1WatchScene`·`EmotionSelect`를 숨김.

### 용량·캐시 성능 (참고)

| 파일 | 크기 | 첫 캐시 체감 |
|------|------|-------------|
| fb_014/015 (Breath) | ~2.5MB | 짧음 |
| fb_006~009 | ~2.5MB | 짧음 |
| fb_002~005 (바디 연주) | ~8.5MB | 길 수 있음 |
| fb_010~013 (춤) | ~3–4MB | 중간 |

추가 개선(선택): 960×540 2차 인코딩, 안내 음성 중 프리로드.

## 2026-06-17 안정화 내용 (v1.34)

- `movie_index.json` UTF-8 BOM 문제를 제거하고, `MSocialMovieLibrary`에서 BOM strip + timeout/fallback 보호 로직 적용.
- 실험용 Camera Far Plane 렌더링 경로 제거, 기본 `RenderTexture + RawImage` 재생 경로로 복귀.
- 상시 디버그 오버레이/상태 텍스트 제거(정상 사용자 플로우에서 화면 로그 미노출).
- Stage1 훈련 선택 후 분기 버튼 이벤트를 전수 점검해 누락 없이 정리.
  - `Stage1Count.set_buttonPressed(bool)` 명시 메서드 보강
  - `Stage1Listen.set_pressbutton(bool)` 명시 메서드 보강

## 진단 (필수)

기기에서 `persistentDataPath/startup.log` 확인:

- `VideoCache loading ...` / `VideoCache hit ...`
- `VideoCache saved ...`
- `Video prepared: ...`
- `Video playing url= ...`
- `Video prepare all urls failed` / `Prepare ALL failed`

**실기기 테스트 전:** 앱 데이터 삭제 권장(구 한글명 캐시 제거).

`startup.log`에 위 줄이 없으면, `feedback_names.json`·`fb_###.mp4`·씬 `vcs[]` 일치 여부를 `Validate-FeedbackMp4.ps1`로 확인하세요.

## 예시: “엄마가 청소하래요”

표시 텍스트(EmotionSelect)는 `ScenePlayer.titles`에 의해 결정되고,
실제 MP4는 Stage1 `movie_index.stage1[SceneIndex]`에서 결정됩니다.

`movie_names.json` 기준:
- legacyFileName: `집_청소기_무표정.mp4`
- id: `s1_019`
- newRel: `Movie\\Movie\\Stage1\\17-32\\s1_019.mp4`

