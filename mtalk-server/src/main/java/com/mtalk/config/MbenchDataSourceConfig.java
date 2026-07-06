package com.mtalk.config;

import com.zaxxer.hikari.HikariDataSource;
import jakarta.persistence.EntityManagerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.jdbc.DataSourceProperties;
import org.springframework.boot.autoconfigure.orm.jpa.JpaProperties;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.orm.jpa.EntityManagerFactoryBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.orm.jpa.JpaTransactionManager;
import org.springframework.orm.jpa.LocalContainerEntityManagerFactoryBean;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.annotation.EnableTransactionManagement;

import javax.sql.DataSource;
import java.util.HashMap;
import java.util.Map;

@Configuration
@EnableTransactionManagement
@EnableJpaRepositories(
        basePackages = "com.mtalk.mbench.repository",
        entityManagerFactoryRef = "mbenchEntityManagerFactory",
        transactionManagerRef = "mbenchTransactionManager"
)
public class MbenchDataSourceConfig {

    @Bean
    @ConfigurationProperties("spring.datasource.mbench")
    public DataSourceProperties mbenchDataSourceProperties() {
        return new DataSourceProperties();
    }

    @Bean
    public DataSource mbenchDataSource() {
        return mbenchDataSourceProperties()
                .initializeDataSourceBuilder()
                .type(HikariDataSource.class)
                .build();
    }

    @Bean
    public LocalContainerEntityManagerFactoryBean mbenchEntityManagerFactory(
            EntityManagerFactoryBuilder builder,
            @Qualifier("mbenchDataSource") DataSource dataSource,
            JpaProperties jpaProperties
    ) {
        Map<String, Object> props = new HashMap<>(jpaProperties.getProperties());
        props.put("hibernate.hbm2ddl.auto", "update");
        props.put("hibernate.dialect", "org.hibernate.dialect.PostgreSQLDialect");
        return builder
                .dataSource(dataSource)
                .packages("com.mtalk.mbench.domain")
                .persistenceUnit("mbench")
                .properties(props)
                .build();
    }

    @Bean
    public PlatformTransactionManager mbenchTransactionManager(
            @Qualifier("mbenchEntityManagerFactory") EntityManagerFactory entityManagerFactory
    ) {
        return new JpaTransactionManager(entityManagerFactory);
    }
}
