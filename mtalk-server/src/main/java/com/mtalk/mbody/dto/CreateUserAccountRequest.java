package com.mtalk.mbody.dto;

import com.fasterxml.jackson.annotation.JsonAlias;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public record CreateUserAccountRequest(
        @JsonAlias("level") @NotNull Integer levelId,
        @NotBlank String domain,
        @NotBlank String id,
        @NotBlank String password,
        @NotBlank String name,
        @NotBlank String email,
        @NotBlank String phone
) {
}
