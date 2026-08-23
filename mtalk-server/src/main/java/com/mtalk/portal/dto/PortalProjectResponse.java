package com.mtalk.portal.dto;

public record PortalProjectResponse(
        long seq,
        String title,
        String category,
        String status,
        String statusLabel,
        int progress,
        String summary,
        String ownerName,
        String latestUpdate,
        long updateCount,
        String createdBy,
        String updatedBy,
        String createdAt,
        String updatedAt
) {
}
