using DailyHryTracker.Core;
using DailyHryTracker.Data;
using DailyHryTracker.Games;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Screens;

public class MainScreen : Screen
{
    enum Tab { Overview, Stats, Awards, Friends, Settings }

    Tab _tab = Tab.Overview;
    static readonly (Tab tab, string label)[] Tabs =
    {
        (Tab.Overview, "Přehled"), (Tab.Stats, "Statistiky"), (Tab.Awards, "Ocenění"), (Tab.Friends, "Přátelé"),
        (Tab.Settings, "Nastavení"),
    };

    readonly FriendsView _friends = new();
    readonly TextField _name = new() { MaxLength = 24, Placeholder = "Tvoje jméno" };
    int _awardScroll;

    static Tracker T => TrackerApp.I.Tracker;

    public void SelectTab(string name)
    {
        foreach (var (tab, _) in Tabs)
            if (tab.ToString().Equals(name, StringComparison.OrdinalIgnoreCase)) _tab = tab;
    }

    public override void Frame(float dt)
    {
        DrawTopBar();
        var content = new Rectangle(20, 80, Ui.Width - 40, Ui.Height - 96);
        switch (_tab)
        {
            case Tab.Overview: DrawOverview(content); break;
            case Tab.Stats: DrawStats(content); break;
            case Tab.Awards: DrawAwards(content); break;
            case Tab.Friends: _friends.Frame(content, dt); break;
            case Tab.Settings: DrawSettings(content); break;
        }
    }

    void DrawTopBar()
    {
        Ui.Rect(new Rectangle(0, 0, Ui.Width, 64), Theme.Panel);
        Ui.Rect(new Rectangle(0, 63, Ui.Width, 1), Theme.Line);
        // logo: malá heatmapa 3x3
        for (int i = 0; i < 9; i++)
            Ui.RoundRect(new Rectangle(20 + i % 3 * 11, 16 + i / 3 * 11, 9, 9), Theme.Heat[1 + (i * 7 + 3) % 4], 2);
        Ui.Text("Daily Hry Tracker", 64, 18, 22, Theme.Text, true);

        int x = 300;
        foreach (var (tab, label) in Tabs)
        {
            var w = (int)Ui.Measure(label, 17, true).X + 28;
            var r = new Rectangle(x, 14, w, 36);
            bool active = _tab == tab;
            if (active) Ui.RoundRect(r, Theme.Panel2, 8);
            else if (Ui.Hover(r)) Ui.RoundRect(r, Theme.Panel2 * 0.5f, 8);
            Ui.TextCentered(label, r, 17, active ? Theme.Text : Theme.TextDim, true);
            if (active) Ui.Rect(new Rectangle(r.X + 10, 60, r.Width - 20, 3), Theme.Orange);
            if (Ui.Clicked(r))
            {
                _tab = tab;
                TextField.Focused = null;
                if (tab == Tab.Friends) FriendSync.Scan(T);
                if (tab == Tab.Settings) _name.Value = T.P.Name;
            }
            x += w + 6;
        }

        int streak = T.CurrentStreak();
        bool todayActive = T.ActiveOn(Tracker.Today);
        var sr = new Rectangle(Ui.Width - 250, 12, 230, 40);
        Ui.RoundRect(sr, Theme.Panel2, 10);
        Flame(new Vector2(sr.X + 24, sr.Center.Y + 2), 12, todayActive ? Theme.Orange : Theme.TextDim);
        Ui.Text($"{streak} {TextUtil.Plural(streak, "den", "dny", "dní")} v kuse", sr.X + 44, sr.Y + 9, 18, Theme.Text, true);
        if (Ui.Hover(sr))
            Ui.Tooltip = todayActive ? "Dnes už máš odehráno – série pokračuje!" : "Dnes jsi ještě nehrál – zahraj aspoň jednu hru, ať série nepropadne.";
    }

    public static void Flame(Vector2 c, float s, Color col)
    {
        Ui.Circle(c + new Vector2(0, s * 0.3f), s * 0.75f, col);
        Ui.Circle(c + new Vector2(0, -s * 0.2f), s * 0.5f, col);
        Ui.Circle(c + new Vector2(s * 0.05f, -s * 0.6f), s * 0.28f, col);
        Ui.Circle(c + new Vector2(0, s * 0.4f), s * 0.38f, Theme.Lighten(col, 0.5f));
    }

    // =============================== PŘEHLED ===============================

    void DrawOverview(Rectangle c)
    {
        var heat = new Rectangle(c.X, c.Y, 950, 205);
        Ui.Panel(heat);
        Heatmap.Draw(new Rectangle(heat.X + 16, heat.Y + 14, heat.Width - 32, heat.Height - 28), T.CountOn, "aktivita za poslední rok");

        var st = new Rectangle(heat.Right + 16, c.Y, c.Right - heat.Right - 16, 205);
        Ui.Panel(st);
        int today = T.CountOn(Tracker.Today);
        Ui.Text("Dnes", st.X + 18, st.Y + 14, 15, Theme.TextDim, true);
        Ui.Text($"{today} / {GameCatalog.All.Count}", st.X + 18, st.Y + 34, 34, Theme.Text, true);
        Ui.ProgressBar(new Rectangle(st.X + 18, st.Y + 80, st.Width - 36, 10), today / (float)GameCatalog.All.Count, Theme.Green);
        int y = st.Y + 104;
        StatLine(st, ref y, "Aktuální série", $"{T.CurrentStreak()}");
        StatLine(st, ref y, "Nejdelší série", $"{T.BestStreak()}");
        StatLine(st, ref y, "Aktivních dní", $"{T.ActiveDays}");
        StatLine(st, ref y, "Ocenění", $"{T.P.Awards.Count} / {Awards.All.Count}");

        Ui.Text("Dnešní hry", c.X, heat.Bottom + 14, 18, Theme.Text, true);
        Ui.Text("„Hrát dnešní“ otevře originální web přímo v aplikaci, křížek vpravo nahoře ho zavře a odškrtne", c.X + 110, heat.Bottom + 17, 14, Theme.TextDim);
        int gy = heat.Bottom + 44;
        const int cols = 3, gap = 14;
        int cw = (c.Width - (cols - 1) * gap) / cols, ch = 140;
        for (int i = 0; i < GameCatalog.All.Count; i++)
        {
            var r = new Rectangle(c.X + i % cols * (cw + gap), gy + i / cols * (ch + gap), cw, ch);
            DrawGameCard(r, GameCatalog.All[i]);
        }
    }

    static void StatLine(Rectangle panel, ref int y, string label, string value)
    {
        Ui.Text(label, panel.X + 18, y, 15, Theme.TextDim);
        Ui.TextRight(value, panel.Right - 18, y, 15, Theme.Text, true);
        y += 23;
    }

    void DrawGameCard(Rectangle r, GameInfo g)
    {
        var day = T.P.Day(Tracker.Today);
        var result = T.ResultOf(g.Id, Tracker.Today);
        bool manual = day?.ManualChecks.Contains(g.Id) ?? false;
        bool done = result != null || manual;

        Ui.Panel(r, done ? Color.Lerp(Theme.Panel, g.Color, 0.12f) : Theme.Panel);
        Ui.RoundRect(new Rectangle(r.X, r.Y, 6, r.Height), g.Color, 3);
        Ui.Text(g.Name, r.X + 20, r.Y + 14, 21, Theme.Text, true);
        Ui.Text(Ui.Ellipsize(g.Tagline, r.Width - 90, 14), r.X + 20, r.Y + 42, 14, Theme.TextDim);

        var cb = new Rectangle(r.Right - 44, r.Y + 14, 28, 28);
        if (Ui.Checkbox(cb, done, g.Color))
        {
            if (result != null) TrackerApp.I.Notify("Hra dohraná v aplikaci se nedá odškrtnout zpět");
            else T.ToggleManual(g.Id, Tracker.Today);
        }
        if (Ui.Hover(cb)) Ui.Tooltip = result != null ? "Dohráno v aplikaci" : done ? "Odškrtnuto – klikni pro zrušení" : "Odškrtnout dnešní hru";

        var stats = T.StatsFor(g.Id);
        string status = result != null
            ? (result.Won ? "Výhra" : "Prohra") + " · " + ShortSummary(g.Id, result)
            : manual ? "Dnes odškrtnuto" : WebGames.IsOpen(g.Id) ? "Hra je otevřená v okně…" : "Dnes ještě nehráno";
        Ui.Text(Ui.Ellipsize(status, r.Width - 40, 15), r.X + 20, r.Y + 66, 15, done ? Theme.Text : Theme.TextDim, result != null);
        if (stats.CurrentStreak > 0)
        {
            Flame(new Vector2(r.Right - 70, r.Y + 75), 7, Theme.Orange);
            Ui.TextRight($"{stats.CurrentStreak}", r.Right - 18, r.Y + 66, 15, Theme.Orange, true);
        }

        int bw = (r.Width - 40 - 10) / 2;
        var b1 = new Rectangle(r.X + 20, r.Bottom - 48, bw, 36);
        var b2 = new Rectangle(b1.Right + 10, b1.Y, bw, 36);
        bool open = WebGames.IsOpen(g.Id);
        if (Ui.Button(b1, open ? "Otevřeno…" : done ? "Hrát znovu" : "Hrát dnešní", g.Color, Theme.Bg, 16, !open))
        {
            if (EmbeddedWeb.Supported) TrackerApp.I.Open(new WebGameScreen(g));
            else if (!WebGames.Open(g)) TrackerApp.I.Notify(WebGames.LastError); // Linux: samostatné okno
        }
        if (Ui.Hover(b1)) Ui.Tooltip = $"Otevře originální {g.Name} přímo v aplikaci ({new Uri(g.Url).Host})";
        if (Ui.Button(b2, "Nekonečná", Theme.Panel2, Theme.Text, 16))
            TrackerApp.I.Open(new GameScreen(g, false));
        if (Ui.Hover(b2)) Ui.Tooltip = "Nekonečná verze přímo v aplikaci – nová hra pokaždé";
    }

    public static string ShortSummary(string gameId, DailyResult r) => gameId switch
    {
        "wordle" => r.Won ? $"{r.Attempts}/6" : "X/6",
        "connections" => $"chyb: {r.Attempts}",
        "krillion" or "noillirk" => $"{r.Score} bodů",
        "globle" => $"{r.Attempts} pokusů",
        "worldle" => r.Won ? $"{r.Attempts}/6" : "X/6",
        "spotle" => r.Won ? $"{r.Attempts}/10" : "X/10",
        "poople" => r.Summary,
        "wikirace" => r.Won ? $"{r.Attempts} kliků" : "nedokončeno",
        _ => r.Summary,
    };

    // =============================== STATISTIKY ===============================

    void DrawStats(Rectangle c)
    {
        var table = new Rectangle(c.X, c.Y, c.Width, 44 + GameCatalog.All.Count * 40 + 10);
        Ui.Panel(table);
        string[] headers = { "Hra", "Denní hry", "Výhry", "Úspěšnost", "Série", "Nejdelší", "Prům. skóre", "Nejlepší", "Nekonečné", "Nek. výhry" };
        int[] xs = { 20, 230, 340, 430, 550, 640, 750, 870, 970, 1100 };
        for (int i = 0; i < headers.Length; i++)
            Ui.Text(headers[i], table.X + xs[i], table.Y + 14, 15, Theme.TextDim, true);
        int y = table.Y + 44;
        foreach (var g in GameCatalog.All)
        {
            var s = T.StatsFor(g.Id);
            var row = new Rectangle(table.X + 8, y, table.Width - 16, 36);
            if (Ui.Hover(row)) Ui.RoundRect(row, Theme.Panel2, 6);
            Ui.RoundRect(new Rectangle(table.X + 12, y + 10, 16, 16), g.Color, 4);
            Ui.Text(g.Name, table.X + 36, y + 8, 17, Theme.Text, true);
            string[] vals =
            {
                s.DailyPlayed.ToString(), s.DailyWon.ToString(), s.DailyPlayed == 0 ? "–" : $"{s.WinRate * 100:0} %",
                s.CurrentStreak.ToString(), s.BestStreak.ToString(), s.DailyPlayed == 0 ? "–" : $"{s.AvgScore:0.#}",
                s.DailyPlayed == 0 ? "–" : s.BestScore.ToString(), s.Endless.Played.ToString(), s.Endless.Won.ToString(),
            };
            for (int i = 0; i < vals.Length; i++)
                Ui.Text(vals[i], table.X + xs[i + 1], y + 8, 17, Theme.Text);
            y += 40;
        }

        // posledních 30 dní
        var chart = new Rectangle(c.X, table.Bottom + 16, c.Width, c.Bottom - table.Bottom - 16);
        Ui.Panel(chart);
        Ui.Text("Posledních 30 dní", chart.X + 20, chart.Y + 14, 17, Theme.Text, true);
        Ui.Text("počet dohraných her za den", chart.X + 190, chart.Y + 17, 14, Theme.TextDim);
        int n = 30, max = GameCatalog.All.Count;
        var plot = new Rectangle(chart.X + 50, chart.Y + 50, chart.Width - 80, chart.Height - 90);
        for (int v = 0; v <= max; v += 3)
        {
            float gy = plot.Bottom - v / (float)max * plot.Height;
            Ui.Rect(plot.X, gy, plot.Width, 1, Theme.Line * 0.6f);
            Ui.TextRight(v.ToString(), plot.X - 10, gy - 8, 13, Theme.TextDim);
        }
        float bw = plot.Width / (float)n;
        for (int i = 0; i < n; i++)
        {
            var d = Tracker.Today.AddDays(i - n + 1);
            int cnt = T.CountOn(d);
            float h = cnt / (float)max * plot.Height;
            var br = new Rectangle((int)(plot.X + i * bw + 3), (int)(plot.Bottom - h), (int)bw - 6, (int)Math.Ceiling(h));
            if (cnt > 0) Ui.RoundRect(br, Theme.Heat[Heatmap.Level(cnt)], 3);
            if (i % 5 == 4 || i == n - 1)
                Ui.TextCentered(d.ToString("d.M."), new Rectangle((int)(plot.X + i * bw), plot.Bottom + 6, (int)bw, 16), 12, Theme.TextDim);
            var hover = new Rectangle((int)(plot.X + i * bw), plot.Y, (int)bw, plot.Height);
            if (Ui.Hover(hover)) Ui.Tooltip = $"{d:d. M. yyyy}: {cnt} {TextUtil.Plural(cnt, "hra", "hry", "her")}";
        }
    }

    // =============================== OCENĚNÍ ===============================

    void DrawAwards(Rectangle c)
    {
        Ui.Text($"Odemčeno {T.P.Awards.Count} z {Awards.All.Count}", c.X, c.Y, 20, Theme.Text, true);
        var area = new Rectangle(c.X, c.Y + 40, c.Width, c.Height - 40);
        const int cols = 4, gap = 12, h = 96;
        int w = (area.Width - (cols - 1) * gap) / cols;
        var ordered = Awards.All.OrderByDescending(a => T.P.Awards.ContainsKey(a.Id)).ThenBy(a => a.Kind).ToList();
        int rows = (ordered.Count + cols - 1) / cols;
        int contentH = rows * (h + gap);
        if (Ui.Hover(area)) _awardScroll = Math.Clamp(_awardScroll - Ui.Wheel * 40, 0, Math.Max(0, contentH - area.Height));
        Ui.BeginClip(area);
        for (int i = 0; i < ordered.Count; i++)
        {
            var a = ordered[i];
            var r = new Rectangle(area.X + i % cols * (w + gap), area.Y + i / cols * (h + gap) - _awardScroll, w, h);
            if (r.Bottom < area.Y || r.Y > area.Bottom) continue;
            bool unlocked = T.P.Awards.TryGetValue(a.Id, out var when);
            Ui.Panel(r, unlocked ? Color.Lerp(Theme.Panel, a.Color, 0.1f) : Theme.Panel);
            TrackerApp.Medal(new Vector2(r.X + 40, r.Y + 54), 24, a.Color, unlocked);
            Ui.Text(Ui.Ellipsize(a.Name, w - 90, 17), r.X + 78, r.Y + 14, 17, unlocked ? Theme.Text : Theme.TextDim, true);
            Ui.TextWrapped(a.Description, new Rectangle(r.X + 78, r.Y + 38, w - 90, 36), 13, Theme.TextDim, false, 2);
            if (unlocked)
                Ui.Text($"získáno {when:d. M. yyyy}", r.X + 78, r.Bottom - 22, 13, Theme.Gold);
            else
            {
                var (cur, target) = a.Progress(T);
                Ui.ProgressBar(new Rectangle(r.X + 78, r.Bottom - 18, w - 150, 8), cur / (float)target, a.Color);
                Ui.TextRight($"{Math.Min(cur, target)}/{target}", r.Right - 12, r.Bottom - 24, 13, Theme.TextDim);
            }
        }
        Ui.EndClip();
    }

    // =============================== NASTAVENÍ ===============================

    void DrawSettings(Rectangle c)
    {
        var p = new Rectangle(c.X, c.Y, 760, 420);
        Ui.Panel(p);
        int x = p.X + 24, y = p.Y + 20;
        Ui.Text("Tvoje jméno (uvidí ho kamarádi)", x, y, 16, Theme.TextDim, true);
        y += 26;
        _name.Frame(new Rectangle(x, y, 360, 44));
        if (Ui.Button(new Rectangle(x + 376, y, 120, 44), "Uložit", Theme.Accent, Theme.Text, 17, _name.Value.Trim().Length > 0)
            || (_name.Submitted && _name.Value.Trim().Length > 0))
        {
            T.P.Name = _name.Value.Trim();
            T.Save();
            TextField.Focused = null;
            TrackerApp.I.Notify("Jméno uloženo");
        }
        y += 70;
        Ui.Text("Data aplikace", x, y, 16, Theme.TextDim, true);
        y += 24;
        Ui.Text(Ui.Ellipsize(SaveStore.FilePath, 560, 15), x, y + 10, 15, Theme.Text);
        if (Ui.Button(new Rectangle(p.Right - 170, y, 146, 38), "Otevřít složku", null, null, 15))
            FriendSync.Reveal(SaveStore.Folder);
        y += 54;
        Ui.Text("Složka přátel", x, y, 16, Theme.TextDim, true);
        y += 24;
        Ui.Text(Ui.Ellipsize(FriendSync.FriendsFolder, 560, 15), x, y + 10, 15, Theme.Text);
        if (Ui.Button(new Rectangle(p.Right - 170, y, 146, 38), "Otevřít složku", null, null, 15))
        {
            Directory.CreateDirectory(FriendSync.FriendsFolder);
            FriendSync.Reveal(FriendSync.FriendsFolder);
        }
        y += 60;
        Ui.TextWrapped("Denní hry se otevírají jako originální weby přímo v okně aplikace (vestavěný prohlížeč, ne Safari ani Chrome). " +
                       "Křížek vpravo nahoře web zavře a hru odškrtne. Přihlášení a statistiky webů zůstávají uložené. " +
                       "Nekonečné verze běží přímo v aplikaci; Globle/Worldle si poprvé stáhnou obrysy zemí, WikiRace čte anglickou Wikipedii.",
            new Rectangle(x, y, p.Width - 48, 100), 15, Theme.TextDim);
        Ui.Text($"Profil ID: {T.P.Id} · vytvořen {T.P.Created:d. M. yyyy}", x, p.Bottom - 30, 13, Theme.TextDim);
    }
}
