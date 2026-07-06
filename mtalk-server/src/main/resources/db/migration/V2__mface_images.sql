CREATE TABLE mface_images (
    seq BIGSERIAL PRIMARY KEY,
    user_account_seq BIGINT NOT NULL,
    file_key VARCHAR(64) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    relative_path VARCHAR(500) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    size BIGINT NOT NULL,
    created_at TIMESTAMP(6) NOT NULL,
    CONSTRAINT uk_mface_images_file_key UNIQUE (file_key),
    CONSTRAINT fk_mface_images_user_account
        FOREIGN KEY (user_account_seq) REFERENCES user_accounts (seq)
);
