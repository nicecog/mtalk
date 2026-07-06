# M-Social Local API Verification (MBody Shared Server)

> **갱신:** 2026-06-14 — CAFE24 원격 서버 검증 포함  
> Run: 2026-06-13 08:05:05 (로컬) / 2026-06-14 (CAFE24 Pre-Build)  
> Server (로컬): MBody_Revised_server @ http://127.0.0.1:21250  
> Server (운영): http://hudit.cafe24.com:8080/msocial  
> Result: **13 / 13 PASS** (로컬) / **Pre-Build 19/19 PASS** (CAFE24)

## Test Results

| Category | Test | Pass | Detail | Unity Ref |
|----------|------|------|--------|-----------|
| A-connect | server health | OK | HTTP 200 {"status":"ok"} | Login.cs url |
| B-auth | new account has=false | OK | false | Login.AdminLogin L97 |
| C-bootstrap | super-admin sign-in | OK | JSESSIONID=True | Login L114 |
| C-bootstrap | create account (level field) | OK | {"seq":3,"id":"ms_080459","name":"socialuser","email":"ms_080459@gmail.com","phone":"000","levelId":1,"domain":"spike","admin":false} | Login L116 |
| C-bootstrap | bootstrap sign-out | OK | {"message":"logged out"} | Login L131 |
| D-auth | user sign-in + seq | OK | seq=3 | Login.sendPost L121 |
| E-auth | existing account has=true | OK | true | Login existing branch |
| F-sync | list mface-images by account | OK | count=0 | Login.cs L188 |
| G-upload | CSV upload via mface-images | OK | {"seq":3,"user_account_seq":3,"key":"e4af308302f3460caff58c04003843a1","name":"msocial_bf36f3e9f3b040f5bc4b5002b05a1bb2.csv","type":"application/octet-stream","size":50,"inserted":"2026-06-13T08:05:03.892973800"} HTTP_STATUS:200 | Login.cs Upload |
| H-negative | wrong password | OK | HTTP 401 |  |
| H-negative | create without admin | OK | HTTP 403 |  |
| I-auth | user sign-out | OK | {"message":"logged out"} | Login.LogOut |
| J-db | data persist | OK | has=true images=1 |  |

All M-Social flows verified against MBody shared server.
