CREATE TABLE user_accounts (
    seq BIGSERIAL PRIMARY KEY,
    level_id INTEGER NOT NULL,
    domain VARCHAR(100) NOT NULL,
    login_id VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    phone VARCHAR(50) NOT NULL,
    admin_account BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP(6) NOT NULL,
    updated_at TIMESTAMP(6) NOT NULL,
    CONSTRAINT uk_user_accounts_login_id UNIQUE (login_id)
);

CREATE TABLE uploaded_files (
    seq BIGSERIAL PRIMARY KEY,
    original_name VARCHAR(255) NOT NULL,
    stored_name VARCHAR(255) NOT NULL,
    relative_path VARCHAR(500) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    size BIGINT NOT NULL,
    created_at TIMESTAMP(6) NOT NULL
);

CREATE TABLE subject_videos (
    seq BIGSERIAL PRIMARY KEY,
    user_account_seq BIGINT NOT NULL,
    file_video VARCHAR(500) NOT NULL,
    content_point INTEGER NOT NULL DEFAULT 0,
    content_type VARCHAR(100) NOT NULL,
    created_at TIMESTAMP(6) NOT NULL,
    CONSTRAINT fk_subject_videos_user_account
        FOREIGN KEY (user_account_seq) REFERENCES user_accounts (seq)
);
