using DailyHryTracker.Core;
using DailyHryTracker.Data;
using DailyHryTracker.Games;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Screens;

/// <summary>Přátelé přes soubory: export vlastního souboru, import souborů od kamarádů a žebříčky.</summary>
public class FriendsView
{
    record Metric(string Label, Func<FriendSnapshot, double> Value, string Unit);

    static readonly Metric[] Metrics =
    {
        new("Aktuální série", s => s.StreakNow, "dní"),
        new("Nejdelší série", s => s.BestStreak, "dní"),
        new("Aktivní dny", s => s.ActiveDays, "dní"),
        new("Dohrané hry", s => s.TotalGames, "her"),
        new("Nekonečné", s => s.EndlessGames, "her"),
        new("Ocenění", s => s.AwardCount, ""),
        new("Dnes", s => s.TodayCount, "her"),
    };

    int _metric;
    string _game; // pokud je vybraná hra, žebříček je podle výher v ní
    string _lastExport;
    string _confirmRemove;
    float _refresh;
    FriendSnapshot _me;
    int _scroll;
    readonly TextField _path = new() { Placeholder = "…nebo sem vlož cestu k souboru", MaxLength = 400 };

    static Tracker T => TrackerApp.I.Tracker;

    public void Frame(Rectangle c, float dt)
    {
        _refresh -= dt;
        if (_me == null || _refresh <= 0)
        {
            _me = FriendSync.MakeSnapshot(T);
            _refresh = 1f;
        }

        // ---- můj soubor ----
        var mine = new Rectangle(c.X, c.Y, (c.Width - 16) / 2, 170);
        Ui.Panel(mine);
        Ui.Text("1. Pošli svůj soubor kamarádům", mine.X + 18, mine.Y + 14, 18, Theme.Text, true);
        Ui.TextWrapped("Vytvoří soubor s tvým jménem a statistikami k dnešnímu datu. Pošli ho komukoliv (chat, e-mail, flashka). " +
                       "Až budeš chtít poslat novější stav, vyexportuj a pošli ho znovu.",
            new Rectangle(mine.X + 18, mine.Y + 44, mine.Width - 36, 60), 14, Theme.TextDim);
        if (Ui.Button(new Rectangle(mine.X + 18, mine.Bottom - 56, 220, 40), "Exportovat můj soubor", Theme.Green, Theme.Text, 16))
        {
            _lastExport = FriendSync.Export(T);
            if (_lastExport != null)
            {
                FriendSync.Reveal(_lastExport);
                TrackerApp.I.Notify($"Uloženo: {Path.GetFileName(_lastExport)}");
            }
            else TrackerApp.I.Notify("Export selhal: " + FriendSync.LastError);
        }
        if (_lastExport != null)
        {
            Ui.Text(Ui.Ellipsize(_lastExport, mine.Width - 280, 13), mine.X + 252, mine.Bottom - 54, 13, Theme.TextDim);
            Ui.Text($"stav k {DateTime.Now:d. M. yyyy H:mm}", mine.X + 252, mine.Bottom - 34, 13, Theme.Gold);
        }

        // ---- přidat kamaráda ----
        var add = new Rectangle(mine.Right + 16, c.Y, c.Right - mine.Right - 16, 170);
        Ui.Panel(add);
        Ui.Text("2. Přidej soubor od kamaráda", add.X + 18, add.Y + 14, 18, Theme.Text, true);
        Ui.TextWrapped("Přetáhni soubor (.dht.json) myší do tohoto okna, nebo ho dej do složky přátel. Novější soubor od stejného kamaráda nahradí starší.",
            new Rectangle(add.X + 18, add.Y + 44, add.Width - 36, 60), 14, Theme.TextDim);
        int by = add.Bottom - 56;
        if (Ui.Button(new Rectangle(add.X + 18, by, 150, 40), "Složka přátel", null, null, 15))
        {
            Directory.CreateDirectory(FriendSync.FriendsFolder);
            FriendSync.Reveal(FriendSync.FriendsFolder);
        }
        if (Ui.Button(new Rectangle(add.X + 176, by, 100, 40), "Obnovit", null, null, 15))
        {
            FriendSync.Scan(T);
            TrackerApp.I.Notify($"Načteno přátel: {T.P.Friends.Count}");
        }
        bool submit = _path.Frame(new Rectangle(add.X + 286, by, add.Width - 286 - 18 - 90, 40), 14);
        if (Ui.Button(new Rectangle(add.Right - 98, by, 80, 40), "Přidat", Theme.Accent, Theme.Text, 15, _path.Value.Trim().Length > 0) || submit)
        {
            var s = FriendSync.Import(T, _path.Value);
            TrackerApp.I.Notify(s != null ? $"Kamarád {s.Name} přidán (data k {s.Exported:d. M. yyyy})" : FriendSync.LastError);
            if (s != null) _path.Value = "";
        }

        // ---- výběr žebříčku ----
        int y = mine.Bottom + 18;
        Ui.Text("Žebříček", c.X, y + 6, 20, Theme.Text, true);
        int x = c.X + 110;
        for (int i = 0; i < Metrics.Length; i++)
        {
            var w = (int)Ui.Measure(Metrics[i].Label, 14, true).X + 22;
            bool active = _game == null && _metric == i;
            if (Ui.Button(new Rectangle(x, y, w, 32), Metrics[i].Label, active ? Theme.Orange : Theme.Panel2, Theme.Text, 14))
            {
                _metric = i;
                _game = null;
            }
            x += w + 6;
        }
        y += 40;
        x = c.X + 110;
        Ui.Text("výhry ve hře:", c.X, y + 8, 13, Theme.TextDim);
        foreach (var g in GameCatalog.All)
        {
            var w = (int)Ui.Measure(g.Name, 13, true).X + 18;
            bool active = _game == g.Id;
            if (Ui.Button(new Rectangle(x, y, w, 30), g.Name, active ? g.Color : Theme.Panel2, active ? Theme.Bg : Theme.TextDim, 13))
                _game = g.Id;
            x += w + 6;
        }
        y += 44;

        // ---- tabulka ----
        var everyone = new List<(FriendSnapshot s, bool me)> { (_me, true) };
        everyone.AddRange(T.P.Friends.Select(f => (f, false)));
        Func<FriendSnapshot, double> value;
        Func<FriendSnapshot, string> label;
        if (_game != null)
        {
            var gid = _game;
            value = s => s.Games.TryGetValue(gid, out var gs) ? gs.Won : 0;
            label = s => s.Games.TryGetValue(gid, out var gs)
                ? $"{gs.Won} výher / {gs.Played} · série {gs.Streak}"
                : "–";
        }
        else
        {
            var m = Metrics[_metric];
            value = m.Value;
            label = s => $"{m.Value(s):0} {m.Unit}".Trim();
        }
        var ranked = everyone.OrderByDescending(e => value(e.s)).ThenBy(e => e.s.Name).ToList();

        var table = new Rectangle(c.X, y, c.Width, c.Bottom - y);
        Ui.Panel(table);
        string[] heads = { "#", "Hráč", "Stav k datu", "Hodnota", "Posledních 30 dní" };
        int[] hx = { 16, 60, 330, 560, 820 };
        for (int i = 0; i < heads.Length; i++) Ui.Text(heads[i], table.X + hx[i], table.Y + 12, 14, Theme.TextDim, true);

        var list = new Rectangle(table.X, table.Y + 38, table.Width, table.Height - 44);
        const int rowH = 44;
        int maxScroll = Math.Max(0, ranked.Count * rowH - list.Height);
        if (Ui.Hover(list)) _scroll = Math.Clamp(_scroll - Ui.Wheel * 30, 0, maxScroll);
        _scroll = Math.Min(_scroll, maxScroll);
        Ui.BeginClip(list);
        for (int i = 0; i < ranked.Count; i++)
        {
            var (s, me) = ranked[i];
            var row = new Rectangle(list.X + 8, list.Y + i * rowH - _scroll, list.Width - 16, rowH - 4);
            if (row.Bottom < list.Y || row.Y > list.Bottom) continue;
            Ui.RoundRect(row, me ? Color.Lerp(Theme.Panel2, Theme.Accent, 0.15f) : Ui.Hover(row) ? Theme.Panel2 : Theme.Panel, 6);
            var rankCol = i == 0 ? Theme.Gold : i == 1 ? new Color(192, 192, 192) : i == 2 ? new Color(205, 127, 50) : Theme.TextDim;
            Ui.Text($"{i + 1}.", row.X + 8, row.Y + 10, 18, rankCol, true);
            Ui.Text(Ui.Ellipsize(s.Name + (me ? " (ty)" : ""), 250, 18), row.X + 52, row.Y + 10, 18, Theme.Text, true);

            int age = (int)(DateTime.Today - s.Exported.Date).TotalDays;
            string when = me ? "teď" : $"{s.Exported:d. M. yyyy}" + (age == 0 ? " (dnes)" : age == 1 ? " (včera)" : $" (před {age} dny)");
            Ui.Text(when, row.X + 322, row.Y + 12, 15, !me && age > 3 ? Theme.Orange : Theme.TextDim);
            if (!me && Ui.Hover(new Rectangle(row.X + 322, row.Y, 220, row.Height)))
                Ui.Tooltip = $"Statistiky jsou ze souboru exportovaného {s.Exported:d. M. yyyy H:mm}. Pro novější si řekni o nový soubor.";

            Ui.Text(Ui.Ellipsize(label(s), 250, 16), row.X + 552, row.Y + 11, 16, Theme.Text, true);
            Heatmap.Strip(new Rectangle(row.X + 812, row.Y + 6, 300, row.Height - 12), 30,
                d => me ? T.CountOn(d) : s.CountOn(d));

            if (!me)
            {
                var rb = new Rectangle(row.Right - 96, row.Y + 6, 88, row.Height - 12);
                bool confirm = _confirmRemove == s.Id;
                if (Ui.Button(rb, confirm ? "Opravdu?" : "Odebrat", confirm ? Theme.Red : Theme.Panel2, Theme.TextDim, 13)
                    && list.Contains(Ui.MousePos))
                {
                    if (confirm)
                    {
                        FriendSync.Remove(T, s);
                        _confirmRemove = null;
                    }
                    else _confirmRemove = s.Id;
                }
            }
        }
        if (T.P.Friends.Count == 0)
            Ui.TextCentered("Zatím žádní kamarádi – pošli jim svůj soubor a přidej si jejich.",
                new Rectangle(list.X, list.Y + rowH + 10, list.Width, 40), 17, Theme.TextDim);
        Ui.EndClip();
    }
}
