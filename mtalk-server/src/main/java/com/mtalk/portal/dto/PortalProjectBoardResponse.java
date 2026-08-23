package com.mtalk.portal.dto;

import java.util.List;

public record PortalProjectBoardResponse(
        long total,
        long planned,
        long inProgress,
        long paused,
        long done,
        List<PortalProjectResponse> projects
) {
}
