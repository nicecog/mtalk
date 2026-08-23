package com.mtalk.portal.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record PortalCntHuditAdminCreateRequest(
        @NotBlank @Size(min = 2, max = 100) String loginId,
        @NotBlank @Size(min = 4, max = 100) String password,
        @Size(max = 100) String displayName
) {
}
