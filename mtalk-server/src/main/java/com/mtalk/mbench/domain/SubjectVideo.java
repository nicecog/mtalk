package com.mtalk.mbench.domain;

import jakarta.persistence.*;
import java.time.LocalDateTime;

@Entity
@Table(name = "subject_videos", indexes = @Index(name = "idx_subject_videos_account", columnList = "user_account_seq"))
public class SubjectVideo {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "user_account_seq", nullable = false)
    private Long userAccountSeq;

    @Column(name = "file_video", nullable = false, length = 64)
    private String fileVideo;

    @Column(name = "content_point", nullable = false)
    private int contentPoint = 0;

    @Column(name = "content_type", nullable = false, length = 64)
    private String contentType;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @PrePersist
    void onCreate() {
        if (createdAt == null) {
            createdAt = LocalDateTime.now();
        }
    }

    public Long getId() {
        return id;
    }

    public Long getUserAccountSeq() {
        return userAccountSeq;
    }

    public void setUserAccountSeq(Long userAccountSeq) {
        this.userAccountSeq = userAccountSeq;
    }

    public String getFileVideo() {
        return fileVideo;
    }

    public void setFileVideo(String fileVideo) {
        this.fileVideo = fileVideo;
    }

    public int getContentPoint() {
        return contentPoint;
    }

    public void setContentPoint(int contentPoint) {
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
