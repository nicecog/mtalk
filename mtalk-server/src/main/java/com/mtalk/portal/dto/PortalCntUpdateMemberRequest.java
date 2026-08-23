package com.mtalk.portal.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record PortalCntUpdateMemberRequest(
        @NotBlank(message = "이름을 입력해 주세요.")
        @Size(max = 100, message = "이름은 100자 이하여야 합니다.")
        String userName,

        @NotBlank(message = "성별을 선택해 주세요.")
        @Pattern(regexp = "[MFmf]", message = "성별은 M 또는 F여야 합니다.")
        String userGender,

        @NotBlank(message = "생년월일을 입력해 주세요.")
        String birthday
) {
}
