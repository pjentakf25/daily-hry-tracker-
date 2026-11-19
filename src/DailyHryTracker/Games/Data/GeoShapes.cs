using System.Text.Json;
using DailyHryTracker.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DailyHryTracker.Games.Data;

public enum GeoStatus { NotStarted, Loading, Ready, Failed }

/// <summary>
/// Obrysy zemí z Natural Earth (public domain, 1:110m). Stáhnou se jednou při prvním otevření
/// Globle/Worldle a uloží do složky s daty. Bez nich hry fungují v náhradním režimu (body na mapě, nápovědy).
/// </summary>
public static class GeoShapes
{
    public const string Url =
        "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_110m_admin_0_countries.geojson";

    static string CachePath => Path.Combine(SaveStore.Folder, "ne_110m_admin_0_countries.geojson");

    public static GeoStatus Status { get; private set; } = GeoStatus.NotStarted;
    public static string Error { get; private set; }

    /// <summary>ISO3 → seznam prstenců (lon, lat).</summary>
    public static Dictionary<string, List<Vector2[]>> Shapes { get; private set; } = new();

    /// <summary>Země, které v datech 1:110m chybí (jsou moc malé) – nejsou mezi tajnými zeměmi.</summary>
    public static readonly HashSet<string> TooSmall = new()
    {
        "AND", "LIE", "MCO", "SMR", "VAT", "SGP", "BHR", "MLT", "MUS", "CPV", "COM", "SYC", "MDV", "WSM", "TON", "BRB",
    };

    static readonly object Lock = new();

    public static void EnsureLoading()
    {
        lock (Lock)
        {
            if (Status is GeoStatus.Loading or GeoStatus.Ready) return;
            Status = GeoStatus.Loading;
        }
        Task.Run(async () =>
        {
            try
            {
                string json;
                if (!File.Exists(CachePath) && Environment.GetEnvironmentVariable("DHT_OFFLINE") != null)
                    throw new Exception("offline režim");
                if (File.Exists(CachePath)) json = await File.ReadAllTextAsync(CachePath);
                else
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("DailyHryTracker/1.0");
                    json = await http.GetStringAsync(Url);
                    Directory.CreateDirectory(SaveStore.Folder);
                    await File.WriteAllTextAsync(CachePath, json);
                }
                Shapes = Parse(json);
                Status = GeoStatus.Ready;
            }
            catch (Exception e)
            {
                Error = e.Message;
                Status = GeoStatus.Failed;
            }
        });
    }

    static Dictionary<string, List<Vector2[]>> Parse(string json)
    {
        var result = new Dictionary<string, List<Vector2[]>>();
        using var doc = JsonDocument.Parse(json);
        foreach (var f in doc.RootElement.GetProperty("features").EnumerateArray())
        {
            var props = f.GetProperty("properties");
            string iso = props.TryGetProperty("ADM0_A3", out var a) ? a.GetString() : null;
            if (string.IsNullOrEmpty(iso)) continue;
            var geom = f.GetProperty("geometry");
            var type = geom.GetProperty("type").GetString();
            var coords = geom.GetProperty("coordinates");
            var rings = new List<Vector2[]>();
            if (type == "Polygon") AddPolygon(coords, rings);
            else if (type == "MultiPolygon")
                foreach (var poly in coords.EnumerateArray()) AddPolygon(poly, rings);
            if (!result.TryGetValue(iso, out var list)) result[iso] = list = new List<Vector2[]>();
            list.AddRange(rings);
        }
        return result;
    }

    static void AddPolygon(JsonElement poly, List<Vector2[]> rings)
    {
        foreach (var ring in poly.EnumerateArray())
            rings.Add(ring.EnumerateArray().Select(p => new Vector2(p[0].GetSingle(), p[1].GetSingle())).ToArray());
    }

    public static bool Has(string iso) => Status == GeoStatus.Ready && Shapes.ContainsKey(iso);

    /// <summary>Nejmenší vzdálenost hranic (přes vrcholy obrysů) v km; bez tvarů vzdálenost středů.</summary>
    public static double BorderDistanceKm(Country a, Country b)
    {
        if (a == b) return 0;
        if (!Has(a.Iso3) || !Has(b.Iso3)) return Countries.DistanceKm(a, b);
        double best = double.MaxValue;
        var pa = Shapes[a.Iso3].SelectMany(r => r).ToArray();
        var pb = Shapes[b.Iso3].SelectMany(r => r).ToArray();
        foreach (var p in pa)
        foreach (var q in pb)
        {
            double d = Countries.DistanceKm(p.Y, p.X, q.Y, q.X);
            if (d < best) best = d;
        }
        return best < 15 ? 0 : best;
    }

    // ---------------- rasterizace ----------------

    /// <summary>Vyplní polygony (even-odd) do bufferu pomocí scanline. project: (lon,lat) → pixel.</summary>
    public static void Fill(List<Vector2[]> rings, Func<Vector2, Vector2> project, int w, int h, Action<int, int> plot)
    {
        var projected = rings.Select(r => r.Select(project).ToArray()).ToList();
        float minY = projected.Min(r => r.Min(p => p.Y)), maxY = projected.Max(r => r.Max(p => p.Y));
        int y0 = Math.Max(0, (int)MathF.Floor(minY)), y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY));
        var xs = new List<float>();
        for (int y = y0; y <= y1; y++)
        {
            float sy = y + 0.5f;
            xs.Clear();
            foreach (var ring in projected)
            {
                for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                {
                    var p1 = ring[i];
                    var p2 = ring[j];
                    if ((p1.Y > sy) != (p2.Y > sy))
                        xs.Add(p1.X + (sy - p1.Y) / (p2.Y - p1.Y) * (p2.X - p1.X));
                }
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
            {
                int xa = Math.Max(0, (int)MathF.Round(xs[k])), xb = Math.Min(w - 1, (int)MathF.Round(xs[k + 1]) - 1);
                for (int x = xa; x <= xb; x++) plot(x, y);
            }
        }
    }

    /// <summary>Silueta jedné země do textury (bílá na průhledné).</summary>
    public static Texture2D Silhouette(GraphicsDevice gd, string iso, int maxW, int maxH, Color color)
    {
        var rings = Shapes[iso];
        // přes 180. poledník (Rusko, Fidži…) – posuneme záporné délky
        float minLon = rings.Min(r => r.Min(p => p.X)), maxLon = rings.Max(r => r.Max(p => p.X));
        bool wrap = maxLon - minLon > 180;
        var fixedRings = rings.Select(r => r.Select(p => wrap && p.X < 0 ? new Vector2(p.X + 360, p.Y) : p).ToArray()).ToList();
        minLon = fixedRings.Min(r => r.Min(p => p.X));
        maxLon = fixedRings.Max(r => r.Max(p => p.X));
        float minLat = fixedRings.Min(r => r.Min(p => p.Y)), maxLat = fixedRings.Max(r => r.Max(p => p.Y));
        float k = MathF.Cos((minLat + maxLat) / 2 * MathF.PI / 180);
        float spanX = Math.Max(0.01f, (maxLon - minLon) * k), spanY = Math.Max(0.01f, maxLat - minLat);
        float scale = Math.Min(maxW / spanX, maxH / spanY);
        int w = Math.Max(1, (int)(spanX * scale)), h = Math.Max(1, (int)(spanY * scale));
        var data = new Color[w * h];
        Fill(fixedRings, p => new Vector2((p.X - minLon) * k * scale, (maxLat - p.Y) * scale), w, h,
            (x, y) => data[y * w + x] = color);
        var tex = new Texture2D(gd, w, h);
        tex.SetData(data);
        return tex;
    }
}

/// <summary>Mapa světa (ekvidistantní válcová projekce) s indexem zemí pro rychlé přebarvování.</summary>
public class WorldMap
{
    public const float LatTop = 84, LatBottom = -58;
    public readonly int W, H;
    readonly short[] _ids;
    readonly List<Country> _countries;
    public Texture2D Texture;
    readonly Color[] _pixels;

    public WorldMap(GraphicsDevice gd, int w)
    {
        W = w;
        H = (int)(w * (LatTop - LatBottom) / 360f);
        _ids = new short[W * H];
        _pixels = new Color[W * H];
        _countries = Countries.All;
        for (short i = 0; i < _countries.Count; i++)
        {
            if (!GeoShapes.Shapes.TryGetValue(_countries[i].Iso3, out var rings)) continue;
            short id = (short)(i + 1);
            GeoShapes.Fill(rings, Project, W, H, (x, y) => _ids[y * W + x] = id);
        }
        // zbylé tvary z dat, které nemáme v seznamu (např. Antarktida, sporná území) – jen jako pevnina
        var known = _countries.Select(c => c.Iso3).ToHashSet();
        foreach (var kv in GeoShapes.Shapes.Where(kv => !known.Contains(kv.Key)))
            GeoShapes.Fill(kv.Value, Project, W, H, (x, y) => { if (_ids[y * W + x] == 0) _ids[y * W + x] = -1; });
        Texture = new Texture2D(gd, W, H);
    }

    public Vector2 Project(Vector2 lonLat) =>
        new((lonLat.X + 180) / 360f * W, (LatTop - lonLat.Y) / (LatTop - LatBottom) * H);

    public Country At(int x, int y)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return null;
        int id = _ids[y * W + x];
        return id > 0 ? _countries[id - 1] : null;
    }

    /// <summary>Přebarví mapu: colorFor vrací barvu pro zemi, nebo null = výchozí pevnina.</summary>
    public void Recolor(Func<Country, Color?> colorFor, Color land, Color ocean, Color border)
    {
        var cache = new Color?[_countries.Count];
        for (int i = 0; i < _countries.Count; i++) cache[i] = colorFor(_countries[i]);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int i = y * W + x;
            int id = _ids[i];
            Color c = id == 0 ? ocean : id < 0 ? land : cache[id - 1] ?? land;
            if (id != 0 && ((x + 1 < W && _ids[i + 1] != id) || (y + 1 < H && _ids[i + W] != id)))
                c = border;
            _pixels[i] = c;
        }
        Texture.SetData(_pixels);
    }
}
