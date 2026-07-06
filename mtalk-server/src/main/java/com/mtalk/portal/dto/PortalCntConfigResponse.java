package com.mtalk.portal.dto;

public record PortalCntConfigResponse(
        String legacyBaseUrl,
        String createCenterUrl,
        String createMemberUrl,
        String resultUrl,
        String interpretationUrl
) {
}
