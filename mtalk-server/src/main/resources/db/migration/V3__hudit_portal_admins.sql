CREATE TABLE hudit_portal_admins (
    seq BIGSERIAL PRIMARY KEY,
    login_id VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100) NOT NULL DEFAULT 'Administrator',
    created_at TIMESTAMP(6) NOT NULL DEFAULT NOW(),
    CONSTRAINT uk_hudit_portal_admins_login_id UNIQUE (login_id)
);
