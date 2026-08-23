package com.mtalk.portal.dto;

import java.util.List;

public record PortalCntMemberAnalysisResponse(
        PortalCntMemberDetail member,
        List<PortalCntAnalysisSection> sections
) {
    public record PortalCntMemberDetail(
            long no,
            String userId,
            String userName,
            int userAge,
            String userGender,
            String birthday,
            String centerId,
            String centerNm
    ) {
    }

    public record PortalCntAnalysisSection(
            String title,
            List<String> headers,
            List<List<String>> rows
    ) {
    }
}
