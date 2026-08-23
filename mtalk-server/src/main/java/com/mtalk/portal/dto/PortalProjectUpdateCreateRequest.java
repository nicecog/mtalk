package com.mtalk.portal.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record PortalProjectUpdateCreateRequest(
        @NotBlank(message = "진행사항을 입력해 주세요.")
        @Size(max = 4000, message = "진행사항은 4000자 이하여야 합니다.")
        String content,

        @Min(value = 0, message = "진행률은 0 이상이어야 합니다.")
        @Max(value = 100, message = "진행률은 100 이하여야 합니다.")
        Integer progress
) {
}
