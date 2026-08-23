package com.mtalk.portal.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record PortalProjectUpsertRequest(
        @NotBlank(message = "프로젝트 이름을 입력해 주세요.")
        @Size(max = 200, message = "프로젝트 이름은 200자 이하여야 합니다.")
        String title,

        @Size(max = 50, message = "분류는 50자 이하여야 합니다.")
        String category,

        @NotBlank(message = "상태를 선택해 주세요.")
        String status,

        @Min(value = 0, message = "진행률은 0 이상이어야 합니다.")
        @Max(value = 100, message = "진행률은 100 이하여야 합니다.")
        int progress,

        @Size(max = 500, message = "요약은 500자 이하여야 합니다.")
        String summary,

        @Size(max = 100, message = "담당자는 100자 이하여야 합니다.")
        String ownerName
) {
}
