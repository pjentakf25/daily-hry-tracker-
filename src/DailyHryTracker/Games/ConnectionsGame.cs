using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

public class ConnectionsGame : MiniGame
{
    class Group
    {
        public ConnCategory Cat;
        public List<string> Words;
    }

    readonly List<Group> _groups = new();
    readonly List<string> _tiles = new();
    readonly HashSet<string> _selected = new();
    readonly List<Group> _solved = new();
    readonly List<string> _guessHistory = new();
    int _mistakes;
    const int MaxMistakes = 4;

    public static readonly Color[] LevelColors =
    {
        new(249, 223, 109), new(160, 195, 90), new(176, 196, 239), new(186, 129, 197)
    };

    protected override void Init()
    {
        for (int attempt = 0; attempt < 500 && _groups.Count < 4; attempt++)
        {
            _groups.Clear();
            var used = new HashSet<string>();
            var pool = ConnectionsData.All.ToList();
            TextUtil.Shuffle(pool, Rng);
            for (int level = 0; level < 4; level++)
            {
                // preferujeme jednu kategorii z každé obtížnosti, ale vezmeme cokoliv, co sedí
                var candidates = pool.Where(c => c.Level == level).Concat(pool.Where(c => c.Level != level));
                foreach (var cat in candidates)
                {
                    if (_groups.Any(g => g.Cat == cat)) continue;
                    var available = cat.Words.Where(w => !used.Contains(w) &&
                                                         !_groups.Any(g => g.Cat.Words.Contains(w))).ToList();
                    if (available.Count < 4) continue;
                    TextUtil.Shuffle(available, Rng);
                    var chosen = available.Take(4).ToList();
                    // žádné vybrané slovo dřívějších skupin nesmí patřit do této kategorie (aby šlo jednoznačně vyřešit)
                    if (_groups.Any(g => g.Words.Any(w => cat.Words.Contains(w)))) continue;
                    _groups.Add(new Group { Cat = cat, Words = chosen });
                    foreach (var w in chosen) used.Add(w);
                    break;
                }
            }
        }
        _groups.Sort((a, b) => a.Cat.Level.CompareTo(b.Cat.Level));
        foreach (var g in _groups) _tiles.AddRange(g.Words);
        TextUtil.Shuffle(_tiles, Rng);
    }

    void Submit()
    {
        if (_selected.Count != 4) return;
        var key = string.Join(",", _selected.OrderBy(x => x));
        if (_guessHistory.Contains(key))
        {
            ShowToast("Tohle už jsi zkoušel");
            return;
        }
        _guessHistory.Add(key);
        var match = _groups.FirstOrDefault(g => !_solved.Contains(g) && g.Words.All(_selected.Contains));
        if (match != null)
        {
            _solved.Add(match);
            _tiles.RemoveAll(match.Words.Contains);
            _selected.Clear();
            if (_solved.Count == 4)
            {
                string[] praise = { "Perfektní!", "Skvělé!", "Pěkně!", "Uf, tak tak!" };
                ShowToast(praise[Math.Min(_mistakes, 3)], 3);
                Finish(true, MaxMistakes - _mistakes, _mistakes, Summary(), $"Vyřešeno, chyb: {_mistakes}");
            }
            return;
        }
        _mistakes++;
        bool oneAway = _groups.Any(g => !_solved.Contains(g) && g.Words.Count(_selected.Contains) == 3);
        ShowToast(oneAway ? "Jedno chybí!" : "Vedle");
        if (_mistakes >= MaxMistakes) Reveal();
    }

    void Reveal()
    {
        foreach (var g in _groups.Where(g => !_solved.Contains(g)).ToList()) _solved.Add(g);
        _tiles.Clear();
        _selected.Clear();
        Finish(false, 0, _mistakes, Summary(), "Došly pokusy");
    }

    public override void GiveUp()
    {
        _mistakes = MaxMistakes;
        Reveal();
    }

    string Summary() => string.Join("", _solved.Select(g => g.Cat.Level)) + $" m{_mistakes}";

    public override void Frame(Rectangle area, float dt)
    {
        const int tw = 180, th = 78, gap = 10;
        int gridW = 4 * tw + 3 * gap;
        int x0 = area.Center.X - gridW / 2;
        int y = area.Y + 30;

        Ui.TextCentered("Vytvoř čtyři skupiny po čtyřech!", new Rectangle(area.X, area.Y, area.Width, 24), 18, Theme.TextDim);

        foreach (var g in _solved)
        {
            var r = new Rectangle(x0, y, gridW, th);
            Ui.RoundRect(r, LevelColors[g.Cat.Level], 8);
            Ui.TextCentered(g.Cat.Name, new Rectangle(r.X, r.Y + 10, r.Width, 26), 20, Theme.Bg, true);
            Ui.TextCentered(string.Join(", ", g.Words), new Rectangle(r.X, r.Y + 42, r.Width, 22), 17, Theme.Bg);
            y += th + gap;
        }

        for (int i = 0; i < _tiles.Count; i++)
        {
            var word = _tiles[i];
            var r = new Rectangle(x0 + i % 4 * (tw + gap), y + i / 4 * (th + gap), tw, th);
            bool sel = _selected.Contains(word);
            var bg = sel ? new Color(90, 89, 78) : new Color(239, 239, 230);
            if (Ui.Hover(r) && !Finished) bg = Theme.Lighten(bg, sel ? 0.1f : -0.08f);
            Ui.RoundRect(r, bg, 8);
            int size = word.Length > 10 ? 15 : word.Length > 8 ? 17 : 20;
            Ui.TextCentered(word, r, size, sel ? Color.White : Theme.Bg, true);
            if (!Finished && Ui.Clicked(r))
            {
                if (sel) _selected.Remove(word);
                else if (_selected.Count < 4) _selected.Add(word);
            }
        }

        int by = area.Y + 30 + 4 * (th + gap) + 20;
        // chyby
        string ml = "Zbývající chyby:";
        var mw = Ui.Measure(ml, 18).X;
        float mx = area.Center.X - (mw + 4 * 26) / 2;
        Ui.Text(ml, mx, by, 18, Theme.TextDim);
        for (int i = 0; i < MaxMistakes; i++)
            Ui.Circle(new Vector2(mx + mw + 20 + i * 26, by + 11), 8,
                i < MaxMistakes - _mistakes ? new Color(90, 89, 78) : Theme.Panel2);

        by += 44;
        int bw = 160;
        int bx = area.Center.X - (3 * bw + 2 * 16) / 2;
        if (Ui.Button(new Rectangle(bx, by, bw, 48), "Zamíchat", null, null, 18, !Finished))
            TextUtil.Shuffle(_tiles, Rng);
        if (Ui.Button(new Rectangle(bx + bw + 16, by, bw, 48), "Zrušit výběr", null, null, 18, !Finished && _selected.Count > 0))
            _selected.Clear();
        if (Ui.Button(new Rectangle(bx + 2 * (bw + 16), by, bw, 48), "Odeslat", Theme.Text, Theme.Bg, 18,
                !Finished && _selected.Count == 4))
            Submit();

        DrawToast(area, dt, area.Y + 30 + 4 * (th + gap) / 2);
    }
}
