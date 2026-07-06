package com.mtalk.portal.dto;

public record PortalCntCreateMembersResponse(
        int createdCount,
        String centerId,
        String firstUserId,
        String lastUserId
) {
}
