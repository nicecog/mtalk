package com.mtalk.portal.dto;

public record PortalProjectUpdateResponse(
        long seq,
        String content,
        Integer progress,
        String createdBy,
        String createdAt
) {
}
