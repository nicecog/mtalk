package com.mtalk.portal.dto;

public record PortalUpdateUserRequest(
        String id,
        String password,
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
