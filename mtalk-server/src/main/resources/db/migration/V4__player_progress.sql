CREATE TABLE player_progress (
    user_account_seq BIGINT NOT NULL,
    play_times INTEGER NOT NULL DEFAULT 1,
    body_play_times INTEGER NOT NULL DEFAULT 0,
    last_visit_date DATE NOT NULL,
    dance_star VARCHAR(20) NOT NULL DEFAULT 'FFFFFF-FFFFFFF',
    created_at TIMESTAMP(6) NOT NULL,
    updated_at TIMESTAMP(6) NOT NULL,
    PRIMARY KEY (user_account_seq),
    CONSTRAINT fk_player_progress_user_account
        FOREIGN KEY (user_account_seq) REFERENCES user_accounts (seq)
);

CREATE TABLE training_results (
    seq BIGSERIAL PRIMARY KEY,
    user_account_seq BIGINT NOT NULL,
    recorded_at VARCHAR(20) NOT NULL,
    result_type VARCHAR(20) NOT NULL,
    trial VARCHAR(255) NOT NULL,
    point VARCHAR(255) NOT NULL,
    details VARCHAR(500) NOT NULL DEFAULT '',
    created_at TIMESTAMP(6) NOT NULL,
    CONSTRAINT fk_training_results_user_account
        FOREIGN KEY (user_account_seq) REFERENCES user_accounts (seq)
);

CREATE INDEX idx_training_results_user_account_seq ON training_results (user_account_seq);
