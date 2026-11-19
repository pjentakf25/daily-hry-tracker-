using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

public class SpotleGame : MiniGame
{
    const int MaxGuesses = 10;
    Movie _target;
    readonly List<Movie> _guesses = new();
    readonly AutoComplete<Movie> _input = new(s => Movies.Suggest(s), m => $"{m.Title} ({m.Year})", "Napiš název filmu…");

    static readonly Color CMatch = new(30, 185, 84);
    static readonly Color CClose = new(210, 166, 32);
    static readonly Color CMiss = new(48, 54, 61);

    protected override void Init()
    {
        _target = Movies.All[Rng.Next(Movies.All.Count)];
        _input.Field.Focus();
    }

    void Guess(Movie m)
    {
        if (_guesses.Contains(m))
        {
            ShowToast("Tenhle film už jsi zkoušel");
            return;
        }
        _guesses.Add(m);
        if (m == _target)
        {
            ShowToast("Správně!", 3);
            Finish(true, MaxGuesses + 1 - _guesses.Count, _guesses.Count, $"{_guesses.Count}/{MaxGuesses}", $"{_target.Title} na {_guesses.Count}. pokus");
        }
        else if (_guesses.Count >= MaxGuesses)
            Finish(false, 0, MaxGuesses + 1, $"X/{MaxGuesses}", $"Film: {_target.Title}");
    }

    public override void GiveUp() => Finish(false, 0, MaxGuesses + 1, $"X/{MaxGuesses}", $"Film: {_target.Title}");

    static (Color c, string arrow) Num(double guess, double target, double close)
    {
        if (Math.Abs(guess - target) < 0.001) return (CMatch, "");
        var c = Math.Abs(guess - target) <= close ? CClose : CMiss;
        return (c, target > guess ? " ▲" : " ▼");
    }

    public override void Frame(Rectangle area, float dt)
    {
        var col = new Rectangle(area.X + 30, area.Y + 10, area.Width - 60, area.Height - 20);
        string[] headers = { "Film", "Rok", "Žánr", "Režisér", "Země", "Délka", "Hodnocení" };
        int[] widths = { 330, 100, 140, 250, 150, 110, 130 };
        int y = col.Y + 70;
        int x = col.X;
        for (int i = 0; i < headers.Length; i++)
        {
            Ui.Text(headers[i], x + 6, y, 15, Theme.TextDim, true);
            x += widths[i] + 6;
        }
        y += 24;
        Ui.TextRight($"Pokus {_guesses.Count}/{MaxGuesses}", col.Right, col.Y + 70, 15, Theme.TextDim);

        foreach (var m in Enumerable.Reverse(_guesses))
        {
            x = col.X;
            var cells = new (string text, Color c)[]
            {
                (m.Title, m == _target ? CMatch : CMiss),
                Cell(m.Year.ToString(), Num(m.Year, _target.Year, 5)),
                (m.Genre, m.Genre == _target.Genre ? CMatch : CMiss),
                (m.Director, m.Director == _target.Director ? CMatch : CMiss),
                (m.Country, m.Country == _target.Country ? CMatch : CMiss),
                Cell($"{m.Runtime} min", Num(m.Runtime, _target.Runtime, 15)),
                Cell(m.Rating.ToString("0.0"), Num(m.Rating, _target.Rating, 0.3)),
            };
            for (int i = 0; i < cells.Length; i++)
            {
                var r = new Rectangle(x, y, widths[i], 50);
                Ui.RoundRect(r, cells[i].c, 6);
                int size = 16;
                var lines = Ui.Wrap(cells[i].text, widths[i] - 12, size, true);
                if (lines.Count == 1) Ui.TextCentered(lines[0], r, size, Theme.Text, true);
                else Ui.TextWrapped(cells[i].text, new Rectangle(r.X + 6, r.Y + 4, r.Width - 12, r.Height), 14, Theme.Text, true, 2);
                x += widths[i] + 6;
            }
            y += 56;
        }

        if (_guesses.Count == 0)
        {
            Ui.TextWrapped("Hledá se jeden film z databáze ~140 známých filmů (světových i českých). " +
                           "Zelená = shoda, žlutá = blízko (rok ±5, délka ±15 min, hodnocení ±0,3), šipka ukazuje, kterým směrem hledat.",
                new Rectangle(col.X, y + 10, 900, 80), 18, Theme.TextDim);
        }

        if (Finished)
        {
            var r = new Rectangle(col.X + 520, col.Y, col.Width - 520, 50);
            Ui.RoundRect(r, Result.Won ? CMatch : Theme.Panel2, 8);
            Ui.TextCentered($"{_target.Title} ({_target.Year}) – {_target.Director}", r, 18, Theme.Text, true);
        }

        DrawToast(area, dt, area.Bottom - 60);
        var picked = _input.Frame(new Rectangle(col.X, col.Y, 500, 50), !Finished, out bool unmatched);
        if (unmatched) ShowToast("Tenhle film v databázi není");
        if (picked != null && !Finished) Guess(picked);
    }

    static (string, Color) Cell(string text, (Color c, string arrow) n) => (text + n.arrow, n.c);
}
