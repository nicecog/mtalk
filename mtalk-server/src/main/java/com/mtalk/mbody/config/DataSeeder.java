package com.mtalk.mbody.config;

import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Component;

@Component
public class DataSeeder implements CommandLineRunner {

    private final MbodyUserAccountRepository MbodyUserAccountRepository;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    public DataSeeder(MbodyUserAccountRepository MbodyUserAccountRepository) {
        this.MbodyUserAccountRepository = MbodyUserAccountRepository;
    }

    @Override
    public void run(String... args) {
        if (MbodyUserAccountRepository.existsByLoginId("super-admin")) {
            return;
        }

        UserAccount admin = new UserAccount();
        admin.setLevelId(10);
        admin.setDomain("spike");
        admin.setLoginId("super-admin");
        admin.setPasswordHash(passwordEncoder.encode("super-admin!@#$"));
        admin.setName("Super Admin");
        admin.setEmail("super-admin@local");
        admin.setPhone("000-0000-0000");
        admin.setAdminAccount(true);
        MbodyUserAccountRepository.save(admin);
    }
}
