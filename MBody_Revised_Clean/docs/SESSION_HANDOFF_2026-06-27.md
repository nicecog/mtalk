# M-Body 세션 작업 기록 (2026-06-27)

> **프로젝트:** `F:\PROJECT\Unity_Projects\MBody_Revised_Clean`
> **Unity:** 6000.3.13f1
> **패키지:** `com.CAU.MBody`
> **대상 기기:** Galaxy Tab A8 SM-T290 (`R9WR611YAAJ`, armeabi-v7a, Android 11)
> **서버:** `http://hudit.cafe24.com:8080/mbody`
> **이전 세션:** [`SESSION_HANDOFF_2026-06-20.md`](./SESSION_HANDOFF_2026-06-20.md)

---

## 0. 오늘 작업 요약

| 구분 | 결과 |
|------|------|
| 모든 씬 "다음" 버튼 전수 점검 | ✅ 정적 49개 전이 + onClick 타깃 메서드 검증 도구 작성 |
| 오렌지/블루 악기 선택 오작동 (다른 악기가 올라감) | ✅ 클릭한 아이콘 직접 배치로 수정 |
| 2회차 진입 크래시 (윈드메이커→플라이 위드미, 악기 선택 후) | ✅ 웹캠 재초기화·코루틴 누수·null 가드 수정 |
| 2회차 크래시 — BlazePose **GPU 버퍼 경합** (실기 logcat 확보) | ✅ Pose 입력 스냅샷 분리 + AsyncGPUReadback + 진입 타이밍 보강 (4-A장) |
| 악기 선택 후 흰 배경 (잘했어요~연주 구간) | ✅ 웹캠 RawImage를 프레임 준비 후에만 표시, 비게임 페이지 숨김 |
| GitHub 업로드 | ✅ `nicecog/mtalk` `main` 브랜치에 최초 커밋 푸시 |

> ✅ 4-A장 수정분은 **APK 빌드·Tab A8 설치 완료**. 실기 2회차 E2E 최종 검증만 대기.

---

## 1. 모든 씬 "다음" 버튼 전체 점검

### 점검 방법 (스크립트 2종)

| 스크립트 | 역할 |
|----------|------|
| `scripts/validate-scene-flow.py` | `ButtonFlow`/`TimerFlow` 기반 페이지 전이 검증 |
| `scripts/validate-onclick-targets.py` | **신규** — 각 버튼 onClick이 가리키는 *실제 컴포넌트*에 호출 메서드가 존재하는지 검증 |
| `scripts/audit-onclick-methods.py` | **신규** — onClick 핸들러를 `Type.Method`로 그룹화해 호스트 버튼 목록 출력 |

### 결과

- **ButtonFlow 내비게이션: 49개 전이 전부 정상** (FAIL 0).
- 메서드 호출형 내비게이션 중 실제 막혔던 건 직전 세션에서 수정한 **EndMessage "다음으로"(`EndMainGame`)** 하나뿐.
- `selectMusic`, `setLevel`, `setSong`, `Stop`, `DanceManager.ClickNext`, `DanceStarManager.ClickNext` 등의 onClick 호출은 **현재 스크립트에 없는 레거시 잔재**이나, 실제 이동은 항상 별도의 `ButtonFlow.ClickNext`가 처리하므로 무해(no-op). 회귀 위험 때문에 그대로 둠.

### 바디 모드 전체 경로 (확인됨)

```
MusicSelect →(Play) SoundSelectYellow →(Next) SoundSelectBlue
→(Next) Alert →(Skip) SpeedSelect →(Skip) MainGameCali
→(Next) ReadyMessage →(Next) MainGame →(Next) EndMessage
→(EndMainGame) MusicSelect(1·2회차) / EndSelect(3회차) →(Result/Review)
```

---

## 2. 오렌지/블루 악기 선택 — 클릭과 다른 악기가 올라가던 문제

### 원인

1. **위치 셔플:** `SoundSelect.OnEnable()`이 악기 *위치만* 무작위로 섞어, 사용자가 본 위치와 실제 악기가 어긋남.
2. **`indexData`로 재조회:** 드롭 시 클릭한 아이콘이 아니라 `Icons[indexData-1]`을 다시 찾아 옮겨, 번호 불일치 시 다른 악기가 올라감.
3. **드래그 종료 레이캐스트:** 손을 뗄 때 *처음 맞은* 슬롯에 배치되어 옆 슬롯에 잘못 들어감.

### 조치 (코드)

| 파일 | 변경 |
|------|------|
| `Assets/DragIcon.cs` | 전면 재작성. 클릭/드래그한 **아이콘 자체**를 슬롯에 배치. 짧은 터치(<24px)는 탭으로 처리. 슬롯 레이캐스트를 손가락 위치 기준으로 가장 가까운 빈 슬롯 선택. `SyncIndexDataFromParent()`, `IsAssignedToSlot()` 추가 |
| `Assets/DropIcon.cs` | `AssignInstrument(DragIcon)` 오버로드로 직접 배치, 중복 드롭 방지 |
| `Assets/DragContainer.cs` | 드래그 중인 원본 아이콘 참조(`sourceIcon`) 보관 |
| `Assets/SoundSelect.cs` | `OnEnable()`의 **위치 셔플 제거** → 악기 위치 고정, 진입 시 `DragIcon` index 재동기화 |

---

## 3. 2회차 진입 크래시 (윈드메이커 완료 → 플라이 위드미 → 악기 선택 후)

> Tab A8이 `unauthorized`/`offline`이라 최신 logcat은 미확보. 코드 정적 분석으로 2회차 재진입 시 터질 수 있는 지점을 모두 보강.

### 원인 후보 및 조치

| 영역 | 문제 | 조치 |
|------|------|------|
| 웹캠 수명주기 | `WebCamInput.Start()`는 1회만 실행 → `ReleaseBodyCapture()` 후 2회차에서 텍스처/RT가 재생성되지 않아 null 접근 | `WebCamInput`에 **`EnsureCapture()`** + init 코루틴 추가. `inputRT`/`webCamTexture` null·정지 시 재생성. `BodyResourceLifecycle.ActivateWebcamUi()`에서 `EnsureCapture()` 호출 |
| 캐시된 참조 | `ReleaseBodyCapture()`가 static 캐시(`cachedWebCam/Pose/InputImage`)를 유지 → 파괴된 객체 참조 | Release 시 캐시 **모두 null 초기화** |
| Pose 디텍터 | `PoseVisuallizer.OnDisable()`이 `ReleaseDetector()`(Dispose) 호출 → 재활성 시 GPU 버퍼 경합 가능 | `OnDisable`은 루프 정지·플래그 리셋만, **`OnDestroy`에서만 Dispose** |
| `MusicSelect.selectMusic` | `clips[t[...]]`·`Yellow/Blue.audio_sources` 인덱스 무검증 → 범위 초과 시 예외 | `AssignInstrumentClips()`/`TryAssignClip()`로 **범위·null 검사**, 미지원 `source_idx` 가드 |
| `SoundSelect.MusicOn` | `bm`/`AudioSource`/`preSounds[song_idx]` 무검증 NRE | 전부 null·범위 검사 후 재생, 실패 시 경고 로그 |
| 코루틴 누수 | 재진입 시 이전 코루틴 잔존 | `SoundSelect.OnEnable/OnDisable`에 **`StopAllCoroutines()`**, `waitSound` 범위·활성 검사 |
| 슬롯 상태 | 2회차에서 이전 회차 슬롯 잔존 | `OnEnable`에서 **`ResetAllSlots()`** 로 슬롯·드롭 초기화 |

### 변경 파일

- `Assets/Script/WebCamInput.cs`
- `Assets/Script/BodyResourceLifecycle.cs`
- `Assets/Script/PoseVisuallizer.cs`
- `Assets/MusicSelect.cs`
- `Assets/SoundSelect.cs`

---

## 4. GitHub 업로드

| 항목 | 값 |
|------|-----|
| 저장소 | `https://github.com/nicecog/mtalk` |
| 브랜치 | `main` |
| 최초 커밋 | `0322786` — Initial commit: MBody_Revised_Clean Unity project |
| 업로드 파일 | 1,217개 (Assets·Packages·ProjectSettings·scripts·docs 등) |

### `.gitignore` 보강 (신규 제외)

- `/.idea/` (IDE 설정)
- `/.utmp/`, `/.gradle/` (안드로이드 빌드 임시)
- `.collabignore`
- 루트 `/*.log`

> 기존 제외: `Library/`, `Build/`, `Logs/`, `*.apk`, `*.aab`. `Assets`에 100MB 초과 파일 없음(전체 약 838MB). 추후 미디어 Git LFS 전환 고려.

---

## 4-A. 2회차 크래시 추가 분석 — BlazePose GPU 경합 (실기 logcat 확보 후)

> Tab A8 재연결 후 `diag.log` + crash 버퍼 확보. 4·5장의 방어 코드만으로는 **여전히 2회차 `MainGameCali` 진입 직후 네이티브 크래시** 발생 확인.

### 실기 로그로 확인한 크래시 패턴

```
Fatal signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x8
tid: Thread-5 (GPU), pid: com.CAU.MBody
Cause: null pointer dereference
backtrace: libunity.so (BuildId 9e42b1ef278251cb)
```

**흐름(2회차):** 1회차 `MainGame` 완료 → `EndMainGame playTimes=1` → `MusicSelect` → 악기 선택(yellow/blue) → `Alert` → `SpeedSelect` → `MainGameCali` → `Webcam ready 640x480` → `enabling pose after webcam settle` → **약 1~2초 후 SIGSEGV, 로그 종료**. C# 예외 없음 = 네이티브 GPU 스레드 크래시.

### 근본 원인 (2건)

1. **GPU 버퍼 동시 접근:** `PoseVisuallizer.ProcessImage()`가 프레임 시작에 `inputRT`를 읽는데, `WebCamInput.Update()`의 `Graphics.Blit(webCamTexture, inputRT)`는 같은 프레임에 같은 RT에 **쓰기**. 2회차 재진입 시 이 read/write 경합이 null deref로 이어짐.
2. **흰 배경:** 웹캠 `RawImage`가 텍스처가 준비되기 전에 `enabled=true`로 켜져 **기본 흰색**으로 표시. 1회차 이후 레이어가 남아 `MusicSelect`~`MainGameCali` 구간 배경을 덮음.

### 조치 (코드)

| 파일 | 변경 |
|------|------|
| `Assets/Script/WebCamInput.cs` | 전면 재작성. `LateUpdate`에서 Blit **완료 후** 별도 `poseSnapshotRT`로 복사 → BlazePose는 `PoseInputTexture`(스냅샷)만 사용해 GPU 동시 쓰기 제거. `HasValidFrame` 프로퍼티 추가. `EnsureCapture` 코루틴 가드 |
| `Assets/Script/PoseVisuallizer.cs` | `WaitForEndOfFrame` **후** `ProcessImage` 호출. `AsyncGPUReadback`로 비동기 회수(`GetData` 블로킹 제거), 미지원 기기만 폴백. 2회차(`playTimes>0`)는 settle 프레임 확대(10), Android 처리 간격 ≥8 |
| `Assets/Script/BodyResourceLifecycle.cs` | 웹캠 프레임이 **유효해질 때까지 RawImage 숨김**, 준비 후에만 텍스처 바인딩·표시(`ShowWebcamFeed`/`HideWebcamFeed`). `ReleaseBodyCapture`에서 Pose 정지 선행 |
| `Assets/FlowManager.cs` | `WebcamHiddenPages`(MusicSelect/SoundSelect/Alert/SpeedSelect/EndMessage) 진입 시 웹캠 UI 숨김(`HideWebcamBetweenGames`) → 흰 배경 제거 |
| `Assets/BodyGameScene.cs` | 웹캠 준비 완료 **후에만** `EnableInputImage()`. 2회차 추가 프레임 대기 후 Pose 활성 |

### 빌드·설치

- `scripts/build-android.ps1 -Variant release` → **빌드 성공**, `MBody-latest-stable.apk` (37.61 MB)
- Tab A8(`R9WR611YAAJ`) `adb install -r` → **Success** (OBB 기존 파일 유지)
- ⏳ **실기 2회차 E2E 재검증 대기** (배경 정상 + 크래시 해소 확인 필요)

---

## 5. 다음 세션 우선 작업

1. **Tab A8 USB 재연결**(현재 unauthorized) → 기기에서 디버깅 허용
2. **APK 빌드**: `scripts/build-android.ps1 -Variant release` (이번 세션 코드 미반영 상태)
3. **악기 선택 검증:** 오렌지/블루에서 누른 그 악기가 슬롯에 올라가는지
4. **2회차 흐름:** 윈드메이커 완료 → 플라이 위드미 선택 → 연주 → 악기 선택 후 크래시 없는지
5. 크래시 시 `diag.log` + `adb logcat`(SIGSEGV/Thread) 확보

---

## 6. 알려진 이슈

| 항목 | 상태 |
|------|------|
| 4-A장 GPU 경합 수정 APK | ✅ 빌드·Tab A8 설치 완료 |
| Tab A8 연결 | ✅ `device` (인증 완료) |
| 2회차 크래시 실기 재현 logcat | ✅ 확보 — SIGSEGV(libunity GPU) 원인 규명 |
| 2회차 크래시 실기 최종 검증 | ⏳ 신규 APK로 재현 여부 확인 대기 |
| 레거시 no-op onClick (`selectMusic` 등) | 💡 장기 정리 대상(현재 무해) |

---

*작성: 2026-06-27 — 악기 선택 정확도·2회차 크래시 방어 코드 + 씬 버튼 전수 점검 + GitHub(`nicecog/mtalk`) 업로드. 후속: BlazePose GPU 경합/흰 배경 수정(4-A장) APK 빌드·Tab A8 설치 완료, 실기 2회차 E2E 최종 검증 대기.*
