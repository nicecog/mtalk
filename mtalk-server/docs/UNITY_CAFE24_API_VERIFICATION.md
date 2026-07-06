# Unity CAFE24 API Verification

> 2026-06-14 08:3634
> Base: http://hudit.cafe24.com:8080
> Result: **6 / 6 PASS**

| App | Test | Pass | Detail |
|-----|------|------|--------|
| M-BENCH | has=false | OK | false |
| M-BENCH | create account | OK | {"success":true,"seq":19,"id":"unity_mb_083634","message":"login ok"} |
| M-BENCH | user login | OK | {"success":true,"seq":19,"id":"unity_mb_083634","message":"login ok"} |
| M-BODY | login+seq | OK | {"seq":20,"id":"s_unity_083635","name":"u","email":"u@gmail.com","phone":"000","levelId":10,"domain":"spike","admin":false} |
| M-FACE | sign-in+seq | OK | {"seq":21,"id":"mf_unity_083636","name":"f","email":"f@gmail.com","phone":"000","levelId":1,"domain":"spike","admin":false} |
| M-SOCIAL | sign-in+seq | OK | {"seq":22,"id":"ms_unity_083636","name":"s","email":"s@gmail.com","phone":"000","levelId":1,"domain":"spike","admin":false} |
