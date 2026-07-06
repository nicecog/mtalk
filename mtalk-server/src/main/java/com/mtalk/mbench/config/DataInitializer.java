package com.mtalk.mbench.config;

import com.mtalk.mbench.service.UserService;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class DataInitializer {

    @Bean
    CommandLineRunner seedAdmin(UserService userService) {
        return args -> userService.ensureDefaultAdmin();
    }
}
