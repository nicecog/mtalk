package com.mtalk.cnthudit.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CnthuditChangePasswordRequest(
        @NotBlank String oldPassword,
        @NotBlank @Size(min = 4, max = 100) String newPassword
) {
}
