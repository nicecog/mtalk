package com.mtalk.mbench.domain;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * Unity 클라이언트의 userAccountSeq(seq) — 서버 DB user_accounts.id (AUTO_INCREMENT PK).
 * 피험자(Subject) 계정 1건을 식별하며, subject_videos.user_account_seq FK로 영상 메타와 연결된다.
 */
@Entity
@Table(name = "user_accounts", indexes = @Index(name = "uk_user_accounts_login_id", columnList = "login_id", unique = true))
public class UserAccount {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "id")
    private Long seq;

    @Column(name = "login_id", nullable = false, length = 128)
    private String loginId;

    @Column(name = "password", nullable = false, length = 255)
    private String password;

    @Column(name = "level_id", nullable = false)
    private int levelId = 1;

    @Column(name = "domain", nullable = false, length = 64)
    private String domain = "spike";

    @Column(name = "name", length = 128)
    private String name;

    @Column(name = "email", length = 256)
    private String email;

    @Column(name = "phone", length = 32)
    private String phone;

    @Column(name = "is_admin", nullable = false)
    private boolean admin;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @PrePersist
    void onCreate() {
        if (createdAt == null) {
            createdAt = LocalDateTime.now();
        }
    }

    public Long getSeq() {
        return seq;
    }

    public void setSeq(Long seq) {
        this.seq = seq;
    }

    public String getLoginId() {
        return loginId;
    }

    public void setLoginId(String loginId) {
        this.loginId = loginId;
    }

    public String getPassword() {
        return password;
    }

    public void setPassword(String password) {
        this.password = password;
    }

    public int getLevelId() {
        return levelId;
    }

    public void setLevelId(int levelId) {
        this.levelId = levelId;
    }

    public String getDomain() {
        return domain;
    }

    public void setDomain(String domain) {
        this.domain = domain;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public String getPhone() {
        return phone;
    }

    public void setPhone(String phone) {
        this.phone = phone;
    }

    public boolean isAdmin() {
        return admin;
    }

    public void setAdmin(boolean admin) {
        this.admin = admin;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }
}
