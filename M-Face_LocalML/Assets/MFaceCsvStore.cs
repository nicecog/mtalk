using System;
using System.IO;

public static class MFaceCsvStore
{
    const string ProgressHeader = "Day_Count,Last_Login,Item";
    const string LogHeader = "Date,Time,Log";
    const string DefaultItemString = "FFFFFFFFFFFFFFFF";

    public struct FaceRecord
    {
        public int dayCount;
        public string lastLogin;
        public string itemString;
        public string csvPath;
    }

    public static string GetCsvPath(string playerId) =>
        Path.Combine(UnityEngine.Application.persistentDataPath, playerId + "_FaceData.csv");

    public static bool TryPrepareOnLogin(string playerId, out FaceRecord record)
    {
        record = default;
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        playerId = playerId.Trim();
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        try {
            EnsureFile(playerId, 1, today, DefaultItemString);
            record.csvPath = GetCsvPath(playerId);

            using (var sr = new StreamReader(record.csvPath)) {
                sr.ReadLine();
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    return false;

                var parts = line.Split(',');
                if (parts.Length < 3)
                    return false;

                record.dayCount = int.Parse(parts[0]);
                record.lastLogin = parts[1];
                record.itemString = NormalizeItemString(parts[2]);

                if (record.lastLogin != today) {
                    record.dayCount++;
                    record.lastLogin = today;
                    RewriteProgressRow(playerId, record.dayCount, record.lastLogin, record.itemString);
                }
            }

            return true;
        } catch (Exception ex) {
            UnityEngine.Debug.LogWarning("[MFaceCsvStore] TryPrepareOnLogin failed: " + ex.Message);
            return false;
        }
    }

    public static bool TryUpdateProgress(string playerId, int dayCount, string itemString, string lastLogin = null)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        playerId = playerId.Trim();
        var visitDate = string.IsNullOrWhiteSpace(lastLogin)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : lastLogin;

        try {
            EnsureFile(playerId, dayCount, visitDate, itemString);
            RewriteProgressRow(playerId, dayCount, visitDate, NormalizeItemString(itemString));
            return true;
        } catch (Exception ex) {
            UnityEngine.Debug.LogWarning("[MFaceCsvStore] TryUpdateProgress failed: " + ex.Message);
            return false;
        }
    }

    public static bool TryAppendLog(string playerId, string logLine)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(logLine))
            return false;

        try {
            EnsureFile(playerId, 1, DateTime.Today.ToString("yyyy-MM-dd"), DefaultItemString);
            using (var writer = new StreamWriter(GetCsvPath(playerId), true)) {
                writer.WriteLine(logLine.Trim());
            }
            return true;
        } catch (Exception ex) {
            UnityEngine.Debug.LogWarning("[MFaceCsvStore] TryAppendLog failed: " + ex.Message);
            return false;
        }
    }

    static void EnsureFile(string playerId, int dayCount, string lastLogin, string itemString)
    {
        var path = GetCsvPath(playerId);
        if (File.Exists(path))
            return;

        using (var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8)) {
            writer.WriteLine(ProgressHeader);
            writer.WriteLine(dayCount + "," + lastLogin + "," + NormalizeItemString(itemString));
            writer.WriteLine(LogHeader);
        }
    }

    static void RewriteProgressRow(string playerId, int dayCount, string lastLogin, string itemString)
    {
        var path = GetCsvPath(playerId);
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return;

        lines[1] = dayCount + "," + lastLogin + "," + NormalizeItemString(itemString);
        using (var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8)) {
            foreach (var line in lines) {
                if (!string.IsNullOrWhiteSpace(line))
                    writer.WriteLine(line.Trim());
            }
        }
    }

    public static string NormalizeItemString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 16)
            return DefaultItemString;
        return value.Trim().ToUpper().Substring(0, 16);
    }
}
