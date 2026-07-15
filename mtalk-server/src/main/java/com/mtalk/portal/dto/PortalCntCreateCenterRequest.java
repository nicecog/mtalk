package com.mtalk.portal.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record PortalCntCreateCenterRequest(
        @NotBlank(message = "센터 ID를 입력해 주세요.")
        @Size(max = 255, message = "센터 ID는 255자 이하여야 합니다.")
        String centerId,

        @Size(max = 255, message = "센터명은 255자 이하여야 합니다.")
        String centerNm,

        @Pattern(regexp = "Y|N", message = "노출 여부는 Y 또는 N이어야 합니다.")
        String openYn
) {
}
