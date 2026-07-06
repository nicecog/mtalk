CREATE TABLE social_progress (
    user_account_seq BIGINT NOT NULL,
    day_count INTEGER NOT NULL DEFAULT 1,
    last_visit_date DATE NOT NULL,
    progress_string VARCHAR(32) NOT NULL DEFAULT 'FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF',
    created_at TIMESTAMP(6) NOT NULL,
    updated_at TIMESTAMP(6) NOT NULL,
    PRIMARY KEY (user_account_seq),
    CONSTRAINT fk_social_progress_user_account
        FOREIGN KEY (user_account_seq) REFERENCES user_accounts (seq)
);

CREATE TABLE social_activity_logs (
    seq BIGSERIAL PRIMARY KEY,
    user_account_seq BIGINT NOT NULL,
    log_date VARCHAR(20) NOT NULL,
    log_time VARCHAR(20) NOT NULL,
    log_body VARCHAR(500) NOT NULL DEFAULT '',
    created_at TIMESTAMP(6) NOT NULL,
    CONSTRAINT fk_social_activity_logs_user_account
        FOREIGN KEY (user_account_seq) REFERENCES user_accounts (seq)
);

CREATE INDEX idx_social_activity_logs_user_account_seq ON social_activity_logs (user_account_seq);
