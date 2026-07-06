#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class Cafe24ApiVerification
{
    [MenuItem("MTalk/Verify CAFE24 API (M-SOCIAL)")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        const string baseUrl = "http://hudit.cafe24.com:8080/msocial";
        try
        {
            var id = "ue_ms_" + DateTime.Now.ToString("HHmmss");
            Log("=== Unity Editor API Verify (M-SOCIAL) ===");
            Log("Base: " + baseUrl);

            AssertOk("health", Get(baseUrl + "/api/health"));
            AssertContains("has=false", Get(baseUrl + "/api/users/accounts/id/has/" + id), "false");
            AssertOk("admin sign-in", Post(baseUrl + "/api/users/sign-in", "{\"id\":\"super-admin\",\"password\":\"super-admin!@#$\"}"));
            AssertOk("create account", Post(baseUrl + "/api/users/accounts",
                "{\"level\":1,\"domain\":\"spike\",\"id\":\"" + id + "\",\"password\":\"pw1234\",\"name\":\"s\",\"email\":\"s@gmail.com\",\"phone\":\"000\"}"));
            AssertOk("sign-out", Get(baseUrl + "/api/users/sign-out"));
            var loginResp = Post(baseUrl + "/api/users/sign-in", "{\"id\":\"" + id + "\",\"password\":\"pw1234\"}");
            AssertOk("user sign-in", loginResp);
            AssertContains("seq in login", loginResp, "\"seq\"");

            Log("RESULT: ALL PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("CAFE24 VERIFY FAIL: " + ex.Message);
            EditorApplication.Exit(1);
        }
    }

    static string Get(string url)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new BypassCertificate();
            req.SendWebRequest();
            while (!req.isDone) { }
            if (req.isDone && req.isNetworkError)
                throw new Exception(url + " " + req.error);
#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(url + " HTTP " + req.responseCode + " " + req.error);
#else
            if (req.isHttpError)
                throw new Exception(url + " HTTP " + req.responseCode);
#endif
            return req.downloadHandler.text;
        }
    }

    static string Post(string url, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SendWebRequest();
            while (!req.isDone) { }
#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(url + " HTTP " + req.responseCode + " " + req.error);
#else
            if (req.isHttpError || req.isNetworkError)
                throw new Exception(url + " " + req.error);
#endif
            return req.downloadHandler.text;
        }
    }

    static void AssertOk(string name, string body) => Log("[PASS] " + name + " => " + body);
    static void AssertContains(string name, string body, string needle)
    {
        if (body == null || !body.Contains(needle)) throw new Exception(name + " expected '" + needle + "'");
        Log("[PASS] " + name);
    }
    static void Log(string msg) => Debug.Log(msg);
    class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] c) => true; }
}
#endif
