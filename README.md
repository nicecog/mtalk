# MTalk — M-Face / M-Body / M-Social

Unity 클라이언트 3종 최신 소스 저장소입니다.

| 디렉터리 | 앱 | 패키지 ID |
|----------|-----|-----------|
| `M-Face_LocalML/` | M-Face (로컬 ML FER+) | `com.Spike.MFace` |
| `MBody_Revised_Clean/` | M-Body | `com.CAU.MBody` |
| `M-Social_Clean/` | M-Social | `com.Spike.MSocial` |

## 이전 단일 M-Body 레이아웃

2026-06-27 이전에 루트에 있던 M-Body 전용 구조는 `backup/` 에 보관되어 있습니다.

## 빌드

각 프로젝트의 `scripts/build-android.ps1` 을 사용합니다.

## 서버

운영 API: `http://hudit.cafe24.com:8080` (`/mface`, `/mbody`, `/msocial`)
