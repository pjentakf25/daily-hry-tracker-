using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker.Games;

public class PoopleGame : MiniGame
{
    const string Goal = "POOP";
    static Dictionary<string, int> _dist;     // vzdálenost slova od POOP
    static List<string> _sortedWords;

    readonly List<string> _ladder = new();
    string _current = "";
    int _par;
    List<string> _solution;
    bool _showSolution;

    static readonly Color Brown = new(150, 100, 60);

    static void BuildGraph()
    {
        if (_dist != null) return;
        _sortedWords = FourLetterWords.All.OrderBy(w => w, StringComparer.Ordinal).ToList();
        _dist = new Dictionary<string, int> { [Goal] = 0 };
        var q = new Queue<string>();
        q.Enqueue(Goal);
        while (q.Count > 0)
        {
            var w = q.Dequeue();
            foreach (var n in Neighbors(w))
                if (!_dist.ContainsKey(n))
                {
                    _dist[n] = _dist[w] + 1;
                    q.Enqueue(n);
                }
        }
    }

    static IEnumerable<string> Neighbors(string w)
    {
        var chars = w.ToCharArray();
        for (int i = 0; i < 4; i++)
        {
            char orig = chars[i];
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (c == orig) continue;
                chars[i] = c;
                var s = new string(chars);
                if (FourLetterWords.All.Contains(s)) yield return s;
            }
            chars[i] = orig;
        }
    }

    static bool OneApart(string a, string b) => a.Length == b.Length && a.Where((c, i) => c != b[i]).Count() == 1;

    protected override void Init()
    {
        BuildGraph();
        var candidates = _sortedWords.Where(w => _dist.TryGetValue(w, out var d) && d >= 4 && d <= 6).ToList();
        var start = candidates[Rng.Next(candidates.Count)];
        _ladder.Add(start);
        _par = _dist[start];
        _solution = new List<string> { start };
        var cur = start;
        while (cur != Goal)
        {
            cur = Neighbors(cur).Where(n => _dist.TryGetValue(n, out var d) && d == _dist[cur] - 1)
                .OrderBy(n => n, StringComparer.Ordinal).First();
            _solution.Add(cur);
        }
    }

    int Steps => _ladder.Count - 1;

    void Submit()
    {
        if (Finished) return;
        var w = _current.ToUpperInvariant();
        _current = "";
        if (w.Length != 4) { ShowToast("Slovo musí mít 4 písmena"); return; }
        if (!OneApart(_ladder[^1], w)) { ShowToast("Změň právě jedno písmeno"); return; }
        if (!FourLetterWords.All.Contains(w)) { ShowToast($"{w} neznám"); return; }
        if (_ladder.Contains(w)) { ShowToast("Tohle slovo už v žebříku máš"); return; }
        _ladder.Add(w);
        if (w == Goal)
        {
            int score = Math.Max(10, 100 - (Steps - _par) * 15);
            ShowToast(Steps == _par ? "Na par! Dokonalé." : "Hotovo!", 3);
            Finish(true, score, Steps, $"{Steps} kroků (par {_par})", $"{Steps} kroků, par {_par}");
        }
    }

    public override void GiveUp()
    {
        _showSolution = true;
        Finish(false, 0, Steps, "vzdáno", $"Nejkratší cesta má {_par} kroků");
    }

    public override void Frame(Rectangle area, float dt)
    {
        if (!Finished && TextField.Focused == null)
        {
            foreach (var ch in Ui.TypedChars)
                if (char.IsAsciiLetter(ch) && _current.Length < 4) _current += char.ToUpperInvariant(ch);
            if (Ui.BackspaceFired && _current.Length > 0) _current = _current[..^1];
            if (Ui.KeyPressed(Keys.Enter)) Submit();
        }

        var col = new Rectangle(area.Center.X - 300, area.Y + 10, 600, area.Height - 20);
        Ui.TextCentered($"Dojdi od {_ladder[0]} k {Goal}  ·  par: {_par} kroků  ·  tvoje kroky: {Steps}",
            new Rectangle(col.X, col.Y, col.Width, 26), 19, Theme.TextDim, true);

        const int tile = 50, gap = 6;
        int rowsVisible = 7;
        var shown = _ladder.Skip(Math.Max(0, _ladder.Count - rowsVisible)).ToList();
        int y = col.Y + 44;
        int gx = col.Center.X - (4 * tile + 3 * gap) / 2;
        string prev = _ladder.Count > shown.Count ? _ladder[_ladder.Count - shown.Count - 1] : null;
        foreach (var w in shown)
        {
            for (int i = 0; i < 4; i++)
            {
                var r = new Rectangle(gx + i * (tile + gap), y, tile, tile);
                bool changed = prev != null && prev[i] != w[i];
                var bg = w == Goal ? Brown : changed ? Theme.Accent : Theme.Panel2;
                Ui.RoundRect(r, bg, 6);
                Ui.TextCentered(w[i].ToString(), r, 26, Theme.Text, true);
            }
            prev = w;
            y += tile + gap;
        }

        if (!Finished)
        {
            for (int i = 0; i < 4; i++)
            {
                var r = new Rectangle(gx + i * (tile + gap), y + 6, tile, tile);
                Ui.Rect(r, Theme.Bg);
                Ui.Border(r, Theme.TextDim, 2);
                if (i < _current.Length) Ui.TextCentered(_current[i].ToString(), r, 26, Theme.Text, true);
            }
            y += tile + 24;
            int bx = col.Center.X - 250;
            if (Ui.Button(new Rectangle(bx, y, 160, 46), "Zpět o krok", null, null, 17, _ladder.Count > 1))
                _ladder.RemoveAt(_ladder.Count - 1);
            if (Ui.Button(new Rectangle(bx + 170, y, 160, 46), "Smazat", null, null, 17, _current.Length > 0))
                _current = "";
            if (Ui.Button(new Rectangle(bx + 340, y, 160, 46), "Potvrdit", Brown, Theme.Text, 17, _current.Length == 4))
                Submit();
            Ui.TextCentered("Piš na klávesnici, Enter potvrdí. Každé slovo musí být anglické slovo o 4 písmenech.",
                new Rectangle(col.X - 100, y + 60, col.Width + 200, 24), 16, Theme.TextDim);
        }
        else if (_showSolution || (Result.Won && Steps > _par))
        {
            Ui.TextCentered("Nejkratší cesta: " + string.Join(" → ", _solution),
                new Rectangle(col.X - 200, y + 20, col.Width + 400, 30), 20, Theme.Text, true);
        }

        DrawToast(area, dt, area.Bottom - 70);
    }
}
