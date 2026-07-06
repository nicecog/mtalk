# M-Face 프로젝트 분석 (Clean 작업본)

> 작성: 2026-06-10  
> 원본: `F:\PROJECT\Unity_Projects\M-Face` (보존, 미변경)  
> 작업본: `F:\PROJECT\Unity_Projects\M-Face_Clean` (Library/Temp/Logs/obj/.idea 제외 복사)

---

## 1. 프로젝트 개요

| 항목 | 값 |
|------|-----|
| 앱명 | M-Face |
| Unity | **2020.3.8f1** |
| 패키지 ID | `com.Spike.MFace` |
| 플랫폼 | Android 태블릿 (가로 고정), iOS 설정은 미완 |
| 크기 (Clean) | ~180 MB (Assets + Packages, Library 제외) |
| 게임 장르 | 음악-감정 매칭 + 웹캠 표정 훈련 (2단계) |
| ML | **온디바이스 없음** — 서버가 JPG 분석 후 감정 점수 반환 |

### MBody와의 차이

| | MBody_Revised_Clean | M-Face_Clean |
|--|---------------------|--------------|
| Unity | 6000.3.13f1 | 2020.3.8f1 |
| 포즈/얼굴 | BlazePose (온디바이스 GPU) | 서버 API 감정 분석 |
| API 클라이언트 | `JsonRequest.cs` (통합) | `Login.cs` + `WebCam*.cs` (분산) |
| 로그인 API | `/api/users/login` | `/api/users/sign-in` |
| 업로드 | MP4 + subject-videos | JPG/CSV → `/api/mface-images` |
| 서버 URL | Local/Remote/Custom 선택 | `175.45.203.182:21250` 하드코딩 |

---

## 2. 디렉터리 구조

```
M-Face_Clean/
├── Assets/
│   ├── *.cs                    # 게임 스크립트 14개 (루트)
│   ├── Animation/                # UI 애니메이터
│   ├── MUZI 성우 녹음/           # 한국어 성우 MP3
│   ├── NatSuite/NatCorder/       # 영상 녹화 SDK (미사용)
│   ├── Page/                     # UI 스프라이트, 표정 가이드 PNG
│   ├── Plugins/Android/          # Gradle 템플릿
│   ├── Scenes/
│   │   ├── MainScene.unity       # 유일한 빌드 씬
│   │   └── SampleScene.unity     # 미사용 2D 템플릿
│   └── StreamingAssets/          # 130+ 트라이얼 음악 (H/N/S/A × 난이도)
├── Packages/manifest.json
├── ProjectSettings/
└── docs/                         # 이 문서
```

---

## 3. 게임 스크립트 (14개)

| 파일 | 역할 |
|------|------|
| `FlowManager.cs` | 앱 부트스트랩, Android 권한, 전면 카메라 선택, 페이지 리셋 |
| `Login.cs` | 로그인/계정생성, CSV 동기화, `ItemString`·`playNum` 관리 |
| `ButtonFlow.cs` | 페이지 전환 |
| `TimerFlow.cs` | N초 후 자동 페이지 이동 |
| `Stage1Manager.cs` | 1단계: 12회 음악→감정카드→웹캠 검증 |
| `Stage2Manager.cs` | 2단계: 곡 선택 → 아이템 선택 진입 |
| `ItemSelect.cs` | 2단계 아이템/보상, 게이지, `ItemString` 잠금 해제 |
| `WebCam.cs` | 1단계 웹캠: JPG 캡처 → 서버 업로드 → 감정 판정 |
| `WebCamStage2.cs` | 2단계 웹캠: 랜덤 표정 과제, 업로드/점수 |
| `InstructionChange.cs` | 1단계 안내 스프라이트 전환 |
| `InstructionChange2.cs` | 2단계 안내 스프라이트 전환 |
| `AudioController.cs` | 성우 클립 + StreamingAssets MP3 로드 |
| `InfoWindow.cs` | 수집 아이템 정보 팝업 |

**미사용:** NatCorder 16개 스크립트 (게임 코드에서 import 없음)

---

## 4. 앱 플로우

```
IntroLogo → LoginPage → IntroMovie
    → Stage1Start → Stage1Intro → Stage1 (12 trials)
        → [음악 재생] → [감정 카드 선택] → [WebCam 캡처]
        → 서버 감정 분석 → 정답/오답 피드백
    → Stage2Start → Stage2Intro → Stage2SongSelect
        → Stage2ItemSelect → [WebCamStage2 5~10 rounds/item]
        → 아이템 획득 (ItemString 16슬롯)
    → EndPop
```

### 감정 분류 (4종)

- 0: Happy, 1: Neutral, 2: Sad, 3: Anger
- 서버 응답 7차원 (Neutral, Happy, Sad, Surprise, Fear, Disgust, Anger) 중 4개 사용
- 임계값 `THR = 0.15f`

---

## 5. 서버 API (클라이언트 기준)

**Base URL (하드코딩):** `https://175.45.203.182:21250`

| Method | Path | 사용처 |
|--------|------|--------|
| GET | `/api/users/accounts/id/has/{id}` | `Login.AdminLogin` |
| POST | `/api/users/sign-in` | 관리자/사용자 로그인 |
| POST | `/api/users/accounts` | 신규 계정 생성 |
| GET | `/api/users/sign-out` | 로그아웃 |
| GET | `/api/mface-images?&account={seq}` | 사용자 파일 목록 |
| GET | `/api/mface-images/{key}` | CSV 다운로드 |
| POST | `/api/mface-images` | JPG/CSV multipart 업로드 |
| DELETE | `/api/mface-images/{seq}` | 구 CSV 삭제 후 재업로드 |

**진행 데이터 CSV 형식** (`{userId}_FaceData.csv`):

```csv
Day_Count,Last_Login,Item
1,2026-06-10,FFFFFFFFFFFFFFFF
Date,Time,Log
...
```

- `ItemString`: 16자 F/T 문자열 (아이템 수집 여부)
- `playNum`: 일차 카운트

---

## 6. 빌드 설정

| 설정 | 값 | 비고 |
|------|-----|------|
| minSdk | 24 | |
| targetSdk | **28** | Play Store 요구(33+) 미달 |
| Architecture | **ARMv7 only** | 64비트 미지원 |
| Orientation | Landscape only | |
| Gradle AGP | 3.4.0 | 구버전 |
| jcenter() | 사용 중 | 2021년 폐쇄 |

---

## 7. 발견된 이슈 (수정 후보)

### 🔴 버그

1. **Stage2 업로드 user 하드코딩** — `WebCamStage2.cs:251`  
   `"user","1"` → 실제 `seq` 사용해야 함 (Stage1은 `seq` 정상 사용)

2. **CSV Day_Count 갱신 버그** — `Login.cs:256`  
   `line[2]` (문자열 3번째 **문자**) vs `tk[2]` (CSV 3번째 **필드**)  
   → `ItemString`이 한 글자만 남을 수 있음

3. **seq 파싱 취약** — `Login.cs:150`  
   `s.Split(':')[1]` — 따옴표/공백 미제거 (MBody `JsonRequest`는 trim 처리됨)

### 🟠 보안/운영

- URL `175.45.203.182:21250` 20곳 이상 중복 하드코딩
- `BypassCertificate` — TLS 검증 전면 비활성화
- `super-admin` / `super-admin!@#$` 클라이언트 내장
- 테스트 ID `test`/`admin` 우회 로직 (`Login.cs:53-60`)

### 🟡 기술 부채

- Unity 2020.3.8f1 (MBody는 6000.3)
- NatCorder 전체 미사용 (~수십 MB 네이티브 플러그인)
- `SampleScene.unity` 빌드 미포함 잔재
- `FindObjectOfType` 느슨한 결합
- 세션 쿠키 수동 관리 없음 (MBody 대비)
- 전용 서버 프로젝트가 워크스페이스에 **없음** (가비아 원격 서버 의존)

### 🟢 MBody_Clean 대비 개선 여지

- `JsonRequest` 패턴으로 API URL/세션 통합
- `PerformanceManager` 유사 적응형 카메라 해상도 (현재 1920×1080 고정)
- Local/Remote/Custom URL 선택
- Editor 자동 빌드 스크립트 부재

---

## 8. 적용 완료 (2026-06-10)

| # | 작업 | 상태 |
|---|------|------|
| 1 | WebCamStage2 `seq` 버그, Login CSV `tk[2]` 수정 | ✅ |
| 2 | `MFaceApiClient.cs` 추가, Login/WebCam 리팩터 | ✅ |
| 3 | NatCorder 삭제, SampleScene 삭제 | ✅ |
| 4 | targetSdk 33, ARM64, Gradle 4.0.1, mavenCentral | ✅ |
| 5 | `MBody_Revised_server`에 M-Face API 추가 | ✅ |

### 로컬 서버 (M-Face API)

`MBody_Revised_server`에 추가된 엔드포인트:

- `POST /api/users/sign-in` (login 별칭)
- `GET /api/users/sign-out` (logout 별칭)
- `GET/POST/DELETE /api/mface-images`
- 계정 생성 `level` 필드 별칭 (`levelId`)

검증 스크립트: `scripts/mface-api-verification.ps1`

Unity 로컬 테스트: `MFaceApiClient.apiTarget = Local`

---

## 9. 복사 정보

```
원본:  F:\PROJECT\Unity_Projects\M-Face
대상:  F:\PROJECT\Unity_Projects\M-Face_Clean
제외:  Library, Temp, Logs, obj, .idea, *.csproj, *.sln
```

원본은 수정하지 않음. 이후 모든 변경은 `M-Face_Clean`에서 진행.
