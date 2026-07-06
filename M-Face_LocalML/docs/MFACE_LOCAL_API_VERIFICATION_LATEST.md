# M-Face Local API Verification (MBody Shared Server)

> Run: 2026-06-13 08:05:12
> Server: MBody_Revised_server @ http://127.0.0.1:21250
> Result: **22 / 22 PASS**

## Unity API Coverage

| Unity Class | API | Flow |
|-------------|-----|------|
| Login | GET /api/users/accounts/id/has/{id} | AdminLogin |
| Login | POST /api/users/sign-in | bootstrap + user login |
| Login | POST /api/users/accounts | new account (level field) |
| Login | GET /api/users/sign-out | logout |
| Login | GET/POST/DELETE /api/mface-images | CSV sync, log, itemUpdate |
| WebCam | POST /api/mface-images | Stage1 JPG + emotion |
| WebCamStage2 | POST /api/mface-images | Stage2 JPG + user seq |

## Test Results

| Category | Test | Pass | Detail | Unity Ref |
|----------|------|------|--------|-----------|
| A-connect | server health | OK | HTTP 200 {"status":"ok"} | MFaceApiClient.GetBaseUrl |
| B-auth | new account has=false | OK | false | Login.AdminLogin L73 |
| C-bootstrap | super-admin sign-in | OK | JSESSIONID=True | Login L84 sign-in alias |
| C-bootstrap | create account (level field) | OK | {"seq":4,"id":"mf080501","name":"mfaceuser","email":"mf080501@gmail.com","phone":"000","levelId":1,"domain":"spike","admin":false} | Login L86 level alias |
| C-bootstrap | bootstrap sign-out | OK | {"message":"logged out"} | Login.LogOut sign-out alias |
| D-auth | user sign-in + seq parse | OK | seq=4 | Login.sendPost L100-118 |
| E-auth | existing account has=true | OK | true | Login existing branch |
| F-csv | initial FaceData.csv upload | OK | {"seq":1,"user_account_seq":4,"key":"724de672f4164b0bbd779fda5b9edf85","name":"mf080501_FaceData.csv","type":"application/octet-stream","size":74,"inserted":"2026-06-13T08:05:02.594600600"} | Login.Upload L141-157 |
| G-list | list by account + Unity parse | OK | key=724de672f4164b0bbd779fda5b9edf85 seq=1 | Login.fileUpdate L172-189 |
| H-download | download CSV by key | OK | bytes=74 | Login.fileUpdate L192 |
| I-log | log append + reupload + delete old | OK | deleted seq=1 | Login.log L251-295 |
| J-item | itemUpdate ItemString | OK | ItemString=TTTTTTTTTTTTTTTT newKey=cf3d352a4f614309b9f91f56582515ad | Login.itemUpdate L303-351 |
| K-stage1 | JPG Happy emotion classify | OK | parsed=0 expected=0 body={"seq":5,"user_account_seq":4,"key":"f1903ff0016c4d7baef6a75e6dfce6cc","name":"mf080501_Stage1_test_Happy.jpg","type":"image/jpeg","size":4,"inserted":"2026-06-13T08:05:04.566619200","emotion":"Neutral:0.05,Happy:0.82,Sad:0.05,Surprise:0.02,Fear:0.02,Disgust:0.02,Anger:0.05"} | WebCam.Upload L234-287 |
| K-stage1 | JPG Neutral emotion classify | OK | parsed=1 expected=1 body={"seq":6,"user_account_seq":4,"key":"6b3c815ba5e84fc4bc2b3bd282434474","name":"mf080501_Stage1_test_Neutral.jpg","type":"image/jpeg","size":4,"inserted":"2026-06-13T08:05:05.090501300","emotion":"Neutral:0.82,Happy:0.05,Sad:0.05,Surprise:0.02,Fear:0.02,Disgust:0.02,Anger:0.05"} | WebCam.Upload L234-287 |
| K-stage1 | JPG Sad emotion classify | OK | parsed=2 expected=2 body={"seq":7,"user_account_seq":4,"key":"07315b432f32409aa03da247f242b0fd","name":"mf080501_Stage1_test_Sad.jpg","type":"image/jpeg","size":4,"inserted":"2026-06-13T08:05:05.274702500","emotion":"Neutral:0.05,Happy:0.05,Sad:0.82,Surprise:0.02,Fear:0.02,Disgust:0.02,Anger:0.05"} | WebCam.Upload L234-287 |
| K-stage1 | JPG Anger emotion classify | OK | parsed=3 expected=3 body={"seq":8,"user_account_seq":4,"key":"a3393cb845b3438580a44b690454a32b","name":"mf080501_Stage1_test_Anger.jpg","type":"image/jpeg","size":4,"inserted":"2026-06-13T08:05:05.453330100","emotion":"Neutral:0.05,Happy:0.05,Sad:0.05,Surprise:0.02,Fear:0.02,Disgust:0.02,Anger:0.82"} | WebCam.Upload L234-287 |
| L-stage2 | Stage2 upload user=seq | OK | user_account_seq=4 | WebCamStage2.Upload L251 |
| M-auth | user sign-out | OK | {"message":"logged out"} | Login.LogOut |
| N-negative | wrong password | OK | HTTP 401 |  |
| N-negative | create without admin | OK | HTTP 403 |  |
| O-shared | MBody /login alias on same server | OK | shared auth service | JsonRequest MBody |
| P-db | data persist (accounts + mface_images) | OK | api-fallback images=6 (H2 or no docker mysql) |  |

All M-Face flows verified against MBody local shared server.
