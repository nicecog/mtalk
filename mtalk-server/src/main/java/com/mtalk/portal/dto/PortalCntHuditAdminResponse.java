package com.mtalk.portal.dto;

public record PortalCntHuditAdminResponse(
        long seq,
        String loginId,
        String displayName,
        boolean enabled,
        String createdAt,
        String lastLoginAt
) {
}
