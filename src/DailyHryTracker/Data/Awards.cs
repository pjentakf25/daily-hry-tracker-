using DailyHryTracker.Core;
using DailyHryTracker.Games;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Data;

public enum AwardKind { Streak, Volume, Mastery, Social }

public class AwardDef
{
    public string Id;
    public string Name;
    public string Description;
    public AwardKind Kind;
    public Color Color;
    public Func<Tracker, (int cur, int target)> Progress;
}

public static class Awards
{
    public static readonly List<AwardDef> All = Build();

    static AwardDef A(string id, string name, string desc, AwardKind kind, Color c, Func<Tracker, (int, int)> p) =>
        new() { Id = id, Name = name, Description = desc, Kind = kind, Color = c, Progress = p };

    static (int, int) Flag(bool b) => (b ? 1 : 0, 1);

    static List<AwardDef> Build()
    {
        var list = new List<AwardDef>();
        var streakColors = new[]
        {
            new Color(205, 127, 50), new Color(192, 192, 192), new Color(227, 179, 65),
            new Color(80, 200, 220), new Color(160, 100, 240), new Color(240, 90, 90), new Color(255, 215, 0)
        };
        int[] streaks = { 3, 7, 14, 30, 60, 100, 365 };
        string[] names = { "Rozjezd", "Celý týden", "Dva týdny", "Měsíc v kuse", "Dva měsíce", "Stovka", "Celý rok" };
        for (int i = 0; i < streaks.Length; i++)
        {
            int n = streaks[i];
            list.Add(A($"streak_{n}", names[i], $"Buď aktivní {n} dní v kuse", AwardKind.Streak, streakColors[i],
                t => (Math.Max(t.CurrentStreak(), t.BestStreak()), n)));
        }

        list.Add(A("perfect_day", "Perfektní den", "Dohraj všech 9 her během jednoho dne", AwardKind.Streak, Theme.Gold,
            t => (t.P.Days.Values.Select(d => d.CompletedCount).DefaultIfEmpty(0).Max(), GameCatalog.All.Count)));
        list.Add(A("perfect_3", "Perfektní trojka", "3 perfektní dny v kuse", AwardKind.Streak, Theme.Orange,
            t => (Math.Max(t.PerfectDaysStreak(), t.BestStreak(d => t.CountOn(d) >= GameCatalog.All.Count)), 3)));
        list.Add(A("perfect_7", "Perfektní týden", "7 perfektních dnů v kuse", AwardKind.Streak, Theme.Red,
            t => (t.BestStreak(d => t.CountOn(d) >= GameCatalog.All.Count), 7)));

        foreach (var g in GameCatalog.All)
        {
            var gid = g.Id;
            list.Add(A($"week_{gid}", $"{g.Name}: týden", $"Hraj {g.Name} 7 dní v kuse", AwardKind.Streak, g.Color,
                t => (t.BestStreak(d => t.IsDone(gid, d)), 7)));
        }

        int[] volume = { 10, 50, 100, 250, 500, 1000 };
        foreach (var v in volume)
            list.Add(A($"total_{v}", $"{v} her", $"Dokonči celkem {v} denních her", AwardKind.Volume, Theme.Accent,
                t => (t.TotalCompleted, v)));
        list.Add(A("endless_25", "Nekonečno", "Odehraj 25 nekonečných her", AwardKind.Volume, Theme.Purple,
            t => (t.TotalEndless, 25)));
        list.Add(A("endless_200", "Za hranicí nekonečna", "Odehraj 200 nekonečných her", AwardKind.Volume, Theme.Purple,
            t => (t.TotalEndless, 200)));

        // mistrovství v jednotlivých hrách
        list.Add(A("m_wordle", "Jasnovidec", "Uhodni Wordle na 2 pokusy", AwardKind.Mastery, GameCatalog.Get("wordle").Color,
            t => Flag(t.AnyResult("wordle", r => r.Won && r.Attempts <= 2))));
        list.Add(A("m_connections", "Bez chyby", "Vyřeš Connections bez jediné chyby", AwardKind.Mastery, GameCatalog.Get("connections").Color,
            t => Flag(t.AnyResult("connections", r => r.Won && r.Attempts == 0))));
        list.Add(A("m_krillion", "Jeden z milionu", "Získej v Krillionu 450+ bodů", AwardKind.Mastery, GameCatalog.Get("krillion").Color,
            t => Flag(t.AnyResult("krillion", r => r.Score >= 450))));
        list.Add(A("m_noillirk", "Hlas lidu", "Získej v Noillirku 450+ bodů", AwardKind.Mastery, GameCatalog.Get("noillirk").Color,
            t => Flag(t.AnyResult("noillirk", r => r.Score >= 450))));
        list.Add(A("m_globle", "Glóbus v hlavě", "Najdi zemi v Globle do 5 pokusů", AwardKind.Mastery, GameCatalog.Get("globle").Color,
            t => Flag(t.AnyResult("globle", r => r.Won && r.Attempts <= 5))));
        list.Add(A("m_worldle", "Kartograf", "Uhodni Worldle na první pokus", AwardKind.Mastery, GameCatalog.Get("worldle").Color,
            t => Flag(t.AnyResult("worldle", r => r.Won && r.Attempts == 1))));
        list.Add(A("m_spotle", "Filmový maniak", "Uhodni film ve Spotle do 3 pokusů", AwardKind.Mastery, GameCatalog.Get("spotle").Color,
            t => Flag(t.AnyResult("spotle", r => r.Won && r.Attempts <= 3))));
        list.Add(A("m_poople", "Nejkratší cesta", "Dojdi v Poople na par (nejmenší možný počet kroků)", AwardKind.Mastery, GameCatalog.Get("poople").Color,
            t => Flag(t.AnyResult("poople", r => r.Won && r.Score >= 100))));
        list.Add(A("m_wikirace", "Wiki-sprinter", "Dokonči WikiRace na max. 4 kliky", AwardKind.Mastery, GameCatalog.Get("wikirace").Color,
            t => Flag(t.AnyResult("wikirace", r => r.Won && r.Attempts <= 4))));

        list.Add(A("friend_1", "Nejsi v tom sám", "Přidej prvního kamaráda", AwardKind.Social, Theme.Green,
            t => (t.P.Friends.Count, 1)));
        list.Add(A("friend_5", "Parta", "Měj 5 kamarádů", AwardKind.Social, Theme.Green,
            t => (t.P.Friends.Count, 5)));
        return list;
    }
}
