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
@Table(name = "player_progress")
public class PlayerProgress {

    public static final String DEFAULT_DANCE_STAR = "FFFFFF-FFFFFFF";

    @Id
    @Column(name = "user_account_seq")
    private Long userAccountSeq;

    @Column(name = "play_times", nullable = false)
    private int playTimes = 1;

    @Column(name = "body_play_times", nullable = false)
    private int bodyPlayTimes = 0;

    @Column(name = "last_visit_date", nullable = false)
    private LocalDate lastVisitDate;

    @Column(name = "dance_star", nullable = false, length = 20)
    private String danceStar = DEFAULT_DANCE_STAR;

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
        danceStar = normalizeDanceStar(danceStar);
    }

    @PreUpdate
    void onUpdate() {
        updatedAt = LocalDateTime.now();
        danceStar = normalizeDanceStar(danceStar);
    }

    public static String normalizeDanceStar(String value) {
        if (value == null || value.isBlank()) {
            return DEFAULT_DANCE_STAR;
        }
        String trimmed = value.trim();
        if (trimmed.equals("0")) {
            return DEFAULT_DANCE_STAR;
        }
        if (trimmed.length() < 6) {
            return DEFAULT_DANCE_STAR;
        }
        if (trimmed.length() == 6) {
            return trimmed + "-FFFFFFF";
        }
        return trimmed;
    }

    public Long getUserAccountSeq() {
        return userAccountSeq;
    }

    public void setUserAccountSeq(Long userAccountSeq) {
        this.userAccountSeq = userAccountSeq;
    }

    public int getPlayTimes() {
        return playTimes;
    }

    public void setPlayTimes(int playTimes) {
        this.playTimes = playTimes;
    }

    public int getBodyPlayTimes() {
        return bodyPlayTimes;
    }

    public void setBodyPlayTimes(int bodyPlayTimes) {
        this.bodyPlayTimes = bodyPlayTimes;
    }

    public LocalDate getLastVisitDate() {
        return lastVisitDate;
    }

    public void setLastVisitDate(LocalDate lastVisitDate) {
        this.lastVisitDate = lastVisitDate;
    }

    public String getDanceStar() {
        return danceStar;
    }

    public void setDanceStar(String danceStar) {
        this.danceStar = danceStar;
    }
}
