using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DailyHryTracker.Games;

public class WorldleGame : MiniGame
{
    const int MaxGuesses = 6;
    Country _target;
    readonly List<(Country c, double km, double bearing, int pct)> _guesses = new();
    readonly AutoComplete<Country> _input = new(s => Countries.Suggest(s), c => $"{c.NameCs}  ({c.NameEn})", "Která je to země?");
    Texture2D _silhouette;

    protected override void Init()
    {
        var pool = Countries.All.Where(c => !GeoShapes.TooSmall.Contains(c.Iso3)).ToList();
        _target = pool[Rng.Next(pool.Count)];
        GeoShapes.EnsureLoading();
        _input.Field.Focus();
    }

    void Guess(Country c)
    {
        if (_guesses.Any(g => g.c == c))
        {
            ShowToast("Tuhle zemi už jsi zkoušel");
            return;
        }
        double km = Countries.DistanceKm(c, _target);
        int pct = c == _target ? 100 : (int)Math.Floor(Math.Max(0, 100 - km / 20037.5 * 100));
        _guesses.Add((c, km, Countries.Bearing(c, _target), pct));
        if (c == _target)
        {
            ShowToast("Správně!", 3);
            Finish(true, MaxGuesses + 1 - _guesses.Count, _guesses.Count, Summary(), $"{_target.NameCs} na {_guesses.Count}/6");
        }
        else if (_guesses.Count >= MaxGuesses)
            Finish(false, 0, 7, Summary(), $"Byla to země {_target.NameCs}");
    }

    string Summary() => string.Join(" ", _guesses.Select(g => g.pct + "%"));

    public override void GiveUp() => Finish(false, 0, 7, Summary(), $"Byla to země {_target.NameCs}");

    public static string Direction(double bearing)
    {
        string[] dirs = { "S", "SV", "V", "JV", "J", "JZ", "Z", "SZ" };
        return dirs[(int)Math.Round(bearing / 45) % 8];
    }

    public static void Arrow(Vector2 center, double bearing, float len, Color c)
    {
        float a = (float)((bearing - 90) * Math.PI / 180);
        var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
        var tip = center + dir * len / 2;
        var tail = center - dir * len / 2;
        Ui.Line(tail, tip, c, 3);
        var left = new Vector2(MathF.Cos(a + 2.5f), MathF.Sin(a + 2.5f)) * len * 0.4f;
        var right = new Vector2(MathF.Cos(a - 2.5f), MathF.Sin(a - 2.5f)) * len * 0.4f;
        Ui.Line(tip, tip + left, c, 3);
        Ui.Line(tip, tip + right, c, 3);
    }

    public override void Frame(Rectangle area, float dt)
    {
        var col = new Rectangle(area.Center.X - 330, area.Y + 10, 660, area.Height - 20);
        var shapeBox = new Rectangle(col.X, col.Y, col.Width, 290);
        Ui.Panel(shapeBox);

        if (_silhouette == null && GeoShapes.Has(_target.Iso3))
            _silhouette = GeoShapes.Silhouette(Ui.Gd, _target.Iso3, shapeBox.Width - 60, shapeBox.Height - 40, Color.White);

        if (_silhouette != null)
        {
            var r = new Rectangle(shapeBox.Center.X - _silhouette.Width / 2, shapeBox.Center.Y - _silhouette.Height / 2,
                _silhouette.Width, _silhouette.Height);
            Ui.Texture(_silhouette, r, Finished ? (Result.Won ? Theme.Green : Theme.Red) : Theme.Text);
        }
        else
        {
            string msg = GeoShapes.Status == GeoStatus.Loading ? "Načítám tvary zemí…" : "Silueta není k dispozici (offline) – hraješ podle nápověd";
            Ui.TextCentered(msg, new Rectangle(shapeBox.X, shapeBox.Y + 30, shapeBox.Width, 30), 18, Theme.TextDim);
            Ui.TextCentered($"Kontinent: {_target.Continent}", new Rectangle(shapeBox.X, shapeBox.Y + 110, shapeBox.Width, 40), 28, Theme.Text, true);
            if (_guesses.Count >= 2)
                Ui.TextCentered($"Začíná na: {_target.NameCs[0]}", new Rectangle(shapeBox.X, shapeBox.Y + 160, shapeBox.Width, 40), 24, Theme.Text);
            if (_guesses.Count >= 4)
                Ui.TextCentered($"Počet písmen: {_target.NameCs.Length}", new Rectangle(shapeBox.X, shapeBox.Y + 205, shapeBox.Width, 40), 24, Theme.Text);
        }

        int y = shapeBox.Bottom + 76;
        for (int i = 0; i < MaxGuesses; i++)
        {
            var row = new Rectangle(col.X, y + i * 44, col.Width, 38);
            if (i < _guesses.Count)
            {
                var g = _guesses[i];
                bool ok = g.c == _target;
                Ui.RoundRect(new Rectangle(row.X, row.Y, 330, row.Height), ok ? Theme.Green : Theme.Panel2, 6);
                Ui.Text(Ui.Ellipsize(g.c.NameCs, 310, 18), row.X + 12, row.Y + 8, 18, Theme.Text, true);
                Ui.RoundRect(new Rectangle(row.X + 338, row.Y, 130, row.Height), Theme.Panel2, 6);
                Ui.TextCentered($"{g.km:N0} km", new Rectangle(row.X + 338, row.Y, 130, row.Height), 17, Theme.Text);
                var dirBox = new Rectangle(row.X + 476, row.Y, 80, row.Height);
                Ui.RoundRect(dirBox, Theme.Panel2, 6);
                if (ok) Ui.TextCentered("cíl", dirBox, 17, Theme.Green, true);
                else
                {
                    Arrow(new Vector2(dirBox.X + 22, dirBox.Center.Y), g.bearing, 24, Theme.Text);
                    Ui.Text(Direction(g.bearing), dirBox.X + 42, dirBox.Y + 9, 16, Theme.TextDim);
                }
                var pctBox = new Rectangle(row.X + 564, row.Y, 96, row.Height);
                Ui.RoundRect(pctBox, Color.Lerp(Theme.Panel2, Theme.Green, g.pct / 100f), 6);
                Ui.TextCentered($"{g.pct} %", pctBox, 17, Theme.Text, true);
            }
            else
            {
                Ui.RoundRect(row, Theme.Panel, 6);
            }
        }

        if (Finished)
        {
            var r = new Rectangle(col.X, y + MaxGuesses * 44 + 6, col.Width, 44);
            Ui.TextCentered($"{_target.NameCs} ({_target.NameEn})", r, 24, Result.Won ? Theme.Green : Theme.Text, true);
        }

        DrawToast(area, dt, shapeBox.Bottom - 50);
        var picked = _input.Frame(new Rectangle(col.X, shapeBox.Bottom + 14, col.Width, 50), !Finished, out bool unmatched);
        if (unmatched) ShowToast("Takovou zemi neznám");
        if (picked != null && !Finished) Guess(picked);
    }
}
