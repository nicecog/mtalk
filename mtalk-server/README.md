# MTalk Unified Server



M-BENCH, M-BODY, M-FACE, M-SOCIAL 4개 서비스를 **단일 컨테이너·단일 주소**로 통합한 Spring Boot 서버입니다.



## URL 구조



| 서비스 | Base Path | 헬스체크 | 예시 API |

|--------|-----------|----------|----------|

| **루트** | `/` | `GET /health` | 서비스 목록 |

| **M-BENCH** | `/mbench` | `GET /mbench/health` | `POST /mbench/api/users/login` |

| **M-BODY** | `/mbody` | `GET /mbody/api/health` | `POST /mbody/api/users/login` |

| **M-FACE** | `/mface` | `GET /mface/api/health` | `POST /mface/api/users/sign-in` |

| **M-SOCIAL** | `/msocial` | `GET /msocial/api/health` | `POST /msocial/api/users/sign-in` |



### 관리 UI



- M-BENCH: `http://host:8080/mbench/admin/`

- M-BODY: `http://host:8080/mbody/admin/`



## 기술 스택



- JDK 21

- Spring Boot 3.4.5

- **PostgreSQL 18.4** (운영 / CAFE24 VPS)

- H2 (로컬 스모크 테스트, `local-h2`)

- Flyway (mbody DB 마이그레이션)



## 로컬 실행



### H2 (빠른 스모크 테스트)



```powershell

$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-21.0.11.10-hotspot"

cd mtalk

mvn clean package -DskipTests

java -jar target/mtalk-server-1.0.0.jar --spring.profiles.active=local-h2

```



### PostgreSQL (Docker, API 전체 검증)



```powershell

# PostgreSQL 18.4 컨테이너 (호스트 포트 5434)

docker compose up -d postgres



java -jar target/mtalk-server-1.0.0.jar --spring.profiles.active=local-pg

```



> Windows에 로컬 PostgreSQL이 5432/5433을 사용 중이면 Docker는 **5434**로 매핑됩니다.  

> CAFE24 VPS에서는 PostgreSQL이 `localhost:5432`로 동작하므로 `cafe24` 프로파일을 사용합니다.



## API 검증



```powershell

# 기본 10항목

.\scripts\Verify-MTalkApi.ps1 -BaseUrl "http://127.0.0.1:8080"



# 전체 31항목 (PostgreSQL + 4서비스 + DB persist)

.\scripts\Verify-MTalkApi-Full.ps1 -BaseUrl "http://127.0.0.1:8080"

```



최신 결과: `docs/MTALK_API_VERIFICATION_LATEST.md`



## CAFE24 VPS 배포



```bash

# 1) PostgreSQL 18.4 설치 후 DB 생성

#    CREATE DATABASE mbench;

#    CREATE DATABASE mbody_revised;



# 2) JAR 배포 후 환경변수 설정

export MBENCH_DB_USER=mtalk

export MBENCH_DB_PASSWORD='<비밀번호>'

export MBODY_DB_USER=mtalk

export MBODY_DB_PASSWORD='<비밀번호>'

export MBENCH_ADMIN_PASSWORD='<관리자 비밀번호>'



# 3) 실행

chmod +x start-cafe24.sh

./start-cafe24.sh

```



프로파일: `cafe24` (PostgreSQL localhost:5432 기본)



## Docker 배포 (전체 스택)



```bash

mvn clean package -DskipTests

docker compose up -d --build

```



- 앱: `http://localhost:8080`

- PostgreSQL: 컨테이너 내부 `postgres:5432` (DB: `mbench`, `mbody_revised`)



### 환경변수



| 변수 | 설명 | 기본값 |

|------|------|--------|

| `SERVER_PORT` | 서버 포트 | 8080 |

| `MBENCH_DB_HOST` | M-BENCH DB 호스트 | localhost |

| `MBENCH_DB_PORT` | M-BENCH DB 포트 | 5432 |

| `MBENCH_DB_NAME` | M-BENCH DB명 | mbench |

| `MBODY_DB_HOST` | M-BODY DB 호스트 | localhost |

| `MBODY_DB_PORT` | M-BODY DB 포트 | 5432 |

| `MBODY_DB_NAME` | M-BODY DB명 | mbody_revised |

| `MBENCH_ADMIN_PASSWORD` | M-BENCH 관리자 비밀번호 | (cafe24: 필수) |



## Unity 클라이언트 URL 변경 예시



```

# MTalk 통합 (단일 도메인)

https://your-vps.example.com/mbench/api/users/login

https://your-vps.example.com/mbody/api/users/login

https://your-vps.example.com/mface/api/users/sign-in

https://your-vps.example.com/msocial/api/users/sign-in

```



## 프로젝트 구조



```

mtalk/

├── src/main/java/com/mtalk/

│   ├── MtalkApplication.java

│   ├── config/                    # 멀티 DataSource (PostgreSQL)

│   ├── mbench/                    # M-BENCH 모듈

│   ├── mbody/                     # M-BODY 백엔드 (공유 서비스)

│   ├── mface/                     # M-FACE 라우팅 (/mface)

│   └── msocial/                   # M-SOCIAL 라우팅 (/msocial)

├── src/main/resources/db/migration/  # Flyway (PostgreSQL)

├── docker-compose.yml             # postgres:18.4-alpine + mtalk

├── Dockerfile

├── start-cafe24.sh

└── scripts/

    ├── Verify-MTalkApi.ps1

    └── Verify-MTalkApi-Full.ps1

```



## DB 구성



- **mbench** DB: M-BENCH 전용 (`user_accounts`, `subject_videos`, `uploaded_files`)

- **mbody_revised** DB: M-BODY/M-FACE/M-SOCIAL 공유 (`user_accounts`, `mface_images`, `subject_videos`, ...)



두 DB는 동일 PostgreSQL 인스턴스에서 분리 운영됩니다.

## Git / 버전 관리

`mtalk-server`는 상위 저장소 [`nicecog/mtalk`](https://github.com/nicecog/mtalk)의 `mtalk-server/` 경로로 관리됩니다.

```powershell
cd C:\project\mtalk
git status mtalk-server/
```

| 항목 | 설명 |
|------|------|
| 추적 대상 | `src/`, `pom.xml`, `Dockerfile`, `docker-compose.prod.yml`, `deploy/`, `scripts/`, `docs/` |
| 제외 (`.gitignore`) | `target/`, `.env`, `.env.*`, `data/`, `uploads/`, `*.log` |
| 환경변수 템플릿 | `.env.example` → 복사 후 `.env.cafe24`, `.env.huditcnt-vps` 등으로 사용 |

민감 정보(비밀번호, JWT 시크릿, SSH 키)는 **절대 커밋하지 마세요**.

