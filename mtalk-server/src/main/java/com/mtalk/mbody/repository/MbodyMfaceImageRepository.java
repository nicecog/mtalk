package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.MfaceImage;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface MbodyMfaceImageRepository extends JpaRepository<MfaceImage, Long> {
    List<MfaceImage> findByUserAccountSeqOrderBySeqAsc(Long userAccountSeq);

    Optional<MfaceImage> findByFileKey(String fileKey);
}
