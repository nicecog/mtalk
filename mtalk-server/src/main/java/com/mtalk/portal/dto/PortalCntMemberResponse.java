package com.mtalk.portal.dto;

public record PortalCntMemberResponse(
        long no,
        String userId,
        String userName,
        String centerId,
        String centerNm,
        int userAge
) {
}
