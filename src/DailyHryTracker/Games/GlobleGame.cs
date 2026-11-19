using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

public class GlobleGame : MiniGame
{
    Country _target;
    readonly List<(Country c, double km)> _guesses = new();
    readonly AutoComplete<Country> _input = new(s => Countries.Suggest(s), c => $"{c.NameCs}  ({c.NameEn})", "Napiš zemi…");
    static WorldMap _map;
    bool _dirty = true;

    static readonly Color Ocean = new(18, 32, 52);
    static readonly Color Land = new(60, 66, 74);

    protected override void Init()
    {
        var pool = Countries.All.Where(c => !GeoShapes.TooSmall.Contains(c.Iso3)).ToList();
        _target = pool[Rng.Next(pool.Count)];
        GeoShapes.EnsureLoading();
        _input.Field.Focus();
    }

    public static Color Heat(double km)
    {
        float t = (float)Math.Clamp(km / 9000.0, 0, 1);
        t = MathF.Sqrt(t);
        return t < 0.5f
            ? Color.Lerp(new Color(120, 0, 10), new Color(240, 90, 40), t * 2)
            : Color.Lerp(new Color(240, 90, 40), new Color(255, 236, 190), (t - 0.5f) * 2);
    }

    void Guess(Country c)
    {
        if (_guesses.Any(g => g.c == c))
        {
            ShowToast("Tuhle zemi už jsi zkoušel");
            return;
        }
        double km = c == _target ? 0 : GeoShapes.BorderDistanceKm(c, _target);
        _guesses.Add((c, km));
        _dirty = true;
        if (c == _target)
        {
            int n = _guesses.Count;
            ShowToast($"Správně! {_target.NameCs}", 4);
            Finish(true, Math.Max(1, 100 - (n - 1) * 5), n, $"{n} pokusů", $"{_target.NameCs} na {n}. pokus");
        }
    }

    public override void GiveUp()
    {
        _guesses.Add((_target, 0));
        _dirty = true;
        Finish(false, 0, _guesses.Count, "vzdáno", $"Hledaná země: {_target.NameCs}");
    }

    Color? ColorFor(Country c)
    {
        if (c == _target && _guesses.Any(g => g.c == c)) return Finished && !Result.Won ? Theme.Red : Theme.Green;
        foreach (var g in _guesses)
            if (g.c == c) return Heat(g.km);
        return null;
    }

    public override void Frame(Rectangle area, float dt)
    {
        var mapRect = new Rectangle(area.X + 20, area.Y + 20, 900, (int)(900 * (WorldMap.LatTop - WorldMap.LatBottom) / 360f));
        Ui.Panel(new Rectangle(mapRect.X - 4, mapRect.Y - 4, mapRect.Width + 8, mapRect.Height + 8), Ocean, 8);

        if (GeoShapes.Status == GeoStatus.Ready && _map == null)
            _map = new WorldMap(Ui.Gd, 900);

        if (_map != null)
        {
            if (_dirty)
            {
                _map.Recolor(ColorFor, Land, Ocean, new Color(25, 28, 34));
                _dirty = false;
            }
            Ui.Texture(_map.Texture, mapRect);
            if (Ui.Hover(mapRect))
            {
                var c = _map.At(Ui.MousePos.X - mapRect.X, Ui.MousePos.Y - mapRect.Y);
                var g = _guesses.FirstOrDefault(x => x.c == c);
                if (g.c != null) Ui.Tooltip = $"{c.NameCs}: {g.km:N0} km";
            }
        }
        else
        {
            DrawFallbackMap(mapRect);
        }

        // legenda
        int ly = mapRect.Bottom + 20;
        Ui.Text("Daleko", mapRect.X, ly, 15, Theme.TextDim);
        for (int i = 0; i < 40; i++)
            Ui.Rect(mapRect.X + 60 + i * 8, ly + 3, 8, 14, Heat(9000 - i * 225));
        Ui.Text("Blízko", mapRect.X + 60 + 330, ly, 15, Theme.TextDim);
        string status = GeoShapes.Status switch
        {
            GeoStatus.Loading => "Stahuji obrysy zemí (jen poprvé)…",
            GeoStatus.Failed => "Obrysy zemí nejsou k dispozici – zobrazuji středy zemí.",
            _ => "Vzdálenost se měří mezi hranicemi zemí.",
        };
        Ui.TextRight(status, mapRect.Right, ly, 15, Theme.TextDim);

        if (Finished)
        {
            var r = new Rectangle(mapRect.X, ly + 40, mapRect.Width, 70);
            Ui.Panel(r, Result.Won ? Theme.Green * 0.8f : Theme.Panel2);
            Ui.TextCentered($"Hledaná země: {_target.NameCs} ({_target.NameEn})", r, 26, Theme.Text, true);
        }

        // pravý panel
        var side = new Rectangle(mapRect.Right + 30, area.Y + 20, area.Right - mapRect.Right - 50, area.Height - 40);
        Ui.Text($"Pokusy: {_guesses.Count}", side.X, side.Y + 70, 18, Theme.TextDim, true);
        var closest = _guesses.OrderBy(g => g.km).ToList();
        int y = side.Y + 100;
        foreach (var g in closest.Take(14))
        {
            var row = new Rectangle(side.X, y, side.Width, 34);
            Ui.RoundRect(row, Theme.Panel, 6);
            Ui.Rect(new Rectangle(row.X, row.Y, 8, row.Height), g.c == _target ? Theme.Green : Heat(g.km));
            Ui.Text(Ui.Ellipsize(g.c.NameCs, row.Width - 120, 17), row.X + 18, row.Y + 7, 17, Theme.Text);
            Ui.TextRight(g.c == _target ? "cíl" : g.km == 0 ? "soused" : $"{g.km:N0} km", row.Right - 10, row.Y + 7, 17, Theme.TextDim);
            y += 40;
        }
        if (closest.Count > 14) Ui.Text($"… a dalších {closest.Count - 14}", side.X, y, 15, Theme.TextDim);

        DrawToast(area, dt, area.Y + 30);

        var picked = _input.Frame(new Rectangle(side.X, side.Y, side.Width, 50), !Finished, out bool unmatched);
        if (unmatched) ShowToast("Takovou zemi neznám");
        if (picked != null && !Finished) Guess(picked);
    }

    void DrawFallbackMap(Rectangle r)
    {
        for (int lon = -150; lon <= 150; lon += 30)
            Ui.Rect(r.X + (lon + 180) / 360f * r.Width, r.Y, 1, r.Height, Theme.Line * 0.5f);
        for (int lat = -40; lat <= 80; lat += 20)
            Ui.Rect(r.X, r.Y + (WorldMap.LatTop - lat) / (WorldMap.LatTop - WorldMap.LatBottom) * r.Height, r.Width, 1, Theme.Line * 0.5f);
        foreach (var c in Countries.All)
        {
            var p = new Vector2(r.X + (float)(c.Lon + 180) / 360f * r.Width,
                r.Y + (float)(WorldMap.LatTop - c.Lat) / (WorldMap.LatTop - WorldMap.LatBottom) * r.Height);
            var col = ColorFor(c);
            Ui.Circle(p, col.HasValue ? 9 : 4, col ?? Land);
            if (col.HasValue && Vector2.Distance(p, Ui.MousePos.ToVector2()) < 10) Ui.Tooltip = c.NameCs;
        }
    }
}
