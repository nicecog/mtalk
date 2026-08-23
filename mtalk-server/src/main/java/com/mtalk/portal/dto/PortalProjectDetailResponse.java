package com.mtalk.portal.dto;

import java.util.List;

public record PortalProjectDetailResponse(
        PortalProjectResponse project,
        List<PortalProjectUpdateResponse> updates
) {
}
