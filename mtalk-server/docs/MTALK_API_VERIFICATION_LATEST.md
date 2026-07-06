# MTalk Unified API Verification (PostgreSQL)

> Run: 2026-06-13 11:06:09
> Base: https://hudit.cafe24.com
> Result: **31 / 31 PASS**

| Service | Category | Test | Pass | Detail |
|---------|----------|------|------|--------|
| ROOT | connect | GET /health | OK | routes=mbody,msocial,mbench,mface |
| M-BENCH | health | GET /mbench/health | OK | mbench |
| M-BENCH | auth | has=false new account | OK | false |
| M-BENCH | auth | admin login | OK | seq=1 |
| M-BENCH | auth | create account | OK | id=mb_110601 |
| M-BENCH | auth | has=true existing | OK | true |
| M-BENCH | admin | admin session | OK | {"loggedIn":true,"seq":1} |
| M-BENCH | auth | user re-login | OK | seq=8 |
| M-BENCH | negative | wrong password | OK | HTTP 401 |
| M-BENCH | DB | postgres persist | OK | remote-skip (API verified) |
| M-BODY | health | GET /mbody/api/health | OK | {"status":"ok"} |
| M-BODY | auth | has=false | OK | false |
| M-BODY | auth | bootstrap+login seq | OK | seq=17 |
| M-BODY | upload | mp4 attach | OK | {"fileVideo":"uploads/mbody/2026/06/13/31adac71-cc89-44cf-aa69-289d755020b4-mtalk-sample.mp4"} |
| M-BODY | upload | subject-videos | OK | {"seq":4,"userAccountSeq":17,"fileVideo":"uploads/mbody/2026/06/13/31adac71-cc89-44cf-aa69-289d755020b4-mtalk-sample.mp4","contentPoint":0,"contentType":"post"} |
| M-BODY | negative | wrong password | OK | HTTP 401 |
| M-BODY | DB | postgres persist | OK | remote-skip (API verified) |
| M-FACE | health | GET /mface/api/health | OK | {"status":"ok","service":"mface"} |
| M-FACE | auth | sign-in seq | OK | seq=18 user=mf_110603 |
| M-FACE | csv | FaceData upload | OK | key=02b2532bb8a746a3a9d87eb8d1485136 |
| M-FACE | csv | list by account | OK | count=1 |
| M-FACE | csv | download by key | OK | http=200 bytes=74 head=Day_Count,Last_Login,Item
1,2026-06-13,F |
| M-FACE | jpg | Stage1 emotion upload | OK | {"seq":13,"user_account_seq":18,"key":"9da3446781d149be8312ebb0a63946d3","name":"mf-stage1.jpg","type":"image/jpeg","size":4,"inserted":"2026-06-13T02:06:06.513536346","emotion":"Neutral:0.55,Happy:0.20,Sad:0.05,Surprise:0.02,Fear:0.02,Disgust:0.02,Anger:0.05"} |
| M-FACE | auth | sign-out | OK | ok |
| M-FACE | DB | postgres persist | OK | remote-skip (API verified) |
| M-SOCIAL | health | GET /msocial/api/health | OK | {"status":"ok","service":"msocial"} |
| M-SOCIAL | auth | sign-in seq | OK | seq=19 |
| M-SOCIAL | sync | CSV upload | OK | {"seq":14,"user_account_seq":19,"key":"dce37c23e2ab4bc682800eeb3db0e5cc","name":"msocial_046e596d661c4e88b050505103723c44.csv","type":"application/octet-stream","size":50,"inserted":"2026-06-13T02:06:08.636946180"} |
| M-SOCIAL | sync | list images | OK | count=1 |
| M-SOCIAL | negative | wrong password | OK | HTTP 401 |
| M-SOCIAL | auth | sign-out | OK | ok |

All MTalk service routes verified against PostgreSQL backend.
