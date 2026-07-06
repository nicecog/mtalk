# MBody / M-Face 세션 핸드오프

> 작성: 2026-06-10  
> **갱신: 2026-06-14 (16:18)** — 홈 버튼 수정, 로그인 알림, Pre-Build 검증, APK 재빌드 반영 (§10–§11)  
> 실기기 테스트: **APK 빌드 완료** → 태블릿 설치·E2E 진행 중

---

## 1. 이번 세션 요약

| 프로젝트 | 작업본 | 상태 |
|----------|--------|------|
| MBody_Revised | `MBody_Revised_Clean` | API 14/14 PASS (이전 세션 + 회귀) |
| M-Face | `M-Face_Clean` | 분석·수정·API 22/22 PASS |
| M-Social | `M-Social_Clean` | **완료** — API·빌드·MP4압축·Tab A 대응 ([SESSION_HANDOFF](../M-Social_Clean/docs/SESSION_HANDOFF_2026-06-14.md)) |

**공유 서버:** `MBody_Revised_server` (`http://127.0.0.1:21250`)  
→ MBody + M-Face API 동시 지원 확인

---

## 2. MBody_Revised_Clean (참고)

- Unity **6000.3.13f1**, BlazePose 온디바이스, `PerformanceManager` 성능 적응
- API 클라이언트: `JsonRequest.cs` (Local/Remote/Custom)
- 검증: `MBody_Revised_server/scripts/unity-api-full-verification.ps1` → **14/14 PASS**
- 실기기: APK 빌드 완료, USB 기기 미연결로 E2E 보류

---

## 3. M-Face_Clean 완료 작업

### 3.1 복사

```
원본:  F:\PROJECT\Unity_Projects\M-Face
작업본: F:\PROJECT\Unity_Projects\M-Face_Clean (~180 MB, Library 제외)
```

### 3.2 버그 수정

| 파일 | 내용 |
|------|------|
| `WebCamStage2.cs` | 업로드 `user` `"1"` → `seq` |
| `Login.cs` | CSV `line[2]` → `tk[2]`, seq 파싱 trim |

### 3.3 API 레이어 통합

- 신규 `MFaceApiClient.cs` — URL 선택, 세션 쿠키, HTTPS bypass 중앙화
- `Login.cs`, `WebCam.cs`, `WebCamStage2.cs` 리팩터 (하드코딩 URL 제거)

### 3.4 미사용 자원 정리

- `Assets/NatSuite/` 삭제
- `Assets/Scenes/SampleScene.unity` 삭제

### 3.5 빌드 현대화

- targetSdk 28 → **33**, ARMv7 → **ARM64**
- Gradle: mavenCentral, AGP 4.0.1, R8 활성화

### 3.6 문서

| 파일 | 내용 |
|------|------|
| `docs/PROJECT_ANALYSIS.md` | 전체 분석 |
| `docs/CLEANUP_REPORT.md` | 정리 보고서 |
| `docs/MFACE_LOCAL_API_VERIFICATION_LATEST.md` | API 검증 결과 |

---

## 4. MBody_Revised_server 확장 (M-Face 지원)

### 추가 엔드포인트

| API | 설명 |
|-----|------|
| `POST /api/users/sign-in` | `/login` 별칭 |
| `GET /api/users/sign-out` | `/logout` 별칭 |
| `GET/POST/DELETE /api/mface-images` | CSV/JPG 저장 |
| 계정 생성 `level` 필드 | `levelId` JsonAlias |

### 추가 DB

- `V2__mface_images.sql` → `mface_images` 테이블

### 감정 분석 (로컬)

- JPG 업로드 시 파일명(Happy/Neutral/Sad/Anger) 기반 **스텁** 반환
- 원격 `175.45.203.182` 실제 ML과 결과 다를 수 있음

---

## 5. M-Face API 세부 검증 결과

**스크립트:** `M-Face_Clean/scripts/mface-api-full-verification.ps1`  
**결과:** **22/22 PASS** (2026-06-10 23:10)  
**리포트:** `docs/MFACE_LOCAL_API_VERIFICATION_LATEST.md`

| 카테고리 | 테스트 수 | 내용 |
|----------|-----------|------|
| A–E, M | 7 | 인증 (sign-in/sign-out, has, bootstrap, seq) |
| F–J | 5 | `_FaceData.csv` 업로드·목록·다운로드·로그·ItemString |
| K–L | 5 | Stage1 JPG 4종 감정 + Stage2 seq 업로드 |
| N–P | 5 | negative, MBody 공유, DB 영속화 |

**동시 검증:** 같은 서버에서 MBody `unity-api-full-verification.ps1` → **14/14 PASS**

---

## 6. 공유 서버 아키텍처

```
MBody_Revised_server :21250
├── /api/health
├── /api/users/*          ← MBody(login) + M-Face/M-Social(sign-in) 공유
├── /api/files/attach     ← MBody 전용 (MP4)
├── /api/subject-videos   ← MBody 전용
├── /api/mface-images     ← M-Face/M-Social 전용 (CSV·JPG)
└── /admin                ← 관리자
```

### 계정 ID 규칙 차이

| 앱 | ID 형식 |
|----|---------|
| MBody | `s_{입력ID}` |
| M-Face / M-Social | `{입력ID}` 소문자 |

---

## 7. 빠른 실행

```powershell
# 서버
cd F:\PROJECT\Unity_Projects\MBody_Revised_server
docker compose up -d
java -jar target\mbody-revised-server-0.0.1-SNAPSHOT.jar

# MBody API 검증
.\scripts\unity-api-full-verification.ps1

# M-Face API 검증
cd F:\PROJECT\Unity_Projects\M-Face_Clean
.\scripts\mface-api-full-verification.ps1
```

### Unity 로컬 연결

| 앱 | 설정 |
|----|------|
| MBody | `JsonRequest.apiTarget = Local` |
| M-Face | `MFaceApiClient.apiTarget = Local` |
| 실기기 | Custom + PC LAN IP (예: `http://192.168.x.x:21250`) |

---

## 8. 보류 / 다음 작업

1. **실기기 E2E** — LocalML APK 설치 후 홈 버튼·로그인·Stage1/2·`[MFaceLocalML]` logcat 확인 (**진행 중**)
2. **M-Social_Clean** — API·빌드·MP4압축·Tab A 대응 **완료** ([SESSION_HANDOFF](../M-Social_Clean/docs/SESSION_HANDOFF_2026-06-14.md))
3. **감정 ML** — 로컬 FER+ 정확도 튜닝 (BlazeFace Access Key 선택)
4. **계정 ID 통일** — MBody `s_` prefix vs M-Face/M-Social plain ID

---

## 9. 관련 경로

| 항목 | 경로 |
|------|------|
| MBody 작업본 | `F:\PROJECT\Unity_Projects\MBody_Revised_Clean` |
| M-Face 작업본 | `F:\PROJECT\Unity_Projects\M-Face_Clean` |
| **M-Face LocalML** | `F:\PROJECT\Unity_Projects\M-Face_LocalML` |
| M-Social 작업본 | `F:\PROJECT\Unity_Projects\M-Social_Clean` |
| 공유 서버 | `F:\PROJECT\Unity_Projects\MBody_Revised_server` |
| CAFE24 통합 서버 | `F:\PROJECT\Unity_Projects\mtalk` |

---

## 10. M-Face_LocalML 추가 작업 (2026-06-14)

> 상세: `docs/MFACE_EMOTION_ARCHITECTURE_ANALYSIS.md`

| 단계 | 내용 | 결과 |
|------|------|------|
| 프로젝트 복사 | `M-Face_Clean` → `M-Face_LocalML` | 완료 (원본 미변경) |
| NatML 연동 | FER+ ONNX + BlazeFace(선택) | Editor 컴파일 OK |
| 로컬 추론 | `Assets/LocalML/*`, WebCam 분기 | `UseLocalEmotion=true` 기본 |
| TFLite 변환 | Docker `onnx2tf` | float32 33.4 MB |
| APK 1차 | TFLite 미포함 | ~198 MB |
| APK (ARM64 only) | TFLite StreamingAssets 번들 | ~233 MB — Tab A 8.0 설치 실패 |
| **APK universal** | ARMv7 + ARM64 + TFLite + 런처 Activity | **~246 MB** (2026-06-14) |

### APK 경로 (최신)

`F:\PROJECT\Unity_Projects\M-Face_LocalML\Build\Android\MFace-LocalML-latest-stable.apk`

| 항목 | 값 |
|------|-----|
| 빌드 시각 | 2026-06-14 16:18 |
| 크기 | 258,157,415 bytes (~246 MB) |
| 패키지 | `com.Spike.MFace.LocalML` |
| ABI | `armeabi-v7a` + `arm64-v8a` |
| 빌드 로그 | `Build/Android/build-home-fix.log` |

### CAFE24 테스트 계정

| 항목 | 값 |
|------|-----|
| 서버 | `http://hudit.cafe24.com:8080/mface` |
| 이용자 ID | `hudit` |
| 비밀번호 | `hudit` |
| seq | 23 (2026-06-14 확인) |

---

## 11. M-Face_LocalML UI·네비게이션 수정 (2026-06-14)

### 11.1 로그인 버튼 무반응 / 알림 없음

| 파일 | 수정 |
|------|------|
| `MainScene.unity` | `LoginPage` `ButtonFlow.SelfPage` → `LoginPage` (기존 `IntroLogo` 오설정) |
| `Login.cs` | 입력 검증, API 오류 메시지, `loginBusy` 가드, CSV `fileUpdate` 빈 응답 처리 |
| `Login/MFaceLoginAlert.cs` | **신규** — Android Toast + `adminText` 화면 메시지 |

로그인 성공 시: "정상 로그인 되었습니다" → `IntroMovie` 이동.

### 11.2 홈 버튼 → 로그인 화면으로 가는 버그

**증상:** 트레이닝 중 우상단 홈 → "중단하기" → 로그인 화면으로 이동.

**원인:** `FlowManager.HomeReset()`이 `ResetPage()` 호출 → `FirstPage`(`1. IntroLogo`) 활성화 → `IntroLogo → LoginPage` 흐름.

**수정:**

| 파일 | 내용 |
|------|------|
| `FlowManager.cs` | `HomePage` 필드 추가, `GoToPage()` 공통화, `HomeReset()` → `HomePage` |
| `MainScene.unity` | `HomePage` = `4. Stage1Start` (fileID `199373684`) |
| `FlowManager.cs` | `HomeReset()` 시 `StopTrainingAudio()` — 트레이닝 음성 정지 |

| 구분 | 목적지 |
|------|--------|
| 앱 시작 (`Awake`) | `FirstPage` = `1. IntroLogo` |
| 홈 버튼 → 중단하기 | `HomePage` = `4. Stage1Start` (스테이지 선택 메인) |

### 11.3 Galaxy Tab A 8.0 (2019) 설치·실행

| 이슈 | 조치 |
|------|------|
| `앱이 설치되지 않았습니다` | ARMv7+ARM64 universal APK |
| 홈 아이콘 없음 / 열기 비활성 | `AndroidManifest.xml`에 `UnityPlayerActivity` + MAIN/LAUNCHER |

### 11.4 자동 검증 스크립트

| 스크립트 | 용도 | 결과 |
|----------|------|------|
| `scripts/Test-MFaceLoginApi.ps1` | `Login.cs` AdminLogin 흐름 재현 | ALL PASS |
| `scripts/Test-MFacePreBuild.ps1` | 모델·씬·API·APK 통합 검증 | **16/16 PASS** |
| `Assets/Editor/Cafe24ApiVerification.cs` | Unity Editor 메뉴 `MTalk/Verify CAFE24 API` | 준비됨 |

```powershell
# 빌드 전 통합 검증
powershell -File "F:\PROJECT\Unity_Projects\M-Face_LocalML\scripts\Test-MFacePreBuild.ps1"

# APK 빌드
& "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -quit -nographics `
  -projectPath "F:\PROJECT\Unity_Projects\M-Face_LocalML" `
  -executeMethod AutomatedAndroidBuild.BuildLatestStableAndroidApk `
  -logFile "Build\Android\build-home-fix.log"
```

### 11.5 실기기 테스트 체크리스트

1. APK 설치: `adb install -r Build\Android\MFace-LocalML-latest-stable.apk`
2. 로그인 `hudit` / `hudit` → Toast + IntroMovie
3. Stage1/2 트레이닝 중 **홈 → 중단하기** → **Stage1Start** (로그인 X)
4. 카메라 권한 → 촬영 → 확인 → `adb logcat -s Unity | findstr MFaceLocalML`
5. Info/종료 팝업 동작 확인

### 11.6 다음

- 실기기 E2E 결과 반영 (표정 인식 정확도, Stage2 보상 CSV)
- BlazeFace Access Key (선택) — 얼굴 크롭 정확도
- APK release keystore (스토어 배포 시)
