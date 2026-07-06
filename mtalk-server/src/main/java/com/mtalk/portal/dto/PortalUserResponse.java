package com.mtalk.portal.dto;

public record PortalUserResponse(
        long seq,
        String id,
        String name,
        String email,
        String phone,
        int levelId,
        String domain,
        boolean admin,
        String createdAt,
        Integer playTimes,
        Integer bodyPlayTimes,
        String danceStar,
        String lastVisitDate,
        Integer dayCount,
        String itemString
) {
}
