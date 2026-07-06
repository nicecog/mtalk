package com.mtalk.mbody.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;

import java.time.LocalDateTime;

@Entity
@Table(name = "subject_videos")
public class SubjectVideo {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long seq;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_account_seq", nullable = false)
    private UserAccount userAccount;

    @Column(name = "file_video", nullable = false, length = 500)
    private String fileVideo;

    @Column(name = "content_point", nullable = false)
    private Integer contentPoint;

    @Column(name = "content_type", nullable = false, length = 100)
    private String contentType;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @PrePersist
    void onCreate() {
        createdAt = LocalDateTime.now();
    }

    public Long getSeq() {
        return seq;
    }

    public UserAccount getUserAccount() {
        return userAccount;
    }

    public void setUserAccount(UserAccount userAccount) {
        this.userAccount = userAccount;
    }

    public String getFileVideo() {
        return fileVideo;
    }

    public void setFileVideo(String fileVideo) {
        this.fileVideo = fileVideo;
    }

    public Integer getContentPoint() {
        return contentPoint;
    }

    public void setContentPoint(Integer contentPoint) {
        this.contentPoint = contentPoint;
    }

    public String getContentType() {
        return contentType;
    }

    public void setContentType(String contentType) {
        this.contentType = contentType;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }
}
