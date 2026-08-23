package com.mtalk.portal.dto;

import jakarta.validation.constraints.Size;

public record PortalCntHuditAdminUpdateRequest(
        @Size(max = 100) String displayName,
        Boolean enabled
) {
}
