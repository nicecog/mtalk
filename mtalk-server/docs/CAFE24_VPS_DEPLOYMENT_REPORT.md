# CAFE24 VPS 배포 검증 리포트 (hudit.cafe24.com)

> Run: 2026-06-13  
> Server: `http://hudit.cafe24.com:8080`  
> VPS IP: `1.234.65.46`  
> Stack: Docker + PostgreSQL 18.4 + JDK 21 (Temurin JRE) + Spring Boot 3.4.5

## 배포 상태

| 항목 | 상태 |
|------|------|
| Docker | 설치 완료 |
| PostgreSQL 18.4 | `mtalk-postgres` healthy |
| MTalk 서버 | `mtalk-server` port 8080 |
| 헬스체크 | `GET /health` → UP |

## API 검증 (서버)

| 테스트 | 결과 |
|--------|------|
| Verify-MTalkApi-Full.ps1 | **31/31 PASS** |
| Verify-UnityCafe24Api.ps1 | **6/6 PASS** |

## Unity 클라이언트 URL 변경

| 앱 | Production Base URL |
|----|---------------------|
| M-BENCH | `http://hudit.cafe24.com:8080/mbench` |
| M-BODY | `http://hudit.cafe24.com:8080/mbody` |
| M-FACE | `http://hudit.cafe24.com:8080/mface` |
| M-SOCIAL | `http://hudit.cafe24.com:8080/msocial` |

### 변경 파일

- `MBench/Assets/MBenchServerConfig.cs`
- `MBody_Revised_Clean/Assets/JsonRequest.cs` + `Scenes/MBody.unity`
- `M-Face_Clean/Assets/MFaceApiClient.cs`
- `M-Social_Clean/Assets/Login.cs`

## Unity 검증 (API 호환 스크립트)

계정 생성 + 로그인 플로우:

| 앱 | 테스트 | 결과 |
|----|--------|------|
| M-BENCH | has → create → login | PASS |
| M-BODY | bootstrap → login + seq | PASS |
| M-FACE | sign-in + seq | PASS |
| M-SOCIAL | sign-in + seq | PASS |

## Unity Editor 실기 테스트

각 프로젝트를 Unity Editor에서 열고 `apiTarget=Remote` 상태로 Play 후 로그인/계정 생성을 확인하세요.

```text
M-BENCH  → MBenchServerConfig.ProductionBaseUrl (Android 빌드 자동 적용)
M-BODY   → JsonRequest.remoteUrl
M-FACE   → MFaceApiClient.remoteUrl (기본 Remote)
M-SOCIAL → Login.BaseUrl
```

## 재배포

```powershell
.\mtalk\scripts\Deploy-Cafe24Vps.ps1
```

## 환경변수

`mtalk/.env.cafe24` (git 제외)
