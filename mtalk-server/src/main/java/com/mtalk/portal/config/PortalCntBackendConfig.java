package com.mtalk.portal.config;

import com.mtalk.portal.service.PortalAuthService;
import com.mtalk.portal.service.PortalCntPdfProxyService;
import com.mtalk.portal.service.PortalCntService;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

@Configuration
public class PortalCntBackendConfig {

    @Bean
    @Primary
    public PortalCntService portalCntService(
            @Qualifier("cntJdbcTemplate") NamedParameterJdbcTemplate cntJdbcTemplate,
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.legacy-base-url:http://cnt.re.kr}") String legacyBaseUrl
    ) {
        return new PortalCntService(cntJdbcTemplate, portalAuthService, legacyBaseUrl);
    }

    @Bean("portalCntHuditService")
    public PortalCntService portalCntHuditService(
            @Qualifier("cntHuditJdbcTemplate") NamedParameterJdbcTemplate cntHuditJdbcTemplate,
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.hudit-base-url:http://huditcnt.cafe24.com}") String huditBaseUrl
    ) {
        return new PortalCntService(cntHuditJdbcTemplate, portalAuthService, huditBaseUrl);
    }

    @Bean
    @Primary
    public PortalCntPdfProxyService portalCntPdfProxyService(
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.legacy-base-url:http://cnt.re.kr}") String legacyBaseUrl,
            @Value("${hudit.cnt.pdf-template-suffix:pdf4th}") String pdfTemplateSuffix,
            @Value("${hudit.cnt.pdf-fetch-timeout-ms:45000}") long pdfFetchTimeoutMs
    ) {
        return new PortalCntPdfProxyService(
                portalAuthService,
                legacyBaseUrl,
                pdfTemplateSuffix,
                "/api/hudit/cnt/pdf-assets",
                pdfFetchTimeoutMs
        );
    }

    @Bean("portalCntHuditPdfProxyService")
    public PortalCntPdfProxyService portalCntHuditPdfProxyService(
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.hudit-internal-base-url:http://huditcnt.cafe24.com}") String huditInternalBaseUrl,
            @Value("${hudit.cnt.hudit-pdf-template-suffix:pdf}") String pdfTemplateSuffix,
            @Value("${hudit.cnt.pdf-fetch-timeout-ms:45000}") long pdfFetchTimeoutMs
    ) {
        return new PortalCntPdfProxyService(
                portalAuthService,
                huditInternalBaseUrl,
                pdfTemplateSuffix,
                "/api/hudit/cnt-cafe24/pdf-assets",
                pdfFetchTimeoutMs
        );
    }

    @Bean("cnthuditPdfProxyService")
    public PortalCntPdfProxyService cnthuditPdfProxyService(
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.hudit-internal-base-url:http://huditcnt.cafe24.com}") String huditInternalBaseUrl,
            @Value("${hudit.cnt.hudit-pdf-template-suffix:pdf}") String pdfTemplateSuffix,
            @Value("${hudit.cnt.pdf-fetch-timeout-ms:45000}") long pdfFetchTimeoutMs
    ) {
        return new PortalCntPdfProxyService(
                portalAuthService,
                huditInternalBaseUrl,
                pdfTemplateSuffix,
                "/cnthudit/api/pdf-assets",
                pdfFetchTimeoutMs
        );
    }
}
