CREATE TABLE hudit_projects (
    seq BIGSERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    category VARCHAR(50) NOT NULL DEFAULT '기타',
    status VARCHAR(30) NOT NULL DEFAULT 'planned',
    progress INTEGER NOT NULL DEFAULT 0,
    summary VARCHAR(500) NOT NULL DEFAULT '',
    owner_name VARCHAR(100) NOT NULL DEFAULT '',
    created_by VARCHAR(100) NOT NULL DEFAULT '',
    updated_by VARCHAR(100) NOT NULL DEFAULT '',
    created_at TIMESTAMP(6) NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP(6) NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_hudit_projects_progress CHECK (progress >= 0 AND progress <= 100),
    CONSTRAINT ck_hudit_projects_status CHECK (status IN ('planned', 'in_progress', 'paused', 'done'))
);

CREATE TABLE hudit_project_updates (
    seq BIGSERIAL PRIMARY KEY,
    project_seq BIGINT NOT NULL REFERENCES hudit_projects(seq) ON DELETE CASCADE,
    content TEXT NOT NULL,
    progress INTEGER,
    created_by VARCHAR(100) NOT NULL DEFAULT '',
    created_at TIMESTAMP(6) NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_hudit_project_updates_progress CHECK (progress IS NULL OR (progress >= 0 AND progress <= 100))
);

CREATE INDEX idx_hudit_projects_status ON hudit_projects(status, updated_at DESC);
CREATE INDEX idx_hudit_projects_category ON hudit_projects(category);
CREATE INDEX idx_hudit_project_updates_project ON hudit_project_updates(project_seq, seq DESC);

INSERT INTO hudit_projects (title, category, status, progress, summary, owner_name, created_by, updated_by)
VALUES
    ('MTALK 통합 서버', 'MTALK', 'in_progress', 80, 'M-BENCH / M-BODY / M-FACE / M-SOCIAL을 단일 Spring Boot 서버로 운영하고 HUDIT에서 계정을 관리합니다.', 'HUDIT', 'system', 'system'),
    ('CNT (cnt.re.kr)', 'CNT', 'in_progress', 70, '레거시 CNT DB 조회·센터/회원 관리, PDF 프록시를 HUDIT 포털에 연동합니다.', 'HUDIT', 'system', 'system'),
    ('CNT Cafe24 / Cnthudit', 'CNT', 'in_progress', 85, 'huditcnt.cafe24.com Cnthudit 앱, 입장 티켓, Cafe24 관리자 계정을 HUDIT와 연동합니다.', 'HUDIT', 'system', 'system'),
    ('Unity 클라이언트 연동', 'Unity', 'in_progress', 60, 'M-BENCH, M-BODY, M-FACE, M-SOCIAL 앱이 Cafe24 운영 API를 바라보도록 맞춥니다.', 'HUDIT', 'system', 'system');

INSERT INTO hudit_project_updates (project_seq, content, progress, created_by)
SELECT seq, 'Cafe24 VPS(hudit.cafe24.com)에 Docker로 배포되어 헬스체크가 정상입니다.', 80, 'system'
FROM hudit_projects WHERE title = 'MTALK 통합 서버';

INSERT INTO hudit_project_updates (project_seq, content, progress, created_by)
SELECT seq, '센터 생성/수정과 PDF 일괄 다운로드를 HUDIT CNT 화면에 연결했습니다.', 70, 'system'
FROM hudit_projects WHERE title = 'CNT (cnt.re.kr)';

INSERT INTO hudit_project_updates (project_seq, content, progress, created_by)
SELECT seq, 'Cnthudit 독립 로그인과 HUDIT 입장 티켓, 관리자 계정 화면을 운영 소스에 반영했습니다.', 85, 'system'
FROM hudit_projects WHERE title = 'CNT Cafe24 / Cnthudit';

INSERT INTO hudit_project_updates (project_seq, content, progress, created_by)
SELECT seq, '운영 API 주소는 http://hudit.cafe24.com:8080 기준으로 검증되어 있습니다.', 60, 'system'
FROM hudit_projects WHERE title = 'Unity 클라이언트 연동';
