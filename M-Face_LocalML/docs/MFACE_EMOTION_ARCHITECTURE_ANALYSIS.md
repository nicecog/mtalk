# M-FACE 얼굴/표정 인식 심층 분석 및 LocalML 마이그레이션

> 분석 대상: `M-Face_Clean` (기존), `M-Face_LocalML` (로컬 ML 신규 복사본)  
> 서버: CAFE24 `http://hudit.cafe24.com:8080/mface`, 레거시 `https://175.45.203.182:21250`  
> **최종 업데이트:** 2026-06-14 (16:18) — 홈 버튼·로그인 알림 수정, Pre-Build 검증, APK 재빌드

---

## 1. 결론 요약

| 항목 | 기존 M-Face |
|------|-------------|
| **유료 클라우드 API** | Azure/AWS/Google Vision **미사용** |
| **얼굴 검출** | Unity 클라이언트 **없음** (전체 프레임 JPG 업로드) |
| **표정 분류** | **서버 HTTP 응답**에 의존 (`POST /api/mface-images`) |
| **CAFE24 백엔드 실제 동작** | `buildStubEmotion()` — **파일명 키워드**로 점수 생성 (CV 아님) |
| **게임 정답 처리** | 클라이언트가 파일명에 `correctType` 라벨을 넣어 업로드 → 스텁이 항상 “정답” 점수 반환 |

**즉, 현재 CAFE24 연동 상태에서는 “실제 웃는/우는 얼굴”을 인식하지 않습니다.**  
레거시 IP 서버(`175.45.203.182`)에 실제 CV가 있었을 가능성은 있으나, 이 워크스페이스에는 해당 서버 코드가 없습니다.

`M-Face_LocalML`은 **NatML + FER+ ONNX**로 기기 내 표정 추론을 수행하도록 분리 구현했습니다.

---

## 2. 클라이언트 아키텍처 (기존)

```
[Android Camera]
      ↓ WebCamTexture (1920×1080)
[WebCam / WebCamStage2]
      ↓ Pause → RenderTexture.Blit → JPG 저장
      ↓ POST /api/mface-images (multipart)
[서버 MfaceImageService]
      ↓ buildStubEmotion(파일명)
      ↓ JSON { emotion: "Neutral:0.05,Happy:0.82,..." }
[클라이언트]
      ↓ THR=0.15 규칙으로 0~3 분류 (Happy/Neutral/Sad/Anger)
      ↓ correctType 과 비교 → 게임 점수
```

### 핵심 클래스

| 파일 | 역할 |
|------|------|
| `FlowManager.cs` | 카메라 권한, 전면 카메라 선택, **홈/앱시작 페이지 분리** (`FirstPage` / `HomePage`) |
| `WebCam.cs` | Stage1 캡처·업로드·표정 판정 |
| `WebCamStage2.cs` | Stage2 동일 패턴 |
| `Stage1Manager.cs` | `correctType` 시행 설정 |
| `MFaceApiClient.cs` | HTTP 래퍼 (Clean 버전) |
| `Login.cs` | 인증 + CSV 동기화 (`_FaceData.csv`) |

### 표정 매핑 (M-Face 4종)

| `emotion` / `correctType` | 의미 |
|---------------------------|------|
| 0 | Happy (웃음) |
| 1 | Neutral |
| 2 | Sad (슬픔/울음 계열) |
| 3 | Anger |

### 서버 스텁 코드 (실제 CV 아님)

```java
// mtalk/.../MfaceImageService.java
if (lower.contains("happy")) happy = 0.82f;
else if (lower.contains("sad")) sad = 0.82f;
// ...
```

클라이언트 `SaveScreeJpg()`는 파일명에 **시행 정답**을 포함합니다:

```csharp
fn = ID + "_Stage1_" + timestamp + "_" + emotionString + ".jpg";
// emotionString = ct[sm1.correctType]  ← 정답 라벨이 파일명에 들어감
```

---

## 3. 외부/유료 API 여부

- **Azure Face API, AWS Rekognition, Google Cloud Vision**: 코드베이스에 **없음**
- **NatSuite NatCorder** (구 M-Face): 영상 녹화 SDK, 표정 분석 **미사용**
- **HTTP 자체 API**: CAFE24 자체 Spring Boot (`/mface/api/...`) — 클라우드 ML이 아닌 **자체 호스팅**

---

## 4. M-Face_LocalML 변경 사항

### 새 프로젝트 경로

`f:\PROJECT\Unity_Projects\M-Face_LocalML` — `M-Face_Clean` 복사본, 기존 프로젝트 **미변경**

### 추가 패키지 (NatML)

```json
"scopedRegistries": [
  { "name": "NatML", "url": "https://registry.npmjs.com", "scopes": ["ai.natml"] },
  { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["ai.natml"] }
],
"dependencies": {
  "ai.natml.natml": "1.1.16",
  "ai.natml.vision.blazeface": "1.0.8"
}
```

> **참고**: `ai.natml.vision.blazeface` npm 패키지는 unpublish 되었으나 OpenUPM 미러 사용.  
> BlazeFace는 **NatML Hub Access Key** 필요 (선택). 없으면 **중앙 크롭** 폴백.

### 로컬 ML 구성

| 파일 | 역할 |
|------|------|
| `Assets/LocalML/FerPlusLocalPredictor.cs` | FER+ ONNX/TFLite NatML 추론 |
| `Assets/LocalML/MFaceLocalEmotionAnalyzer.cs` | 얼굴 크롭 + 표정 분석 파이프라인 |
| `Assets/LocalML/MFaceEmotionMapper.cs` | FER+ 8클래스 → M-Face 4클래스 |
| `Assets/LocalML/MFaceLocalMLConfig.cs` | 로컬/서버 모드 PlayerPrefs |
| `Assets/LocalML/MFaceModelBootstrap.cs` | Android StreamingAssets → persistentDataPath TFLite 복사 |
| `Assets/MLModels/emotion-ferplus-8.onnx` | Editor/Windows용 번들 모델 (~33MB) |
| `Assets/StreamingAssets/ML/emotion-ferplus-8.tflite` | Android용 TFLite (~35MB, APK 포함) |
| `Assets/Editor/MFaceLocalMLTests.cs` | 에디터 메뉴 테스트 |
| `Assets/Editor/AutomatedAndroidBuild.cs` | APK 빌드 (`com.Spike.MFace.LocalML`) |

### WebCam 연동

- `MFaceLocalMLConfig.UseLocalEmotion == true` (기본값): **서버 emotion 무시**, 로컬 FER+ 사용
- `false`: 기존 서버 업로드 + 파싱 (레거시/스텁)
- 파일명: 로컬 모드 시 `LocalCapture` (정답 라벨 제거 → 스텁 오염 방지)

---

## 5. NatML / ML Kit 관련

- **Google ML Kit**: Unity 패키지로 직접 통합되어 있지 않음
- **NatML (NatMLX)**: Android NNAPI + TFLite, Windows ONNX — **온디바이스** 추론
- **Keijiro Takahashi**: NatML 작성자는 아님. NatML은 NatMLX(olokobayusuf) 프로젝트. Keijiro는 별도 Unity 도구 제작자.

### 플랫폼별 모델

| 플랫폼 | NatML 포맷 | 현재 상태 |
|--------|------------|-----------|
| Windows / Unity Editor | `.onnx` | `Assets/MLModels/emotion-ferplus-8.onnx` 번들 |
| Android | `.tflite` | `Assets/StreamingAssets/ML/emotion-ferplus-8.tflite` **배치 완료** |

### TFLite 변환 (2026-06-14 완료)

Windows 로컬 `pip onnx2tf`는 TensorFlow/numpy/ml_dtypes 의존성 충돌로 실패. **Docker onnx2tf**로 변환 성공.

```powershell
# 작업 디렉터리 준비
$work = "f:\PROJECT\Unity_Projects\M-Face_LocalML\Build\tflite_work"
Copy-Item "Assets\MLModels\emotion-ferplus-8.onnx" "$work\emotion-ferplus-8.onnx"

# Docker 변환 (--entrypoint 필수)
docker run --rm --entrypoint onnx2tf `
  -v "${work}:/workdir" -w /workdir `
  ghcr.io/pinto0309/onnx2tf:latest `
  -i emotion-ferplus-8.onnx -o out

# float32 출력 → StreamingAssets
Copy-Item "$work\out\emotion-ferplus-8_float32.tflite" `
  "Assets\StreamingAssets\ML\emotion-ferplus-8.tflite"
```

| 항목 | 값 |
|------|-----|
| 입력 ONNX | 33.4 MB |
| 출력 TFLite (float32) | 33.4 MB (`emotion-ferplus-8_float32.tflite`) |
| APK 내부 경로 | `assets/ML/emotion-ferplus-8.tflite` (35,037,864 bytes) |

---

## 6. 테스트 방법

### 에디터 (Windows)

1. Unity 6000.3.13f1로 `M-Face_LocalML` 열기
2. 메뉴: **M-Face → LocalML → Enable Local Emotion**
3. (선택) Edit → Project Settings → NatML Access Key — BlazeFace용
4. **M-Face → LocalML → Analyze Selected Texture** 또는 캡처 JPG 분석

### 단말기 설치 (Galaxy Tab A 8.0 2019 등)

**증상:** `앱이 설치되지 않았습니다` / `호환되지 않는 앱`

**원인:** 초기 APK가 `arm64-v8a`만 포함. Tab A 8.0 (SM-T290)은 **32-bit Android**로 동작하는 경우가 많아 `armeabi-v7a` 네이티브 라이브러리 필요.

**해결:** 2026-06-14 이후 빌드는 ARMv7+ARM64 universal APK 사용.

```text
APK 내부 ABI:
  lib/armeabi-v7a/*.so   ← 32-bit 태블릿용
  lib/arm64-v8a/*.so     ← 64-bit 기기용
```

설치 전 체크:
1. 기존 `com.Spike.MFace.LocalML` / `com.Spike.MFace` 충돌 시 먼저 삭제
2. 저장 공간 **500 MB 이상** 여유 (APK 246 MB + 압축 해제)
3. 설정 → 보안 → **알 수 없는 앱 설치** 허용
4. PC 연결 시 정확한 오류 확인:

```powershell
adb install -r "Build\Android\MFace-LocalML-latest-stable.apk"
```

**증상:** 설치는 되지만 홈 화면 아이콘 없음, 설치 완료 화면 **열기** 버튼 비활성, 설정→앱에는만 표시

**원인:** `Assets/Plugins/Android/AndroidManifest.xml`에 `tools:replace`만 있고 **MAIN/LAUNCHER Activity 없음** → Android가 실행 가능한 진입점을 찾지 못함.

**해결:** `UnityPlayerActivity` + `MAIN`/`LAUNCHER` intent-filter 추가 후 재빌드 (2026-06-14).

### 단말기 테스트

1. APK 설치: `Build/Android/MFace-LocalML-latest-stable.apk`
2. 로그인 (`hudit` / `hudit`) → Toast "정상 로그인 되었습니다" → IntroMovie
3. Stage1 카메라 → 확인 버튼
4. `adb logcat`에서 `[MFaceLocalML]` 로그 확인 (로컬 FER+ 추론)
5. 첫 실행 시 TFLite가 `persistentDataPath`로 복사됨 (`MFaceModelBootstrap`)
6. **홈 버튼:** 트레이닝 중 우상단 홈 → "중단하기" → **Stage1Start** (로그인 화면 아님)

### 화면 흐름 (Page)

```
앱 시작:  1.IntroLogo → 2.LoginPage → 3.IntroMovie → 4.Stage1Start (메인)
Stage1:   4.Stage1Start → 5.Stage1Intro → 6.Stage1 (카메라)
Stage2:   4.Stage1Start → 7.Stage2Start → 8.Stage2Intro → 10.Stage2SongSelect → ...
홈 버튼:  (트레이닝 중) → 4.Stage1Start
```

`FlowManager.FirstPage` = IntroLogo (앱 시작 전용)  
`FlowManager.HomePage` = Stage1Start (로그인 후 메인)

### APK 빌드

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "f:\PROJECT\Unity_Projects\M-Face_LocalML" `
  -executeMethod AutomatedAndroidBuild.BuildLatestStableAndroidApk `
  -logFile "Build\Android\build-tflite.log"
```

| 항목 | 값 |
|------|-----|
| 출력 APK | `Build/Android/MFace-LocalML-latest-stable.apk` |
| 패키지 ID | `com.Spike.MFace.LocalML` |
| Unity | 6000.3.13f1 (Android Player 모듈 필요) |
| Scripting | IL2CPP, **ARMv7 + ARM64** (Galaxy Tab A 8.0 2019 등 32-bit 태블릿 호환) |
| TFLite 미포함 빌드 | ~198 MB (2026-06-14 초반) |
| TFLite 포함 (ARM64 only) | ~233 MB — Tab A 8.0 **설치 실패** (ABI 불일치) |
| **TFLite 포함 (ARMv7+ARM64)** | **~246 MB** (2026-06-14 16:18) — `armeabi-v7a` + `arm64-v8a` |
| 빌드 로그 (최신) | `Build/Android/build-home-fix.log` |

### API 검증 (로그인/CSV)

```powershell
# Login.cs 흐름 재현
powershell -File scripts\Test-MFaceLoginApi.ps1

# 빌드 전 통합 검증 (모델·씬·API·APK)
powershell -File scripts\Test-MFacePreBuild.ps1
# 16/16 PASS (2026-06-14)
```

```powershell
powershell -File mtalk\scripts\Verify-UnityCafe24Api.ps1
```

표정 API는 로컬 ML 모드에서 **게임 판정에 사용하지 않음**.

---

## 7. 알려진 한계 및 다음 단계

1. **FER+ 정확도**: 64×64 grayscale, 조명/각도에 민감 — BlazeFace 키 권장
2. ~~**Android TFLite**: 미배치~~ → **2026-06-14 배치·APK 번들 완료**
3. ~~**홈 버튼 로그인 이동**~~ → **2026-06-14 `HomePage` 분리로 수정**
4. **NatML archived**: 장기적으로 MediaPipe Unity / Barracuda FER+ 대안 검토
5. **서버 스텁**: 로컬 ML 모드(`UseLocalEmotion=true`)에서는 게임 판정에 미사용
6. **릴리스 서명**: debug keystore 사용 중 — 스토어 배포 시 release keystore 설정 필요
7. **실기기 E2E**: APK 빌드 완료 (2026-06-14 16:18) — 태블릿 설치·표정 인식 검증 진행 중

---

## 8. 파일 변경 요약 (M-Face_LocalML만)

- `WebCam.cs`, `WebCamStage2.cs` — 로컬 분석 분기
- `FlowManager.cs` — `HomePage` / `GoToPage()` / `StopTrainingAudio()` (홈 버튼 수정)
- `Login.cs` — 로그인 검증·오류 처리·CSV 안전 처리
- `Login/MFaceLoginAlert.cs` — **신규** (Toast + 화면 알림)
- `Packages/manifest.json` — NatML 의존성 (OpenUPM scoped registry만 사용)
- `Assets/LocalML/*` — 신규 (6개 스크립트)
- `Assets/MLModels/emotion-ferplus-8.onnx` — 신규
- `Assets/StreamingAssets/ML/emotion-ferplus-8.tflite` — 신규 (2026-06-14)
- `Assets/Scenes/MainScene.unity` — `HomePage`, LoginPage `ButtonFlow` 수정
- `Assets/Plugins/Android/AndroidManifest.xml` — MAIN/LAUNCHER Activity
- `Assets/Editor/AutomatedAndroidBuild.cs` — APK 빌드 (ARMv7+ARM64)
- `scripts/Test-MFaceLoginApi.ps1` — **신규** 로그인 API 검증
- `scripts/Test-MFacePreBuild.ps1` — **신규** 빌드 전 통합 검증
- `Build/tflite_work/` — ONNX→TFLite 변환 작업 디렉터리 (Docker 산출물)

**M-Face_Clean 은 변경 없음.**

---

## 9. 작업 타임라인

| 일자 | 작업 | 결과 |
|------|------|------|
| 2026-06-13 | `M-Face_Clean` 표정 아키텍처 분석, CAFE24 스텁 확인 | 문서화 |
| 2026-06-13 | `M-Face_LocalML` 프로젝트 복사, NatML 패키지 추가 | Unity import 성공 |
| 2026-06-13 | 로컬 ML 파이프라인 구현 (`LocalML/*`, WebCam 연동) | Editor 컴파일 OK |
| 2026-06-14 | TFLite 미포함 APK 1차 빌드 | ~198 MB, 로컬 추론 기기 실패 예상 |
| 2026-06-14 | Docker `onnx2tf`로 FER+ ONNX→TFLite 변환 | float32 33.4 MB |
| 2026-06-14 | StreamingAssets 배치 + APK 재빌드 (ARM64 only) | ~233 MB, Tab A 8.0 설치 실패 |
| 2026-06-14 | **ARMv7+ARM64 universal APK 재빌드** | **~246 MB**, `armeabi-v7a` 포함 |
| 2026-06-14 | **런처 Activity 수정** (`AndroidManifest.xml`) | 아이콘/열기 버튼 비활성화 해결 |
| 2026-06-14 | **로그인 알림** (`MFaceLoginAlert`, `Login.cs`) | Toast + 오류 메시지, ButtonFlow 수정 |
| 2026-06-14 | **홈 버튼 수정** (`HomePage` = Stage1Start) | 트레이닝 중단 시 로그인 화면 이동 버그 해결 |
| 2026-06-14 | **Pre-Build 검증** (`Test-MFacePreBuild.ps1`) | 16/16 PASS |
| 2026-06-14 | **APK 재빌드** (홈·로그인 수정 반영) | 258 MB, `build-home-fix.log` |
| — | Galaxy Tab A 8.0 (2019) Android 11 실기기 E2E | **진행 중** (APK 설치·테스트) |

### 관련 4앱 APK 현황 (동일 세션)

| 앱 | 프로젝트 | APK | 크기 |
|----|----------|-----|------|
| M-BENCH | `MBench` | `Build/Android/MBench-latest-stable.apk` | ~424 MB |
| M-BODY | `MBody_Revised_Clean` | `Build/Android/MBody-latest-stable.apk` | ~21 MB |
| M-SOCIAL | `M-Social_Clean` | `Build/Android/MSocial-latest-stable.apk` | **~272 MB** (v1.15) |
| M-FACE (Clean) | `M-Face_Clean` | `Build/Android/MFace-latest-stable.apk` | ~180 MB |
| **M-FACE (LocalML)** | `M-Face_LocalML` | `Build/Android/MFace-LocalML-latest-stable.apk` | **~246 MB** (2026-06-14 16:18) |

### CAFE24 API 검증

```powershell
powershell -File mtalk\scripts\Verify-UnityCafe24Api.ps1
# 6/6 PASS (로그인·계정 생성)
```

---

*이 문서는 M-FACE 표정 인식 분석 및 LocalML 마이그레이션 작업 내역을 정리합니다. 코드 변경 시 함께 갱신하세요.*
