package com.mtalk.portal.dto;

import jakarta.validation.constraints.NotBlank;

public record PortalCreateUserRequest(
        @NotBlank String id,
        @NotBlank String password,
        String name,
        String email,
        String phone,
        Integer levelId,
        String domain,
        Integer playTimes,
        Integer bodyPlayTimes,
        String danceStar,
        Integer dayCount,
        String itemString
) {
}
