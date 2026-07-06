package com.mtalk.mbody.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import jakarta.persistence.PreUpdate;
import jakarta.persistence.Table;

import java.time.LocalDate;
import java.time.LocalDateTime;

@Entity
@Table(name = "social_progress")
public class SocialProgress {

    public static final String DEFAULT_PROGRESS_STRING = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";

    @Id
    @Column(name = "user_account_seq")
    private Long userAccountSeq;

    @Column(name = "day_count", nullable = false)
    private int dayCount = 1;

    @Column(name = "last_visit_date", nullable = false)
    private LocalDate lastVisitDate;

    @Column(name = "progress_string", nullable = false, length = 32)
    private String progressString = DEFAULT_PROGRESS_STRING;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;

    @PrePersist
    void onCreate() {
        LocalDateTime now = LocalDateTime.now();
        createdAt = now;
        updatedAt = now;
        if (lastVisitDate == null) {
            lastVisitDate = LocalDate.now();
        }
        progressString = normalizeProgressString(progressString);
    }

    @PreUpdate
    void onUpdate() {
        updatedAt = LocalDateTime.now();
        progressString = normalizeProgressString(progressString);
    }

    public static String normalizeProgressString(String value) {
        if (value == null || value.isBlank()) {
            return DEFAULT_PROGRESS_STRING;
        }
        String trimmed = value.trim().toUpperCase();
        if (trimmed.length() < 32) {
            return DEFAULT_PROGRESS_STRING;
        }
        return trimmed.substring(0, 32);
    }

    public Long getUserAccountSeq() {
        return userAccountSeq;
    }

    public void setUserAccountSeq(Long userAccountSeq) {
        this.userAccountSeq = userAccountSeq;
    }

    public int getDayCount() {
        return dayCount;
    }

    public void setDayCount(int dayCount) {
        this.dayCount = dayCount;
    }

    public LocalDate getLastVisitDate() {
        return lastVisitDate;
    }

    public void setLastVisitDate(LocalDate lastVisitDate) {
        this.lastVisitDate = lastVisitDate;
    }

    public String getProgressString() {
        return progressString;
    }

    public void setProgressString(String progressString) {
        this.progressString = progressString;
    }
}
