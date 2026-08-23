package com.mtalk.portal.repository;

import com.mtalk.portal.domain.PortalProjectUpdate;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface PortalProjectUpdateRepository extends JpaRepository<PortalProjectUpdate, Long> {
    List<PortalProjectUpdate> findByProjectSeqOrderBySeqDesc(Long projectSeq);

    long countByProjectSeq(Long projectSeq);

    Optional<PortalProjectUpdate> findFirstByProjectSeqOrderBySeqDesc(Long projectSeq);

    void deleteByProjectSeq(Long projectSeq);
}
