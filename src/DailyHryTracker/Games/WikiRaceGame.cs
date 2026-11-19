using System.Text.Json;
using DailyHryTracker.Core;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

/// <summary>WikiRace nad anglickou Wikipedií – články se stahují přes MediaWiki API a zobrazují přímo v aplikaci.</summary>
public class WikiRaceGame : MiniGame
{
    static readonly (string from, string to)[] Pairs =
    {
        ("Banana", "Albert Einstein"), ("Pizza", "Moon"), ("Prague", "Basketball"), ("Coffee", "Napoleon"),
        ("LEGO", "Ancient Egypt"), ("Volcano", "Shakespeare"), ("Cat", "Bitcoin"), ("Chess", "Mount Everest"),
        ("Tea", "World War I"), ("Penguin", "Beethoven"), ("Minecraft", "Roman Empire"), ("Chocolate", "Saturn"),
        ("Bicycle", "Leonardo da Vinci"), ("Honey", "Olympic Games"), ("Pirate", "Internet"), ("Football", "DNA"),
        ("Guitar", "Antarctica"), ("Shark", "Czech Republic"), ("Bread", "Moon landing"), ("Dinosaur", "Jazz"),
        ("Tomato", "Isaac Newton"), ("Train", "Mozart"), ("Ice hockey", "Pyramid"), ("Beer", "Black hole"),
        ("Dog", "Great Wall of China"), ("Batman", "Vikings"), ("Rainbow", "Elvis Presley"), ("Potato", "Titanic"),
        ("Hamburger", "Mars"), ("Zebra", "Wolfgang Amadeus Mozart"), ("Pokémon", "French Revolution"),
        ("Harry Potter", "Volcano"), ("Sushi", "Space Shuttle"), ("Elephant", "Bitcoin"), ("Castle", "Michael Jordan"),
        ("Wine", "Apollo 11"), ("Tiger", "Charles Darwin"), ("Snow", "Leonardo DiCaprio"), ("Cheese", "Samurai"),
        ("Rome", "Smartphone"),
    };

    class Article
    {
        public string Title;
        public string Extract;
        public List<string> Links = new();
        public string Error;
    }

    static readonly HttpClient Http = CreateHttp();

    static HttpClient CreateHttp()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("DailyHryTracker/1.0 (desktop game)");
        return h;
    }

    string _startTitle, _targetTitle;
    Article _current, _target;
    Task<Article> _loading;
    readonly List<string> _path = new();
    int _clicks;
    float _elapsed;
    int _scroll;
    readonly TextField _filter = new() { Placeholder = "Filtrovat odkazy…", MaxLength = 40 };
    bool _started;

    protected override void Init()
    {
        var p = Pairs[Rng.Next(Pairs.Length)];
        if (!Daily && Rng.Next(2) == 0) p = (p.to, p.from);
        _startTitle = p.from;
        _targetTitle = p.to;
        _loading = Fetch(_targetTitle, false);
    }

    static async Task<Article> Fetch(string title, bool withLinks)
    {
        var a = new Article { Title = title };
        try
        {
            if (Environment.GetEnvironmentVariable("DHT_OFFLINE") != null) throw new Exception("offline režim");
            string baseUrl = "https://en.wikipedia.org/w/api.php?action=query&format=json&formatversion=2&redirects=1";
            var t = Uri.EscapeDataString(title);
            var json = await Http.GetStringAsync($"{baseUrl}&prop=extracts&exintro=1&explaintext=1&exsentences=3&titles={t}");
            using (var doc = JsonDocument.Parse(json))
            {
                var page = doc.RootElement.GetProperty("query").GetProperty("pages")[0];
                if (page.TryGetProperty("missing", out _)) { a.Error = "Článek neexistuje."; return a; }
                a.Title = page.GetProperty("title").GetString();
                a.Extract = page.TryGetProperty("extract", out var ex) ? ex.GetString() : "";
            }
            if (!withLinks) return a;
            string cont = null;
            for (int i = 0; i < 6; i++)
            {
                var url = $"{baseUrl}&prop=links&pllimit=max&plnamespace=0&titles={Uri.EscapeDataString(a.Title)}";
                if (cont != null) url += "&plcontinue=" + Uri.EscapeDataString(cont);
                var lj = await Http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(lj);
                var page = doc.RootElement.GetProperty("query").GetProperty("pages")[0];
                if (page.TryGetProperty("links", out var links))
                    foreach (var l in links.EnumerateArray())
                        a.Links.Add(l.GetProperty("title").GetString());
                if (doc.RootElement.TryGetProperty("continue", out var c) && c.TryGetProperty("plcontinue", out var pc))
                    cont = pc.GetString();
                else break;
            }
            a.Links = a.Links.Distinct().ToList();
        }
        catch (Exception e)
        {
            a.Error = "Nepodařilo se načíst Wikipedii: " + e.Message;
        }
        return a;
    }

    void Navigate(string title, bool countClick = true)
    {
        if (_loading != null) return;
        if (countClick) _clicks++;
        _loading = Fetch(title, true);
        _filter.Value = "";
        _scroll = 0;
    }

    public override void GiveUp() => Finish(false, 0, _clicks, "vzdáno", $"Cíl byl {_targetTitle}");

    public override void Frame(Rectangle area, float dt)
    {
        // dokončené načítání
        if (_loading is { IsCompleted: true })
        {
            var a = _loading.Result;
            _loading = null;
            if (_target == null)
            {
                _target = a;
                if (a.Error == null) _targetTitle = a.Title;
            }
            else
            {
                if (a.Error != null)
                {
                    ShowToast(a.Error, 4);
                    if (_clicks > 0 && _current != null) _clicks--;
                }
                else
                {
                    _current = a;
                    _path.Add(a.Title);
                    if (string.Equals(a.Title, _targetTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        int score = Math.Max(10, 100 - Math.Max(0, _clicks - 3) * 10);
                        ShowToast("Cíl dosažen!", 4);
                        Finish(true, score, _clicks, $"{_clicks} kliků, {_elapsed:0} s",
                            $"{_clicks} kliků za {FormatTime(_elapsed)}");
                    }
                }
            }
        }

        var side = new Rectangle(area.Right - 360, area.Y + 10, 340, area.Height - 20);
        var main = new Rectangle(area.X + 20, area.Y + 10, side.X - area.X - 40, area.Height - 20);

        // cíl
        Ui.Panel(side);
        Ui.Text("CÍL", side.X + 18, side.Y + 14, 14, Theme.TextDim, true);
        Ui.TextWrapped(_targetTitle, new Rectangle(side.X + 18, side.Y + 34, side.Width - 36, 60), 24, Theme.Gold, true, 2);
        if (_target?.Extract != null)
            Ui.TextWrapped(_target.Extract, new Rectangle(side.X + 18, side.Y + 100, side.Width - 36, 200), 15, Theme.TextDim, false, 8);
        int sy = side.Y + 300;
        Ui.Text($"Kliky: {_clicks}", side.X + 18, sy, 20, Theme.Text, true);
        Ui.Text($"Čas: {FormatTime(_elapsed)}", side.X + 180, sy, 20, Theme.Text, true);
        sy += 40;
        Ui.Text("Cesta:", side.X + 18, sy, 15, Theme.TextDim, true);
        sy += 22;
        foreach (var t in _path.TakeLast(12))
        {
            Ui.Text(Ui.Ellipsize("• " + t, side.Width - 36, 15), side.X + 18, sy, 15, Theme.Text);
            sy += 21;
        }

        if (!_started)
        {
            var box = new Rectangle(main.X, main.Y + 40, main.Width, 300);
            Ui.Panel(box);
            Ui.TextCentered($"{_startTitle}  →  {_targetTitle}", new Rectangle(box.X, box.Y + 40, box.Width, 40), 30, Theme.Text, true);
            Ui.TextWrapped(Info.Help, new Rectangle(box.X + 40, box.Y + 110, box.Width - 80, 80), 18, Theme.TextDim);
            bool ready = _target != null;
            if (_target?.Error != null)
                Ui.TextCentered(_target.Error, new Rectangle(box.X, box.Y + 190, box.Width, 30), 17, Theme.Red);
            if (Ui.Button(new Rectangle(box.Center.X - 110, box.Bottom - 80, 220, 54), ready ? "Start" : "Připojuji…",
                    Theme.Accent, Theme.Text, 22, ready && _target.Error == null))
            {
                _started = true;
                Navigate(_startTitle, false);
            }
            if (_target?.Error != null && Ui.Button(new Rectangle(box.Center.X - 110, box.Bottom + 20, 220, 44), "Zkusit znovu"))
            {
                _target = null;
                _loading = Fetch(_targetTitle, false);
            }
            DrawToast(area, dt, area.Bottom - 60);
            return;
        }

        if (!Finished && _current != null) _elapsed += dt;

        if (_current == null)
        {
            Ui.TextCentered("Načítám článek…", main, 22, Theme.TextDim);
            DrawToast(area, dt, area.Bottom - 60);
            return;
        }

        Ui.Text(Ui.Ellipsize(_current.Title, main.Width - 140, 30), main.X, main.Y, 30, Theme.Text, true);
        if (_loading != null) Ui.TextRight("načítám…", main.Right, main.Y + 8, 16, Theme.Accent);
        else if (_path.Count > 1 && !Finished &&
                 Ui.Button(new Rectangle(main.Right - 110, main.Y, 110, 36), "← Zpět", null, null, 16))
        {
            _path.RemoveAt(_path.Count - 1);
            var back = _path[^1];
            _path.RemoveAt(_path.Count - 1);
            Navigate(back);
        }
        float eh = Ui.TextWrapped(_current.Extract ?? "", new Rectangle(main.X, main.Y + 46, main.Width, 110), 16, Theme.TextDim, false, 5);

        int fy = main.Y + 56 + (int)eh;
        _filter.Frame(new Rectangle(main.X, fy, 360, 40), 17, !Finished);
        var links = _current.Links;
        var f = TextUtil.Normalize(_filter.Value);
        if (f.Length > 0) links = links.Where(l => TextUtil.Normalize(l).Contains(f)).ToList();
        Ui.Text($"{links.Count} odkazů", main.X + 376, fy + 10, 16, Theme.TextDim);

        var listRect = new Rectangle(main.X, fy + 52, main.Width, main.Bottom - fy - 52);
        const int cols = 3, rowH = 30;
        int colW = listRect.Width / cols;
        int totalRows = (links.Count + cols - 1) / cols;
        int visibleRows = listRect.Height / rowH;
        if (Ui.Hover(listRect)) _scroll = Math.Clamp(_scroll - Ui.Wheel * 3, 0, Math.Max(0, totalRows - visibleRows));
        Ui.BeginClip(listRect);
        for (int row = _scroll; row < Math.Min(totalRows, _scroll + visibleRows + 1); row++)
        for (int c = 0; c < cols; c++)
        {
            int i = row * cols + c;
            if (i >= links.Count) break;
            var r = new Rectangle(listRect.X + c * colW, listRect.Y + (row - _scroll) * rowH, colW - 8, rowH - 4);
            bool hover = Ui.Hover(r) && listRect.Contains(Ui.MousePos);
            if (hover) Ui.RoundRect(r, Theme.Panel2, 4);
            Ui.Text(Ui.Ellipsize(links[i], r.Width - 10, 16), r.X + 6, r.Y + 4, 16, hover ? Theme.Text : new Color(88, 166, 255));
            if (!Finished && _loading == null && hover && Ui.Clicked(r)) Navigate(links[i]);
        }
        Ui.EndClip();
        if (totalRows > visibleRows)
        {
            float t = _scroll / (float)Math.Max(1, totalRows - visibleRows);
            int barH = Math.Max(30, listRect.Height * visibleRows / totalRows);
            Ui.RoundRect(new Rectangle(listRect.Right - 4, listRect.Y + (int)((listRect.Height - barH) * t), 4, barH), Theme.Line, 2);
        }

        DrawToast(area, dt, area.Bottom - 60);
    }

    static string FormatTime(float s) => $"{(int)s / 60}:{(int)s % 60:00}";
}
