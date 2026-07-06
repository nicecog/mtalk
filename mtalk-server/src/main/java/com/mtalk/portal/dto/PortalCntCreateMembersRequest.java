package com.mtalk.portal.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;

public record PortalCntCreateMembersRequest(
        @NotNull @Min(1) @Max(500) Integer count
) {
}
