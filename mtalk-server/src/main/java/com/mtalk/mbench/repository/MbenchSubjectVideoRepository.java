package com.mtalk.mbench.repository;

import com.mtalk.mbench.domain.SubjectVideo;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface MbenchSubjectVideoRepository extends JpaRepository<SubjectVideo, Long> {
    List<SubjectVideo> findByUserAccountSeqOrderByCreatedAtDesc(Long userAccountSeq);
    void deleteByUserAccountSeq(Long userAccountSeq);
}
