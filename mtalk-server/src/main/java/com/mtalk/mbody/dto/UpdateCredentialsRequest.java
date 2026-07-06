package com.mtalk.mbody.dto;

import jakarta.validation.constraints.NotBlank;

public record UpdateCredentialsRequest(
        @NotBlank String id,
        @NotBlank String password
) {
}
