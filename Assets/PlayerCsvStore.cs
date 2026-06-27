using System;
using System.IO;

public static class PlayerCsvStore
{
    const string PlayerHeader = "Index,ID,PlayTimes,LatestUpdate,DanceStar";
    const string ResultHeader = "DateTime,Type,Trial,Point,etc";

    public struct PlayerRecord
    {
        public int idx;
        public string id;
        public int playTimes;
        public string danceStar;
        public string savePath;
    }

    static string PlayersPath => Path.Combine(MBodyPaths.DataRoot, "Players.csv");

    public static bool TryPrepareOnLogin(string playerId, out PlayerRecord record)
    {
        record = default;
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        playerId = playerId.Trim();
        var nowDate = DateTime.Now.ToString("yy-MM-dd");

        try {
            EnsurePlayersFile(playerId, nowDate);

            var found = false;
            var count = 0;
            using (var sr = new StreamReader(PlayersPath)) {
                sr.ReadLine();
                while (true) {
                    var dataString = sr.ReadLine();
                    if (dataString == null)
                        break;
                    count++;
                    var val = dataString.Split(',');
                    if (val.Length > 1 && val[1] == playerId) {
                        record.id = val[1];
                        found = true;
                        record.idx = int.Parse(val[0]);
                        var latest = val[3];
                        record.playTimes = int.Parse(val[2]);
                        if (latest != nowDate)
                            record.playTimes++;
                        record.danceStar = NormalizeDanceStar(val[4]);
                        break;
                    }
                }
            }

            if (found) {
                RewritePlayerRow(playerId, record.idx, record.playTimes, nowDate, record.danceStar);
            } else {
                record.idx = count + 1;
                record.id = playerId;
                record.playTimes = 1;
                record.danceStar = "0";
                using (var outStream = new StreamWriter(PlayersPath, true)) {
                    outStream.WriteLine(record.idx + "," + playerId + ",1," + nowDate + ",0");
                }
            }

            record.savePath = Path.Combine(MBodyPaths.DataRoot, "saveData_" + playerId + ".csv");
            if (!File.Exists(record.savePath)) {
                using (var outStream = File.CreateText(record.savePath)) {
                    outStream.WriteLine(ResultHeader);
                }
            }

            return true;
        } catch (Exception ex) {
            UnityEngine.Debug.LogWarning("[PlayerCsvStore] TryPrepareOnLogin failed: " + ex.Message);
            return false;
        }
    }

    public static bool TryUpdateProgress(string playerId, int playTimes, string danceStar)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        playerId = playerId.Trim();
        var nowDate = DateTime.Now.ToString("yy-MM-dd");

        try {
            if (!File.Exists(PlayersPath) || !FindPlayerRow(playerId, out var idx))
                return false;

            RewritePlayerRow(playerId, idx, playTimes, nowDate, danceStar);
            return true;
        } catch (Exception ex) {
            UnityEngine.Debug.LogWarning("[PlayerCsvStore] TryUpdateProgress failed: " + ex.Message);
            return false;
        }
    }

    static void EnsurePlayersFile(string playerId, string nowDate)
    {
        if (File.Exists(PlayersPath))
            return;

        using (var outStream = File.CreateText(PlayersPath)) {
            outStream.WriteLine(PlayerHeader);
            outStream.WriteLine("1," + playerId + ",1," + nowDate + ",1");
        }
    }

    static bool FindPlayerRow(string playerId, out int idx)
    {
        idx = 0;
        using (var sr = new StreamReader(PlayersPath)) {
            sr.ReadLine();
            while (true) {
                var dataString = sr.ReadLine();
                if (dataString == null)
                    return false;
                var val = dataString.Split(',');
                if (val.Length > 1 && val[1] == playerId) {
                    idx = int.Parse(val[0]);
                    return true;
                }
            }
        }
    }

    static void RewritePlayerRow(string playerId, int idx, int playTimes, string nowDate, string danceStar)
    {
        var st = File.ReadAllLines(PlayersPath);
        var lineIndex = -1;
        for (var i = 0; i < st.Length; i++) {
            if (st[i].Split(',')[1] == playerId) {
                lineIndex = i;
                break;
            }
        }

        if (lineIndex < 0)
            return;

        st[lineIndex] = idx + "," + playerId + "," + playTimes + "," + nowDate + "," + danceStar;
        using (var outStream = new StreamWriter(PlayersPath)) {
            outStream.WriteLine(PlayerHeader);
            for (var j = 1; j < st.Length; j++)
                outStream.WriteLine(st[j]);
        }
    }

    static string NormalizeDanceStar(string value)
    {
        if (value.Length < 6)
            return "FFFFFF-FFFFFFF";
        if (value.Length == 6)
            return value + "-FFFFFFF";
        return value;
    }
}
