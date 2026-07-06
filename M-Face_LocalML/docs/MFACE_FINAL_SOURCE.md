# M-Face 최종 APK 소스 (LocalML)

> **Canonical project:** `M-Face_LocalML`  
> **Deprecated:** `M-Face_Clean` (레거시 서버 스텁·JPG 업로드 방식, 유지보수 중단)

| 항목 | 값 |
|------|-----|
| 패키지 ID | `com.Spike.MFace` |
| APK 출력 | `Build/Android/MFace-latest-stable.apk` |
| 표정 분석 | 기기 내 NatML + FER+ (온디바이스) |
| JPG 서버 업로드 | **사용 안 함** (로컬 캡처 → FER+ 분석만) |

## 진행도·로그 저장 (CSV 대체)

예전 `{userId}_FaceData.csv` 파일을 서버에 올리던 방식은 **사용하지 않습니다.**  
동일 데이터는 **REST API + DB**로 처리합니다.

| 앱 동작 | 로컬 (`MFaceCsvStore`) | 서버 API | DB 테이블 |
|---------|------------------------|----------|-----------|
| 로그인 후 진행도 불러오기 | 서버 성공 시 로컬 미러 | `GET /mface/api/players/me/progress` | `face_progress` |
| 아이템/일차 저장 | `TryUpdateProgress` | `PUT /mface/api/players/me/progress` | `face_progress` |
| Stage 종료 로그 | `TryAppendLog` | `POST /mface/api/players/me/activity-logs` | `face_activity_logs` |
| 페이지 진입 동기화 | `MFaceProgressSync` → 서버 pull | `GET .../progress` | `face_progress` |
| 서버 실패 시 | 로컬 CSV fallback | — | — |

**필드 매핑:** `Day_Count` → `dayCount`, `Last_Login` → `lastVisitDate`, `Item` → `itemString`(16자), 로그 → `logDate`/`logTime`/`logBody`.

## 빌드

```powershell
powershell -File M-Face_LocalML\scripts\build-android.ps1
```

## 검증

```powershell
powershell -File M-Face_LocalML\scripts\Test-MFaceLoginApi.ps1
powershell -File mtalk\scripts\Test-MFaceProgressApi.ps1
powershell -File M-Face_LocalML\scripts\Test-MFacePreBuild.ps1
```

## 더 이상 사용하지 않는 API

- `POST /api/mface-images` — JPG/CSV 업로드 (Clean 레거시)
- 서버 `emotion` 스텁 (`buildStubEmotion`) — 표정 판정 미사용
