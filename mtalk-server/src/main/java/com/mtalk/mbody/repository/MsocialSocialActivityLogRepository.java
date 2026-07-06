package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.SocialActivityLog;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface MsocialSocialActivityLogRepository extends JpaRepository<SocialActivityLog, Long> {

    List<SocialActivityLog> findTop100ByUserAccountSeqOrderBySeqDesc(Long userAccountSeq);
}
