package com.mtalk.portal.dto;

public record PortalResetPasswordResponse(
        long seq,
        String id,
        String tempPassword
) {
}
