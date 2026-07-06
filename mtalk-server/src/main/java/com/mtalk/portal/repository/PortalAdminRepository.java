package com.mtalk.portal.repository;

import com.mtalk.portal.domain.PortalAdmin;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface PortalAdminRepository extends JpaRepository<PortalAdmin, Long> {
    Optional<PortalAdmin> findByLoginId(String loginId);
    boolean existsByLoginId(String loginId);
}
