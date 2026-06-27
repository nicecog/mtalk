#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class Cafe24ApiVerification
{
    public static void Run() => RunForService("mbody", "login", "logout", "ue_md_");

    [MenuItem("MTalk/Verify CAFE24 API (M-BODY)")]
    public static void RunFromMenu() => Run();

    static void RunForService(string service, string signInPath, string signOutPath, string idPrefix)
    {
        AllowHttp();
        var baseUrl = "http://hudit.cafe24.com:8080/" + service;
        try
        {
            var id = idPrefix + DateTime.Now.ToString("HHmmss");
            Log("=== Unity Editor API Verify (" + service.ToUpper() + ") ===");
            Log("Base: " + baseUrl);

            AssertOk("health", Get(baseUrl + "/api/health"));
            AssertContains("has=false", Get(baseUrl + "/api/users/accounts/id/has/" + id), "false");

            var adminBody = "{\"id\":\"super-admin\",\"password\":\"super-admin!@#$\"}";
            AssertOk("admin " + signInPath, Post(baseUrl + "/api/users/" + signInPath, adminBody));

            var createJson = "{\"levelId\":10,\"domain\":\"spike\",\"id\":\"" + id + "\",\"password\":\"pw1234\",\"name\":\"u\",\"email\":\"u@gmail.com\",\"phone\":\"000\"}";
            if (service == "mface" || service == "msocial")
                createJson = "{\"level\":1,\"domain\":\"spike\",\"id\":\"" + id + "\",\"password\":\"pw1234\",\"name\":\"u\",\"email\":\"u@gmail.com\",\"phone\":\"000\"}";

            AssertOk("create account", Post(baseUrl + "/api/users/accounts", createJson));
            AssertOk("sign-out", Get(baseUrl + "/api/users/" + signOutPath));

            var loginJson = "{\"id\":\"" + id + "\",\"password\":\"pw1234\"}";
            var loginResp = Post(baseUrl + "/api/users/" + signInPath, loginJson);
            AssertOk("user " + signInPath, loginResp);
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

    static void AllowHttp()
    {
#if UNITY_2022_2_OR_NEWER
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif
    }

    static string Get(string url)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new BypassCertificate();
            req.SendWebRequest();
            while (!req.isDone) { }
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(url + " HTTP " + req.responseCode + " " + req.error);
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
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(url + " HTTP " + req.responseCode + " " + req.error + " " + req.downloadHandler.text);
            return req.downloadHandler.text;
        }
    }

    static void AssertOk(string name, string body) => Log("[PASS] " + name + " => " + body);

    static void AssertContains(string name, string body, string needle)
    {
        if (body == null || !body.Contains(needle))
            throw new Exception(name + " expected '" + needle + "' in " + body);
        Log("[PASS] " + name);
    }

    static void Log(string msg) => Debug.Log(msg);

    class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
#endif
