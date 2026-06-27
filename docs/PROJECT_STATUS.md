# MBody 프로젝트 진행·분석 문서 (Clean)

> 이 프로젝트(`MBody_Revised_Clean`)는 미사용 자원 정리 + 성능 최적화가 적용된 **활성 작업본**입니다.

**통합 문서 (전체 분석·이력):**  
[`../../MBody_Revised/docs/PROJECT_STATUS.md`](../../MBody_Revised/docs/PROJECT_STATUS.md)

**이 프로젝트 전용 보고서:**

| 문서 | 내용 |
|------|------|
| [`CLEANUP_REPORT.md`](./CLEANUP_REPORT.md) | 미사용 자원 삭제·빌드 |
| [`PERFORMANCE_OPTIMIZATION.md`](./PERFORMANCE_OPTIMIZATION.md) | 성능 최적화 상세 |
| [`ANDROID_DEVICE_PROFILING.md`](./ANDROID_DEVICE_PROFILING.md) | Android 설치·logcat·Profiler |
| [`cleanup-deleted-files.txt`](./cleanup-deleted-files.txt) | 삭제 파일 전체 목록 |

---

## 빠른 참조

| 항목 | 값 |
|------|-----|
| 원본 | `MBody_Revised` (보존, 미변경) |
| 삭제 파일 | 139개 (+ .meta) |
| Windows64 빌드 | **PASS** — `Build/Windows/MBody.exe` |
| Android release | **PASS** — `Build/Android/MBody-latest-stable.apk` + `.main.obb` |
| Android profiling | **PASS** — `Build/Android/MBody-profiling-dev.apk` |
| 컴파일 | `Assembly-CSharp.dll`, CS 오류 없음 |
| 로컬 API 통신 | **14/14 PASS** (성능 패치 후 회귀 포함) |
| 성능 시스템 | `PerformanceManager` — 시작 벤치 + 런타임 stress |

### 성능 티어 (자동)

| Tier | 카메라 | Pose | 녹화 |
|------|--------|------|------|
| Low | 720p | 2프레임마다 | 720p @ 20fps |
| Medium | 720p | 매 프레임 | 720p @ 25fps |
| High | 1080p | 매 프레임 | 1080p @ 25fps |

### 로컬 서버 연동 (JsonRequest)

```csharp
apiTarget = ApiTarget.Local;
localDesktopUrl = "http://127.0.0.1:21250";
```

서버 실행: `MBody_Revised_server` → `docker compose up -d` + `mvn spring-boot:run`

### 빌드 스크립트

```powershell
.\scripts\build-android.ps1 -Variant release
.\scripts\build-android.ps1 -Variant profiling
.\scripts\android-perf-logcat.ps1 -Install -DurationSec 120
```

---

## 문서 섹션 대응 (통합 MD)

| 통합 문서 섹션 | 내용 |
|----------------|------|
| §13 | Clean 정리 작업 |
| §14 | Unity↔Server API 검증 |
| §15 | 성능 최적화 |
| §16 | Android 빌드·프로파일링 |
