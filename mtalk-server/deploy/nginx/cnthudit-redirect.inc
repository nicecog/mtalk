# Inserted on HUDIT VPS HTTPS/HTTP server blocks during deploy.
# Redirects the old path-based admin UI to huditcnt; APK /cnt stays on this host only via mtalk.

location ^~ /cnthudit {
    return 301 https://huditcnt.cafe24.com$request_uri;
}
