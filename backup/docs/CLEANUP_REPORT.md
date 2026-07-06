# MBody_Revised_Clean 정리 보고서

> 작성일: 2026-06-09  
> 원본: `MBody_Revised` → 복사본: `MBody_Revised_Clean`  
> 통합 문서: [`MBody_Revised/docs/PROJECT_STATUS.md` §13](../../MBody_Revised/docs/PROJECT_STATUS.md)

---

## 1. 작업 요약

| 단계 | 결과 |
|------|------|
| 프로젝트 복사 | `MBody_Revised` → `MBody_Revised_Clean` (Library/Temp 제외) |
| 미사용 자원 재검증 | GUID 교차 참조 + Record.unity 격리 확인 |
| 삭제 | **139개 파일** (+ .meta 동반 삭제) |
| AndroidManifest 정리 | screenrecorder 라이브러리 참조 제거 |
| Unity 컴파일 | `Assembly-CSharp.dll` 생성, **CS 오류 없음** |
| Windows64 빌드 | **Build Finished, Result: Success** |

빌드 산출물: `Build/Windows/MBody.exe` (667 KB) + `MBody_Data/`

---

## 2. 삭제된 항목 (재검증 후 확정)

### 2.1 완전 미참조 C# 스크립트 (4개)

| 파일 | 재검증 |
|------|--------|
| `ButtonInstance.cs` | 씬·코드·프리팹 guid 0, onClick 바인딩 0 |
| `DanceAudio.cs` | 빈 스텁, 참조 0 |
| `EnvMove.cs` | 씬 미부착, 참조 0 |
| `TimeCheck.cs` | 빈 스텁, 참조 0 |

**유지한 스크립트 (삭제 안 함)**

| 파일 | 이유 |
|------|------|
| `latestVideoPlay.cs` | `LV4End` > `Image` 자식에 부착, 페이지 활성화 시 `OnEnable` 실행 |
| `AutomatedAndroidBuild.cs` | Editor 빌드 파이프라인 |
| `CSVScroll.cs` | Review 페이지에 부착, `OnEnable`에서 CSV 표시 |

### 2.2 Record.unity 생태계 (빌드 미포함)

| 삭제 파일 |
|-----------|
| `Assets/Scene/Record.unity` |
| `Scripts/NativeScreenRecorderAndMuxer.cs` |
| `Scripts/NativePluginManager.cs` |
| `Scripts/UnityInternalAudioRecorder.cs` |
| `Plugins/Android/screenrecorderlib-release_1mbs_2.aar` |
| `Plugins/Android/aspectjrt-1.8.2.jar` |
| `StreamingAssets/isoparser-default.properties` |

`Assets/Scene/` 폴더는 비어 삭제됨.

### 2.3 미참조 미디어 자원

| 유형 | 삭제 수 |
|------|---------|
| 이미지 (png 등) | 100 |
| 오디오 (mp3 등) | 26 |
| 비디오 (mp4) | 2 |
| **합계** | **128** (+ 위 스크립트/씬 11 = 139) |

전체 삭제 목록: [`cleanup-deleted-files.txt`](./cleanup-deleted-files.txt)

---

## 3. 수정된 파일

### `Assets/Plugins/Android/AndroidManifest.xml`

- screenrecorder 패키지·`AndroidUtils` 액티비티 제거
- 표준 `UnityPlayerActivity` 런처만 유지

---

## 4. 빌드 검증

### 컴파일 (`unity-import-compile.log`)

```
Csc Library/Bee/.../Assembly-CSharp.dll
CopyFiles Library/ScriptAssemblies/Assembly-CSharp.dll
```

- `error CS*` 없음

### Windows64 Player (`unity-windows-build.log`)

```
building x86_64 windows player!
Build Finished, Result: Success.
```

출력 경로: `Build/Windows/MBody.exe`

---

## 5. 용량 변화 (Assets 기준)

| | 원본 | Clean |
|---|------|-------|
| Assets | ~3369 MB | ~3350 MB (미사용 자원 삭제 반영) |

대부분 용량은 DanceVideo·음원 등 **사용 중인** 미디어.

---

## 6. 후속 권장 (정리 단계 — 대부분 완료)

| # | 항목 | 상태 |
|---|------|------|
| 1 | Android APK 빌드 | ✅ `MBody-latest-stable.apk` + OBB (2026-06-09) |
| 2 | 실제 작업본 `MBody_Revised_Clean` | ✅ 진행 중 |
| 3 | 씬 Missing 참조 확인 | ⬜ Editor 수동 권장 |
| 4 | 성능 최적화 | ✅ §9 참조 |
| 5 | Android 실기 logcat | ⬜ USB 기기 필요 |

---

## 7. 서버 통신 검증 (요약)

| 항목 | 결과 |
|------|------|
| Unity API 6종 | 전체 PASS |
| 세부 검증 14건 | 14/14 PASS |
| 보고서 | [`MBody_Revised_server/docs/LOCAL_API_TEST_REPORT.md`](../../MBody_Revised_server/docs/LOCAL_API_TEST_REPORT.md) |

## 8. 변경 이력 (정리)

| 날짜 | 내용 |
|------|------|
| 2026-06-09 | 정리본 생성, 139개 파일 삭제, Windows64 빌드 성공 |
| 2026-06-09 | `MBody_Revised/docs/PROJECT_STATUS.md` §13에 통합 반영 |
| 2026-06-09 | Unity↔Server 통신 전체 세부 검증 14/14 PASS, MD 업데이트 |

---

## 9. 성능 최적화 작업 (2026-06-09)

> 상세: [`PERFORMANCE_OPTIMIZATION.md`](./PERFORMANCE_OPTIMIZATION.md) · 통합 §15

### 9.1 요약

| 항목 | 내용 |
|------|------|
| `PerformanceManager` | 시작 GPU 벤치 → Low/Medium/High, 런타임 stress 0~2 |
| `PoseVisuallizer` | `GetData` → `AsyncGPUReadback`, GC 제거, 손목 Lerp |
| `VideoCaptureCam` | `GetPixels32` → `AsyncTextureInput`, `inputRT` 소스 |
| `WebCamInput` | 프로필 기반 720p/1080p |
| `BodyGameScene` | 매 프레임 debug string 제거 |

### 9.2 신규·수정 파일

| 구분 | 파일 |
|------|------|
| 신규 | `PerformanceManager.cs`, `AutomatedWindowsBuild.cs`, `scripts/build-android.ps1`, `scripts/android-perf-logcat.ps1` |
| 수정 | `AndroidBootstrapLoader.cs`, `WebCamInput.cs`, `PoseVisuallizer.cs`, `VideoCaptureCam.cs`, `BodyGameScene.cs`, `AutomatedAndroidBuild.cs` |

---

## 10. Android 빌드 (2026-06-09)

| 산출물 | 크기 | 비고 |
|--------|------|------|
| `Build/Android/MBody-latest-stable.apk` | 21 MB | release, split binary |
| `Build/Android/MBody-latest-stable.main.obb` | ~3.2 GB | 필수 |
| `Build/Android/MBody-profiling-dev.apk` | 48 MB | Dev + Unity Profiler |

```powershell
.\scripts\build-android.ps1 -Variant release
.\scripts\build-android.ps1 -Variant profiling
```

가이드: [`ANDROID_DEVICE_PROFILING.md`](./ANDROID_DEVICE_PROFILING.md)

---

## 11. 검증 이력 (누적)

| 일시 | 검증 | 결과 |
|------|------|------|
| 2026-06-09 | Windows64 빌드 | PASS |
| 2026-06-09 | 서버 API 14건 | 14/14 PASS |
| 2026-06-09 | 성능 패치 후 컴파일 | PASS |
| 2026-06-09 | Android release APK | PASS |
| 2026-06-09 | Android profiling APK | PASS |
| 2026-06-09 | 성능 패치 후 API 회귀 | 14/14 PASS |
| — | Android 실기 logcat | ⬜ 기기 미연결 |

---

## 12. 변경 이력 (전체)

| 날짜 | 내용 |
|------|------|
| 2026-06-09 | 정리본 생성, 139개 파일 삭제, Windows64 빌드 성공 |
| 2026-06-09 | `MBody_Revised/docs/PROJECT_STATUS.md` §13에 통합 반영 |
| 2026-06-09 | Unity↔Server 통신 전체 세부 검증 14/14 PASS |
| 2026-06-09 | 성능 최적화·Android APK 2종·문서 §9~§12 — 통합 §15·§16 반영 |
