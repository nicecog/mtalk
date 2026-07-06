package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.FaceActivityLog;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface MfaceFaceActivityLogRepository extends JpaRepository<FaceActivityLog, Long> {

    List<FaceActivityLog> findTop100ByUserAccountSeqOrderBySeqDesc(Long userAccountSeq);
}
