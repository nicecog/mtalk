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
@Table(name = "face_progress")
public class FaceProgress {

    public static final String DEFAULT_ITEM_STRING = "FFFFFFFFFFFFFFFF";

    @Id
    @Column(name = "user_account_seq")
    private Long userAccountSeq;

    @Column(name = "day_count", nullable = false)
    private int dayCount = 1;

    @Column(name = "last_visit_date", nullable = false)
    private LocalDate lastVisitDate;

    @Column(name = "item_string", nullable = false, length = 16)
    private String itemString = DEFAULT_ITEM_STRING;

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
        itemString = normalizeItemString(itemString);
    }

    @PreUpdate
    void onUpdate() {
        updatedAt = LocalDateTime.now();
        itemString = normalizeItemString(itemString);
    }

    public static String normalizeItemString(String value) {
        if (value == null || value.isBlank()) {
            return DEFAULT_ITEM_STRING;
        }
        String trimmed = value.trim().toUpperCase();
        if (trimmed.length() < 16) {
            return DEFAULT_ITEM_STRING;
        }
        return trimmed.substring(0, 16);
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

    public String getItemString() {
        return itemString;
    }

    public void setItemString(String itemString) {
        this.itemString = itemString;
    }
}
