package com.mtalk.mbody.dto;

import jakarta.validation.constraints.NotBlank;

public record LoginRequest(
        @NotBlank String id,
        @NotBlank String password
) {
}
