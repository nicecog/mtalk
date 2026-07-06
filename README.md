# MTalk — M-Face / M-Body / M-Social + Unified Server

Unity 클라이언트 3종과 CAFE24 운영 Spring Boot 서버 소스입니다.

| 디렉터리 | 내용 | 패키지/서비스 |
|----------|------|----------------|
| `M-Face_LocalML/` | M-Face (로컬 ML FER+) | `com.Spike.MFace` |
| `MBody_Revised_Clean/` | M-Body | `com.CAU.MBody` |
| `M-Social_Clean/` | M-Social | `com.Spike.MSocial` |
| `mtalk-server/` | 통합 API 서버 (M-BENCH/M-BODY/M-FACE/M-SOCIAL) | `http://hudit.cafe24.com:8080` |

## 이전 단일 M-Body 레이아웃

2026-06-27 이전에 루트에 있던 M-Body 전용 구조는 `backup/` 에 보관되어 있습니다.

## Unity 빌드

각 프로젝트의 `scripts/build-android.ps1` 을 사용합니다.

## 서버 빌드·배포

```powershell
cd mtalk-server
mvn clean package -DskipTests
# 운영: .env.example 참고, .env.cafe24 는 git 제외 → VPS /opt/mtalk/.env
```

API base paths: `/mbench`, `/mbody`, `/mface`, `/msocial`