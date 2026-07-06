package com.mtalk.portal.service;

import com.mtalk.portal.domain.PortalAdmin;
import com.mtalk.portal.repository.PortalAdminRepository;
import jakarta.annotation.PostConstruct;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class PortalDataSeeder {

    private final PortalAdminRepository portalAdminRepository;
    private final PortalAuthService portalAuthService;
    private final String defaultLoginId;
    private final String defaultPassword;

    public PortalDataSeeder(
            PortalAdminRepository portalAdminRepository,
            PortalAuthService portalAuthService,
            @Value("${hudit.portal.default-login-id:hudit}") String defaultLoginId,
            @Value("${hudit.portal.default-password:hudit!}") String defaultPassword
    ) {
        this.portalAdminRepository = portalAdminRepository;
        this.portalAuthService = portalAuthService;
        this.defaultLoginId = defaultLoginId;
        this.defaultPassword = defaultPassword;
    }

    @PostConstruct
    public void seedDefaultAdmin() {
        if (portalAdminRepository.existsByLoginId(defaultLoginId)) {
            return;
        }
        PortalAdmin admin = new PortalAdmin();
        admin.setLoginId(defaultLoginId);
        admin.setPasswordHash(portalAuthService.encodePassword(defaultPassword));
        admin.setDisplayName("HUDIT Administrator");
        portalAdminRepository.save(admin);
    }
}
