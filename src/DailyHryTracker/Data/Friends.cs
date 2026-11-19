using System.Text;
using System.Text.Json;
using DailyHryTracker.Games;

namespace DailyHryTracker.Data;

public class FriendGameStat
{
    public int Played { get; set; }
    public int Won { get; set; }
    public double AvgScore { get; set; }
    public int Streak { get; set; }
    public int EndlessPlayed { get; set; }
}

/// <summary>
/// Veřejný "otisk" profilu, který si kamarádi vyměňují – buď jako kód přes schránku,
/// nebo jako soubor ve sdílené složce (Dropbox / iCloud / OneDrive / Google Drive…).
/// </summary>
public class FriendSnapshot
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Exported { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public int ActiveDays { get; set; }
    public int TotalGames { get; set; }
    public int EndlessGames { get; set; }
    public int AwardCount { get; set; }
    /// <summary>Aktivita za posledních 120 dní, nejstarší první, jeden znak = počet her (0-9).</summary>
    public string Recent { get; set; } = "";
    /// <summary>Datum posledního dne v <see cref="Recent"/>.</summary>
    public string RecentEnd { get; set; } = "";
    public Dictionary<string, FriendGameStat> Games { get; set; } = new();
    /// <summary>Dnešní výsledky: id hry → souhrn.</summary>
    public Dictionary<string, string> Today { get; set; } = new();
    public string TodayDate { get; set; } = "";

    public int CountOn(DateTime d)
    {
        if (!DateTime.TryParse(RecentEnd, out var end)) return 0;
        int idx = Recent.Length - 1 - (int)(end.Date - d.Date).TotalDays;
        if (idx < 0 || idx >= Recent.Length) return 0;
        return Recent[idx] - '0';
    }

    /// <summary>Aktuální série přepočtená k dnešku (snapshot mohl být exportován před pár dny).</summary>
    public int StreakNow
    {
        get
        {
            if (!DateTime.TryParse(RecentEnd, out var end)) return 0;
            var gap = (DateTime.Today - end.Date).TotalDays;
            if (gap > 1) return 0;
            return CurrentStreak;
        }
    }

    public int TodayCount => TodayDate == Profile.Key(DateTime.Today) ? Today.Count : 0;
}

public static class FriendSync
{
    public const string Extension = ".dht.json";

    /// <summary>Složka, kam se dávají soubory od kamarádů.</summary>
    public static string FriendsFolder => Path.Combine(SaveStore.Folder, "Friends");

    /// <summary>Složka, kam se ukládá můj exportovaný soubor (Plocha, jinak složka s daty).</summary>
    public static string ExportFolder
    {
        get
        {
            var desk = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Directory.Exists(desk) ? desk : SaveStore.Folder;
        }
    }

    public static string LastError;

    public static FriendSnapshot MakeSnapshot(Tracker t)
    {
        var s = new FriendSnapshot
        {
            Id = t.P.Id,
            Name = t.P.Name,
            Exported = DateTime.Now,
            CurrentStreak = t.CurrentStreak(),
            BestStreak = t.BestStreak(),
            ActiveDays = t.ActiveDays,
            TotalGames = t.TotalCompleted,
            EndlessGames = t.TotalEndless,
            AwardCount = t.P.Awards.Count,
            RecentEnd = Profile.Key(DateTime.Today),
            TodayDate = Profile.Key(DateTime.Today),
        };
        var sb = new StringBuilder();
        for (int i = 119; i >= 0; i--)
            sb.Append((char)('0' + Math.Min(9, t.CountOn(DateTime.Today.AddDays(-i)))));
        s.Recent = sb.ToString();
        foreach (var g in GameCatalog.All)
        {
            var st = t.StatsFor(g.Id);
            s.Games[g.Id] = new FriendGameStat
            {
                Played = st.DailyPlayed, Won = st.DailyWon, AvgScore = Math.Round(st.AvgScore, 2),
                Streak = st.CurrentStreak, EndlessPlayed = st.Endless.Played,
            };
            var today = t.P.Day(DateTime.Today);
            if (today != null && today.IsDone(g.Id))
                s.Today[g.Id] = today.Results.TryGetValue(g.Id, out var r) ? r.Summary : "odškrtnuto";
        }
        return s;
    }

    static string SafeName(string s)
    {
        var bad = Path.GetInvalidFileNameChars();
        var clean = new string((s ?? "hrac").Select(c => bad.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return string.IsNullOrEmpty(clean) ? "hrac" : clean;
    }

    /// <summary>Vytvoří soubor se mnou (jméno + staty + datum), který pošleš kamarádovi. Vrací cestu.</summary>
    public static string Export(Tracker t)
    {
        try
        {
            var snap = MakeSnapshot(t);
            Directory.CreateDirectory(ExportFolder);
            var path = Path.Combine(ExportFolder, $"DailyHry-{SafeName(snap.Name)}{Extension}");
            File.WriteAllText(path, JsonSerializer.Serialize(snap, SaveStore.Json));
            LastError = null;
            return path;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            return null;
        }
    }

    static FriendSnapshot ReadFile(string path)
    {
        try
        {
            var s = JsonSerializer.Deserialize<FriendSnapshot>(File.ReadAllText(path));
            return string.IsNullOrEmpty(s?.Id) ? null : s;
        }
        catch { return null; }
    }

    /// <summary>
    /// Načte všechny soubory ze složky přátel. Od jednoho kamaráda (stejné Id) bere nejnovější export,
    /// starší soubory téhož kamaráda smaže, aby se složka nezaplňovala.
    /// </summary>
    public static void Scan(Tracker t)
    {
        try
        {
            Directory.CreateDirectory(FriendsFolder);
            var loaded = new Dictionary<string, (FriendSnapshot s, string path)>();
            foreach (var f in Directory.GetFiles(FriendsFolder, "*.json"))
            {
                var s = ReadFile(f);
                if (s == null || s.Id == t.P.Id) continue;
                if (loaded.TryGetValue(s.Id, out var prev))
                {
                    var (keep, drop) = prev.s.Exported >= s.Exported ? (prev, (s, f)) : ((s, f), prev);
                    loaded[s.Id] = keep;
                    try { File.Delete(drop.Item2); } catch { }
                }
                else loaded[s.Id] = (s, f);
            }
            t.P.Friends = loaded.Values.Select(v => v.s).OrderBy(s => s.Name).ToList();
            t.CheckAwards();
            t.Save();
            LastError = null;
        }
        catch (Exception e)
        {
            LastError = e.Message;
        }
    }

    /// <summary>Zkopíruje soubor od kamaráda do složky přátel (např. když ho vložíš cestou nebo ze schránky).</summary>
    public static FriendSnapshot Import(Tracker t, string path)
    {
        path = path?.Trim().Trim('"', '\'') ?? "";
        if (path.StartsWith("file://")) path = Uri.UnescapeDataString(path[7..]);
        if (path.StartsWith("~"))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..].TrimStart('/', '\\'));
        var s = File.Exists(path) ? ReadFile(path) : null;
        if (s == null)
        {
            LastError = "Soubor nejde načíst – je to export z Daily Hry Trackeru?";
            return null;
        }
        if (s.Id == t.P.Id)
        {
            LastError = "Tohle je tvůj vlastní soubor.";
            return null;
        }
        Directory.CreateDirectory(FriendsFolder);
        File.Copy(path, Path.Combine(FriendsFolder, $"{SafeName(s.Name)}-{s.Id}-{s.Exported:yyyyMMddHHmmss}{Extension}"), true);
        Scan(t);
        return s;
    }

    public static void Remove(Tracker t, FriendSnapshot s)
    {
        foreach (var f in Directory.GetFiles(FriendsFolder, "*.json"))
            if (ReadFile(f)?.Id == s.Id)
                try { File.Delete(f); } catch { }
        Scan(t);
    }

    /// <summary>Otevře složku ve Finderu / Průzkumníku.</summary>
    public static void Reveal(string folderOrFile)
    {
        try
        {
            string exe, args;
            bool isFile = File.Exists(folderOrFile);
            if (OperatingSystem.IsMacOS()) { exe = "open"; args = isFile ? $"-R \"{folderOrFile}\"" : $"\"{folderOrFile}\""; }
            else if (OperatingSystem.IsWindows()) { exe = "explorer"; args = isFile ? $"/select,\"{folderOrFile}\"" : $"\"{folderOrFile}\""; }
            else { exe = "xdg-open"; args = $"\"{(isFile ? Path.GetDirectoryName(folderOrFile) : folderOrFile)}\""; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, args) { UseShellExecute = false });
        }
        catch { }
    }
}
