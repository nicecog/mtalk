package com.mtalk.portal.repository;

import com.mtalk.portal.domain.PortalProject;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface PortalProjectRepository extends JpaRepository<PortalProject, Long> {
    List<PortalProject> findAllByOrderByUpdatedAtDesc();

    List<PortalProject> findByStatusOrderByUpdatedAtDesc(String status);

    List<PortalProject> findByCategoryOrderByUpdatedAtDesc(String category);

    List<PortalProject> findByStatusAndCategoryOrderByUpdatedAtDesc(String status, String category);

    long countByStatus(String status);
}
