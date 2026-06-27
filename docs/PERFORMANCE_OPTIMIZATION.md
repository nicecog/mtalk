# MBody_Revised_Clean 성능 최적화 보고서

> 작성·갱신: 2026-06-09  
> 대상: `MBody_Revised_Clean` (Unity 6000.3.13f1)  
> 통합 문서: [`MBody_Revised/docs/PROJECT_STATUS.md` §15](../../MBody_Revised/docs/PROJECT_STATUS.md)

---

## 1. 작업 배경

### 1.1 요구사항

- 저가형 **Android**에서도 손 추적·녹화·게임 기능 유지
- **iOS 고가형**에서도 동일 앱 동작
- 앱 시작 시 기기 성능에 따라 **720p/1080p 자동 선택**
- 실행 중 부하 증가 시 **자동 품질 조정**
- `GetData()` 동기 GPU→CPU 병목 제거

### 1.2 네이티브(C/C++) 소스 조사 결과

| 계층 | 수정 가능 | 비고 |
|------|-----------|------|
| NatCorder `.dll`/`.aar` | ❌ | P/Invoke만 조정 |
| BlazePose Compute Shader | △ | 패키지 수준 |
| 사용자 작성 `.c`/`.cpp` | 없음 | — |

실질 개선은 **C# 파이프라인 + GPU readback 방식** 변경으로 달성.

---

## 2. 병목 분석 (최적화 전)

### 2.1 `PoseVisuallizer.LateUpdate`

```csharp
// 변경 전 (문제)
float[] t = new float[34*4];           // 매 프레임 GC
detecter.outputBuffer.GetData(t);      // GPU 전체 동기화
// 손목 index 19, 20 + confidence 33 만 사용
```

- `GetData()`: BlazePose GPU 작업 완료까지 CPU 대기
- 34개 랜드마크 전체 복사, 실사용 5 float

### 2.2 `VideoCaptureCam`

```csharp
// 변경 전
wc.GetPixels32(pixelBuffer);           // 1080p CPU readback ~8MB/프레임
recorder.CommitFrame(pixelBuffer, ...);
```

- BlazePose와 **동시** 카메라 버퍼 이중 readback
- raw `WebCamTexture` 사용 → 종횡비 보정 누락

### 2.3 `WebCamInput`

- 1920×1080 고정 (BlazePose는 128/256으로 다운샘플 → 과도한 입력)

---

## 3. 적용 아키텍처

### 3.1 이중 적응 (1번 + 2번)

```
[앱 시작]
  PerformanceManager.InitializeAsync()
    ├─ PlayerPrefs 캐시 있음 → 티어 즉시 적용
    └─ 없음 → GPU Blit + AsyncGPUReadback 벤치 (2~3초)
         → Low / Medium / High 결정 → PlayerPrefs 저장

[실행 중] (매 2초)
  평균 프레임 시간 > 38ms → stress++ (최대 2)
  평균 프레임 시간 < 30ms → stress--
  녹화 중 → pose interval +1

[EffectivePoseProcessInterval]
  = BasePoseProcessInterval + RuntimeStressLevel + (recording ? 1 : 0)
```

### 3.2 데이터 흐름 (최적화 후)

```
WebCamTexture → inputRT (프로필 해상도)
  → BlazePose ProcessImage (interval 적용 시 스킵)
  → AsyncGPUReadback → 손목 좌표
  → Lerp → FlowManager.UpdateAI (매 프레임)

녹화: inputRT → AsyncTextureInput → NatCorder.dll
```

---

## 4. `PerformanceManager` 상세

### 4.1 파일

`Assets/Script/PerformanceManager.cs`

### 4.2 티어별 설정

| Tier | CameraResolution | TargetFrameRate | BasePoseInterval | Record |
|------|------------------|-----------------|------------------|--------|
| Low | 1280×720 | 30 | 2 | 1280×720 @ 20fps |
| Medium | 1280×720 | 30 | 1 | 1280×720 @ 25fps |
| High | 1920×1080 | 30 | 1 | 1920×1080 @ 25fps |

### 4.3 벤치마크 분류 (`ClassifyTier`)

| 조건 | Tier |
|------|------|
| `!supportsAsyncGPUReadback` 또는 `!supportsComputeShaders` 또는 RAM &lt; 3000MB | Low |
| Blit &gt; 2.8ms 또는 Readback &gt; 9ms 또는 RAM &lt; 4500MB | Low |
| Blit &lt; 1.2ms AND Readback &lt; 4.5ms AND RAM ≥ 5000MB | High |
| 그 외 | Medium |

### 4.4 PlayerPrefs

| 키 | 설명 |
|----|------|
| `MBody_PerfTier` | 0=Low, 1=Medium, 2=High |
| `MBody_PerfVersion` | `1` — 앱 업데이트 시 재벤치하려면 증가 |

### 4.5 로그 태그 (기기 프로파일링)

| 태그 | 시점 |
|------|------|
| `[PerformanceManager] Benchmark complete` | 최초 벤치 완료 |
| `[PerformanceManager] Loaded cached tier` | 캐시 티어 로드 |
| `[PerformanceManager] Runtime downgrade/upgrade` | stress 변경 |
| `[PerfStats]` | 5초마다 tier·stress·avgFrameMs 요약 |

---

## 5. 파일별 변경 내역

### 5.1 신규

| 파일 | 역할 |
|------|------|
| `Assets/Script/PerformanceManager.cs` | 벤치·티어·런타임 적응 |
| `Assets/Editor/AutomatedWindowsBuild.cs` | Windows64 batch 빌드 |
| `scripts/build-android.ps1` | Android release/profiling 빌드 |
| `scripts/android-perf-logcat.ps1` | logcat 성능 캡처 |

### 5.2 수정

| 파일 | 변경 |
|------|------|
| `AndroidBootstrapLoader.cs` | `yield return performance.InitializeAsync()` 후 MBody 로드 |
| `WebCamInput.cs` | `Start` 코루틴 — 프로필 준비 후 카메라·inputRT 생성 |
| `PoseVisuallizer.cs` | AsyncGPUReadback, scratch 재사용, Lerp, OnRenderObject·shader 제거 |
| `VideoCaptureCam.cs` | AsyncTextureInput, inputRT 소스, SetRecordingActive |
| `BodyGameScene.cs` | Update 내 debug string foreach 제거 |
| `AutomatedAndroidBuild.cs` | `BuildProfilingAndroidApk()` 추가 |

---

## 6. `PoseVisuallizer` 구현 요약

- `landmarkScratch = new float[136]` — 한 번만 할당
- `poseFrameCounter % EffectivePoseProcessInterval` — 추론 스킵
- `AsyncGPUReadback.Request(detecter.outputBuffer, OnLandmarkReadback)`
- `!supportsAsyncGPUReadback` 시에만 `GetData` 폴백
- 매 `LateUpdate`: `Vector2.Lerp(display, target, 0.35f)` → `UpdateAI`
- 손목: index **19**(왼쪽), **20**(오른쪽), confidence: index **33** `.x`

---

## 7. `VideoCaptureCam` 구현 요약

- `textureInput = supportsAsyncGPUReadback ? new AsyncTextureInput(recorder) : new TextureInput(recorder)`
- `textureInput.CommitFrame(wci.inputImageTexture, clock.timestamp)`
- `startRecord` / `stopRecording` 에서 `PerformanceManager.SetRecordingActive`
- 녹화 해상도·FPS: `RecordWidth`, `RecordHeight`, `RecordFps`

---

## 8. 빌드·테스트 검증

| 일시 | 항목 | 결과 |
|------|------|------|
| 2026-06-09 | C# 컴파일 | PASS (warning만) |
| 2026-06-09 | Windows64 `MBody.exe` | PASS |
| 2026-06-09 | Android `MBody-latest-stable.apk` + OBB | PASS |
| 2026-06-09 | Android `MBody-profiling-dev.apk` | PASS |
| 2026-06-09 | 서버 API `unity-api-full-verification.ps1` | **14/14 PASS** |

로그: `Build/Android/unity-android-build.log`, `Build/unity-compile.log`

---

## 9. 실기 테스트 체크리스트 (미완료)

USB Android 기기 미연결 상태 — 아래는 수동 확인 필요.

- [ ] 첫 실행: 벤치 로그 + 티어 할당
- [ ] 재실행: 캐시 티어, 벤치 생략
- [ ] Body 게임: 손 아이콘 추적
- [ ] 녹화 중: `[PerfStats]` stress·interval 증가
- [ ] 녹화 영상 재생·서버 업로드
- [ ] iOS High 티어 1080p

가이드: [`ANDROID_DEVICE_PROFILING.md`](./ANDROID_DEVICE_PROFILING.md)

---

## 10. 선택적 후속 작업

| 항목 | 효과 |
|------|------|
| BlazePose `trimOutputs` — segmentation/world landmark 제거 | GPU 시간 절감 |
| Pose detection 간헐 실행 (ROI 안정 후) | 저가형 추가 여유 |
| `JsonRequest` dead code 정리 | 유지보수 |

---

## 11. 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-06-09 | 최초 작성 — PerformanceManager, GetData 제거, 녹화 경로, 빌드 검증 |
| 2026-06-09 | PerfStats 로깅, Android 2종 APK, 스크립트·기기 가이드 추가 |
