package com.mtalk.portal.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record PortalCntUpdateCenterRequest(
        @NotBlank(message = "센터 명을 입력해 주세요.")
        @Size(max = 200, message = "센터 명은 200자 이하여야 합니다.")
        String centerNm,

        @Pattern(regexp = "Y|N", message = "노출 여부는 Y 또는 N이어야 합니다.")
        String openYn,

        @Pattern(regexp = "[0-3]", message = "검사 대상 구분은 0부터 3 사이여야 합니다.")
        String maxSession
) {
}
