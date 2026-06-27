# Android 기기 프로파일링 가이드

> 작성·갱신: 2026-06-09  
> 프로젝트: `MBody_Revised_Clean`  
> 통합 문서: [`MBody_Revised/docs/PROJECT_STATUS.md` §16](../../MBody_Revised/docs/PROJECT_STATUS.md)

---

## 1. 빌드 산출물

| 파일 | 크기 (2026-06-09) | 용도 |
|------|-------------------|------|
| `Build/Android/MBody-latest-stable.apk` | 21 MB | 배포용 (split binary) |
| `Build/Android/MBody-latest-stable.main.obb` | ~3.2 GB | **필수** 확장 파일 |
| `Build/Android/MBody-profiling-dev.apk` | 48 MB | Development + Unity Profiler |

> split binary: APK만으로는 실행 불가. **APK + OBB** 반드시 함께 설치.

### 빌드 명령

```powershell
cd MBody_Revised_Clean
.\scripts\build-android.ps1 -Variant release      # 배포 APK
.\scripts\build-android.ps1 -Variant profiling    # Profiler dev APK
```

Editor: `AutomatedAndroidBuild.BuildLatestStableAndroidApk` / `BuildProfilingAndroidApk`

---

## 2. USB 설치

### 전제

- Android USB 디버깅 활성화
- `adb` 경로: `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`

### 설치

```powershell
cd MBody_Revised_Clean
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb devices
& $adb install-multiple -r Build\Android\MBody-latest-stable.apk Build\Android\MBody-latest-stable.main.obb
```

### profiling APK (동일 package `com.CAU.MBody`, 동일 OBB 사용 가능)

```powershell
& $adb install-multiple -r Build\Android\MBody-profiling-dev.apk Build\Android\MBody-latest-stable.main.obb
```

---

## 3. logcat 성능 캡처

앱 실행 후 Body 게임·녹화 등 **2분간** 사용하면서 로그 수집:

```powershell
.\scripts\android-perf-logcat.ps1 -Install -DurationSec 120
```

옵션:

| 옵션 | 설명 |
|------|------|
| `-Install` | APK+OBB 설치 후 캡처 |
| `-ProfilingApk` | profiling dev APK 사용 |
| `-DurationSec` | 캡처 시간(초) |

출력: `Build/Android/perf-logcat-YYYYMMDD-HHmmss.txt`

### 확인할 로그 태그

| 태그 | 의미 |
|------|------|
| `[PerformanceManager] Benchmark complete` | 최초 벤치 결과 (Tier, blit/readback ms) |
| `[PerformanceManager] Loaded cached tier` | 재실행 시 캐시 티어 |
| `[PerformanceManager] Runtime downgrade` | 부하 증가 → stress 상승 |
| `[PerformanceManager] Runtime upgrade` | 안정화 → stress 하강 |
| `[PerfStats]` | 5초마다 tier·stress·poseInterval·avgFrameMs |

### 예시 로그

```
[PerformanceManager] Benchmark complete. Tier=Low, blit=3.10ms, readback=7.20ms, ram=3072MB
[PerfStats] tier=Low, stress=0, poseInterval=2, camera=1280x720, record=1280x720@20fps, recording=False, avgFrameMs=31.2
[PerformanceManager] Runtime downgrade stress=1, avgFrame=42.1ms
[PerfStats] tier=Low, stress=1, poseInterval=3, recording=True, avgFrameMs=38.5
```

---

## 4. Unity Profiler (dev APK)

1. `.\scripts\build-android.ps1 -Variant profiling`
2. profiling APK + OBB 설치
3. 기기 USB 또는 동일 LAN
4. Unity Editor → **Window → Analysis → Profiler**
5. **Active Profiler** → 연결된 Android 기기 선택
6. CPU/GPU: `PoseVisuallizer.LateUpdate`, `BlazePoseDetecter.ProcessImage`, `VideoCaptureCam.Update` 확인

---

## 5. 성능 티어 초기화 (재벤치)

앱 데이터 삭제:

```powershell
& $adb shell pm clear com.CAU.MBody
```

또는 앱 설정 → 저장공간 → 데이터 삭제.

---

## 6. 실기 테스트 체크리스트

| # | 시나리오 | 확인 |
|---|----------|------|
| 1 | 첫 실행 벤치 | logcat에 `Benchmark complete`, Tier 할당 |
| 2 | 재실행 | `Loaded cached tier`, 벤치 지연 없음 |
| 3 | Body 게임 | 손 아이콘(19/20) 추적 자연스러움 |
| 4 | Dance + 녹화 | `[PerfStats] recording=True`, stress 증가 가능 |
| 5 | 녹화 파일 | 재생 가능, 해상도 프로필과 일치 |
| 6 | 서버 업로드 | 로그인 후 업로드 성공 (API 14/14는 PC에서 검증 완료) |
| 7 | 저가형 Android | avgFrameMs ~33ms(30fps) 근처 유지 |
| 8 | 고가형 (해당 시) | High 티어, 1080p 카메라 프로필 |

---

## 7. 현재 상태 (2026-06-09)

| 항목 | 상태 |
|------|------|
| APK/OBB 빌드 | ✅ 완료 |
| profiling APK 빌드 | ✅ 완료 |
| USB 기기 logcat | ⬜ 연결된 기기 없음 — 위 절차로 수동 실행 |
| Unity Profiler 실측 | ⬜ 기기 연결 필요 |

---

## 8. 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-06-09 | 최초 작성 — 빌드 산출물, install, logcat, Profiler |
| 2026-06-09 | PerfStats 로그 예시, 체크리스트, 빌드 크기·상태 갱신 |
