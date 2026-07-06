package com.mtalk.config;

import com.zaxxer.hikari.HikariDataSource;
import jakarta.persistence.EntityManagerFactory;
import org.flywaydb.core.Flyway;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.flyway.FlywayMigrationInitializer;
import org.springframework.boot.autoconfigure.jdbc.DataSourceProperties;
import org.springframework.boot.autoconfigure.orm.jpa.JpaProperties;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.orm.jpa.EntityManagerFactoryBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.DependsOn;
import org.springframework.context.annotation.Primary;
import org.springframework.context.annotation.Profile;
import org.springframework.core.env.Environment;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.orm.jpa.JpaTransactionManager;
import org.springframework.orm.jpa.LocalContainerEntityManagerFactoryBean;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.annotation.EnableTransactionManagement;

import javax.sql.DataSource;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;

@Configuration
@EnableTransactionManagement
@EnableJpaRepositories(
        basePackages = {"com.mtalk.mbody.repository", "com.mtalk.portal.repository"},
        entityManagerFactoryRef = "mbodyEntityManagerFactory",
        transactionManagerRef = "mbodyTransactionManager"
)
public class MbodyDataSourceConfig {

    private final Environment environment;

    public MbodyDataSourceConfig(Environment environment) {
        this.environment = environment;
    }

    @Bean
    @Primary
    @ConfigurationProperties("spring.datasource.mbody")
    public DataSourceProperties mbodyDataSourceProperties() {
        return new DataSourceProperties();
    }

    @Bean
    @Primary
    public DataSource mbodyDataSource() {
        return mbodyDataSourceProperties()
                .initializeDataSourceBuilder()
                .type(HikariDataSource.class)
                .build();
    }

    @Bean(initMethod = "migrate")
    @Profile("!local-h2")
    public Flyway mbodyFlyway(@Qualifier("mbodyDataSource") DataSource dataSource) {
        return Flyway.configure()
                .dataSource(dataSource)
                .locations("classpath:db/migration")
                .baselineOnMigrate(true)
                .load();
    }

    @Bean
    @DependsOn("mbodyFlyway")
    @Profile("!local-h2")
    public FlywayMigrationInitializer mbodyFlywayInitializer(Flyway mbodyFlyway) {
        return new FlywayMigrationInitializer(mbodyFlyway, null);
    }

    @Bean
    @Primary
    public LocalContainerEntityManagerFactoryBean mbodyEntityManagerFactory(
            EntityManagerFactoryBuilder builder,
            @Qualifier("mbodyDataSource") DataSource dataSource,
            JpaProperties jpaProperties
    ) {
        Map<String, Object> props = new HashMap<>(jpaProperties.getProperties());
        if (Arrays.asList(environment.getActiveProfiles()).contains("local-h2")) {
            props.put("hibernate.hbm2ddl.auto", "create-drop");
        } else {
            props.put("hibernate.hbm2ddl.auto", "validate");
            props.put("hibernate.dialect", "org.hibernate.dialect.PostgreSQLDialect");
        }
        return builder
                .dataSource(dataSource)
                .packages("com.mtalk.mbody.domain", "com.mtalk.portal.domain")
                .persistenceUnit("mbody")
                .properties(props)
                .build();
    }

    @Bean
    @Primary
    public PlatformTransactionManager mbodyTransactionManager(
            @Qualifier("mbodyEntityManagerFactory") EntityManagerFactory entityManagerFactory
    ) {
        return new JpaTransactionManager(entityManagerFactory);
    }
}
