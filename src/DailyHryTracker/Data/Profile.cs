using System.Text.Json;
using System.Text.Json.Serialization;

namespace DailyHryTracker.Data;

public class DailyResult
{
    public bool Won { get; set; }
    public int Score { get; set; }
    public int Attempts { get; set; }
    /// <summary>Krátký textový souhrn (např. mřížka Wordle), zobrazuje se v historii a posílá se přátelům.</summary>
    public string Summary { get; set; } = "";
    public DateTime FinishedAt { get; set; }
}

public class DayRecord
{
    /// <summary>Hry odehrané nativně v aplikaci (klíč = id hry).</summary>
    public Dictionary<string, DailyResult> Results { get; set; } = new();
    /// <summary>Ručně odškrtnuté hry (třeba když jsi hrál jinde).</summary>
    public HashSet<string> ManualChecks { get; set; } = new();
    public int EndlessPlayed { get; set; }

    [JsonIgnore] public int CompletedCount => Results.Keys.Union(ManualChecks).Count();
    public bool IsDone(string gameId) => Results.ContainsKey(gameId) || ManualChecks.Contains(gameId);
}

public class EndlessStats
{
    public int Played { get; set; }
    public int Won { get; set; }
    public int BestScore { get; set; }
    public long TotalScore { get; set; }
}

public class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; set; } = Environment.UserName;
    public DateTime Created { get; set; } = DateTime.Now;
    public Dictionary<string, DayRecord> Days { get; set; } = new();
    public Dictionary<string, EndlessStats> Endless { get; set; } = new();
    public Dictionary<string, DateTime> Awards { get; set; } = new();
    public List<FriendSnapshot> Friends { get; set; } = new();

    public static string Key(DateTime d) => d.ToString("yyyy-MM-dd");

    public DayRecord Day(DateTime d, bool create = false)
    {
        var k = Key(d);
        if (Days.TryGetValue(k, out var r)) return r;
        if (!create) return null;
        r = new DayRecord();
        Days[k] = r;
        return r;
    }

    public EndlessStats EndlessFor(string gameId)
    {
        if (!Endless.TryGetValue(gameId, out var s)) Endless[gameId] = s = new EndlessStats();
        return s;
    }
}

public static class SaveStore
{
    public static string Folder
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(root)) root = AppContext.BaseDirectory;
            return Path.Combine(root, "DailyHryTracker");
        }
    }

    public static string FilePath => Path.Combine(Folder, "profile.json");

    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Profile Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Profile>(File.ReadAllText(FilePath), Json) ?? new Profile();
        }
        catch (Exception e)
        {
            // poškozený soubor nezahodíme – odložíme ho stranou
            try { File.Copy(FilePath, FilePath + ".broken-" + DateTime.Now.Ticks, true); } catch { }
            Console.Error.WriteLine("Nepodařilo se načíst profil: " + e.Message);
        }
        return new Profile();
    }

    public static void Save(Profile p)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(p, Json));
            File.Move(tmp, FilePath, true);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Nepodařilo se uložit profil: " + e.Message);
        }
    }
}
