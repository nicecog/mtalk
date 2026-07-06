package com.mtalk.mbench.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.nio.file.Path;

@Configuration
public class AppConfig {

    @Bean
    Path uploadRoot(@Value("${mbench.upload-dir}") String uploadDir) {
        Path path = Path.of(uploadDir).toAbsolutePath().normalize();
        path.toFile().mkdirs();
        return path;
    }
}
