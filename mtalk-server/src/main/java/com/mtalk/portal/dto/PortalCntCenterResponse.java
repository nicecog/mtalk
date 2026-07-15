package com.mtalk.portal.dto;

public record PortalCntCenterResponse(
        long no,
        String centerId,
        String centerNm,
        String openYn,
        String maxSession,
        String sessionLabel,
        String auditDt
) {
}
