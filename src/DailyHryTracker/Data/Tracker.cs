using DailyHryTracker.Games;

namespace DailyHryTracker.Data;

public class GameStatsView
{
    public int DailyPlayed, DailyWon, CurrentStreak, BestStreak;
    public double AvgScore;
    public int BestScore;
    public EndlessStats Endless;
    public float WinRate => DailyPlayed == 0 ? 0 : DailyWon / (float)DailyPlayed;
}

/// <summary>Výpočty nad profilem: série, statistiky, zápis výsledků a ocenění.</summary>
public class Tracker
{
    public Profile P { get; }
    public event Action<AwardDef> AwardUnlocked;

    public Tracker(Profile p)
    {
        P = p;
    }

    public static DateTime Today => DateTime.Today;

    public void Save() => SaveStore.Save(P);

    public int CountOn(DateTime d) => P.Day(d)?.CompletedCount ?? 0;
    public bool ActiveOn(DateTime d) => CountOn(d) > 0;
    public bool IsDone(string gameId, DateTime d) => P.Day(d)?.IsDone(gameId) ?? false;
    public DailyResult ResultOf(string gameId, DateTime d) =>
        P.Day(d) is { } r && r.Results.TryGetValue(gameId, out var res) ? res : null;

    // ---------------- série ----------------

    /// <summary>Aktuální série: počítá se od dneška, ale pokud dnes ještě nic, tak od včerejška (série ještě nepropadla).</summary>
    public int CurrentStreak(Func<DateTime, bool> active = null)
    {
        active ??= ActiveOn;
        var d = Today;
        if (!active(d)) d = d.AddDays(-1);
        int n = 0;
        while (active(d)) { n++; d = d.AddDays(-1); }
        return n;
    }

    public int BestStreak(Func<DateTime, bool> active = null)
    {
        active ??= ActiveOn;
        var dates = P.Days.Keys.Select(k => DateTime.Parse(k)).Where(active).OrderBy(x => x).ToList();
        int best = 0, cur = 0;
        DateTime? prev = null;
        foreach (var d in dates)
        {
            cur = prev.HasValue && (d - prev.Value).TotalDays == 1 ? cur + 1 : 1;
            best = Math.Max(best, cur);
            prev = d;
        }
        return best;
    }

    public int ActiveDays => P.Days.Values.Count(d => d.CompletedCount > 0);
    public int TotalCompleted => P.Days.Values.Sum(d => d.CompletedCount);
    public int TotalEndless => P.Endless.Values.Sum(e => e.Played);

    public int PerfectDaysStreak() => CurrentStreak(d => CountOn(d) >= GameCatalog.All.Count);

    public GameStatsView StatsFor(string gameId)
    {
        var results = P.Days.Values.Where(d => d.Results.ContainsKey(gameId)).Select(d => d.Results[gameId]).ToList();
        return new GameStatsView
        {
            DailyPlayed = results.Count,
            DailyWon = results.Count(r => r.Won),
            AvgScore = results.Count == 0 ? 0 : results.Average(r => r.Score),
            BestScore = results.Count == 0 ? 0 : results.Max(r => r.Score),
            CurrentStreak = CurrentStreak(d => IsDone(gameId, d)),
            BestStreak = BestStreak(d => IsDone(gameId, d)),
            Endless = P.EndlessFor(gameId),
        };
    }

    // ---------------- zápis ----------------

    public void RecordDaily(string gameId, DailyResult r, DateTime date)
    {
        var day = P.Day(date, true);
        if (day.Results.ContainsKey(gameId)) return;
        r.FinishedAt = DateTime.Now;
        day.Results[gameId] = r;
        CheckAwards();
        Save();
    }

    public void RecordEndless(string gameId, bool won, int score)
    {
        var s = P.EndlessFor(gameId);
        s.Played++;
        if (won) s.Won++;
        s.TotalScore += score;
        s.BestScore = Math.Max(s.BestScore, score);
        P.Day(Today, true).EndlessPlayed++;
        CheckAwards();
        Save();
    }

    public void ToggleManual(string gameId, DateTime date)
    {
        var day = P.Day(date, true);
        if (day.Results.ContainsKey(gameId)) return; // nativně dohrané nejde odškrtnout
        if (!day.ManualChecks.Remove(gameId)) day.ManualChecks.Add(gameId);
        CheckAwards();
        Save();
    }

    // ---------------- ocenění ----------------

    public void CheckAwards()
    {
        foreach (var a in Awards.All)
        {
            if (P.Awards.ContainsKey(a.Id)) continue;
            var (cur, target) = a.Progress(this);
            if (cur >= target)
            {
                P.Awards[a.Id] = DateTime.Now;
                AwardUnlocked?.Invoke(a);
            }
        }
    }

    /// <summary>Nejlepší výsledek v dané hře splňující podmínku (pro herní ocenění).</summary>
    public bool AnyResult(string gameId, Func<DailyResult, bool> pred) =>
        P.Days.Values.Any(d => d.Results.TryGetValue(gameId, out var r) && pred(r));
}
