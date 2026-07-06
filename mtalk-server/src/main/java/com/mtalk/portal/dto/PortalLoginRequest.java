package com.mtalk.portal.dto;

import jakarta.validation.constraints.NotBlank;

public record PortalLoginRequest(
        @NotBlank String loginId,
        @NotBlank String password
) {
}
