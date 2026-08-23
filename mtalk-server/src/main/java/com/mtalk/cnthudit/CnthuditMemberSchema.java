package com.mtalk.cnthudit;

import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Component;

@Component
public class CnthuditMemberSchema {

    private static final Logger log = LoggerFactory.getLogger(CnthuditMemberSchema.class);

    private final NamedParameterJdbcTemplate cntHuditJdbcTemplate;

    public CnthuditMemberSchema(
            @Qualifier("cntHuditJdbcTemplate") NamedParameterJdbcTemplate cntHuditJdbcTemplate
    ) {
        this.cntHuditJdbcTemplate = cntHuditJdbcTemplate;
    }

    @PostConstruct
    public void ensureBirthdayColumn() {
        Integer count = cntHuditJdbcTemplate.getJdbcTemplate().queryForObject(
                """
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'cnt_member'
                  AND COLUMN_NAME = 'birthday'
                """,
                Integer.class
        );
        if (count != null && count == 0) {
            cntHuditJdbcTemplate.getJdbcTemplate().execute(
                    "ALTER TABLE cnt_member ADD COLUMN birthday DATE NULL"
            );
            log.info("added cnt_member.birthday");
        }
    }
}
