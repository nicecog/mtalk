package com.mtalk.portal.dto;

import java.util.List;

public record PortalCntPageResponse<T>(
        List<T> items,
        long total,
        int page,
        int size,
        int totalPages
) {
}
