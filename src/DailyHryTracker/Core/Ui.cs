using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker.Core;

/// <summary>
/// Minimalistické immediate-mode UI: kreslení obdélníků, textu, tlačítek a čtení vstupu.
/// Vše se kreslí i vyhodnocuje v jednom průchodu (Frame).
/// </summary>
public static class Ui
{
    public const int Width = 1280;
    public const int Height = 800;

    public static GraphicsDevice Gd;
    public static SpriteBatch Sb;
    static Texture2D _pixel;
    static Texture2D _circle;
    const int CircleSize = 128;

    static FontSystem _regular;
    static FontSystem _bold;

    // ---- vstup ----
    public static MouseState Mouse, PrevMouse;
    public static KeyboardState Kb, PrevKb;
    public static readonly List<char> TypedChars = new();
    static readonly List<char> _pendingChars = new();
    public static int Wheel;
    public static Point MousePos => Mouse.Position;
    /// <summary>Levé tlačítko bylo v tomhle snímku stisknuto (včetně rychlých ťuknutí, viz <see cref="Sdl.WatchClicks"/>).</summary>
    public static bool LeftPressed { get; private set; }
    public static bool RightPressed => Mouse.RightButton == ButtonState.Pressed && PrevMouse.RightButton == ButtonState.Released;
    /// <summary>Když už klik "spotřeboval" nějaký prvek (např. modální okno), další prvky ho nedostanou.</summary>
    public static bool ClickConsumed;
    public static string Tooltip;
    public static float Time;
    static float _backRepeat;
    public static bool BackspaceFired;

    // rozdílné přidržení Ctrl/Cmd pro kopírování/vkládání
    public static bool CtrlDown => Kb.IsKeyDown(Keys.LeftControl) || Kb.IsKeyDown(Keys.RightControl) ||
                                   Kb.IsKeyDown(Keys.LeftWindows) || Kb.IsKeyDown(Keys.RightWindows);

    public static void Init(GraphicsDevice gd, GameWindow window)
    {
        Gd = gd;
        Sb = new SpriteBatch(gd);
        try { Sdl.WatchClicks(); } catch { /* bez SDL událostí zůstává obyčejné snímání myši */ }
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _circle = new Texture2D(gd, CircleSize, CircleSize);
        var data = new Color[CircleSize * CircleSize];
        float r = CircleSize / 2f;
        for (int y = 0; y < CircleSize; y++)
        for (int x = 0; x < CircleSize; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            float a = Math.Clamp(r - d, 0f, 1f);
            data[y * CircleSize + x] = Color.White * a;
        }
        _circle.SetData(data);

        FontSystemDefaults.FontResolutionFactor = 2f;
        FontSystemDefaults.KernelWidth = 1;
        FontSystemDefaults.KernelHeight = 1;
        _regular = new FontSystem();
        _regular.AddFont(File.ReadAllBytes(FindFont(false)));
        _bold = new FontSystem();
        _bold.AddFont(File.ReadAllBytes(FindFont(true)));

        window.TextInput += (_, e) =>
        {
            if (!char.IsControl(e.Character)) _pendingChars.Add(e.Character);
        };
    }

    static string FindFont(bool bold)
    {
        string baseDir = AppContext.BaseDirectory;
        string win = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] candidates = bold
            ? new[]
            {
                Path.Combine(baseDir, "Fonts", "Bold.ttf"),
                "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
                "/Library/Fonts/Arial Bold.ttf",
                Path.Combine(win, "segoeuib.ttf"),
                Path.Combine(win, "arialbd.ttf"),
                "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf",
                "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
            }
            : new[]
            {
                Path.Combine(baseDir, "Fonts", "Regular.ttf"),
                "/System/Library/Fonts/Supplemental/Arial.ttf",
                "/Library/Fonts/Arial.ttf",
                Path.Combine(win, "segoeui.ttf"),
                Path.Combine(win, "arial.ttf"),
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/TTF/DejaVuSans.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
            };
        foreach (var c in candidates)
            if (!string.IsNullOrEmpty(c) && File.Exists(c)) return c;
        if (bold) return FindFont(false);
        throw new FileNotFoundException(
            "Nenašel jsem žádný TTF font. Vlož Regular.ttf a Bold.ttf do složky Fonts vedle aplikace.");
    }

    public static void BeginFrame(float dt)
    {
        Time += dt;
        PrevMouse = Mouse;
        PrevKb = Kb;
        Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
        Kb = Keyboard.GetState();
        bool sdlDown = Sdl.TakeLeftDown();
        LeftPressed = sdlDown || (Mouse.LeftButton == ButtonState.Pressed && PrevMouse.LeftButton == ButtonState.Released);
        Wheel = (Mouse.ScrollWheelValue - PrevMouse.ScrollWheelValue) / 120;
        if (Wheel == 0 && Mouse.ScrollWheelValue != PrevMouse.ScrollWheelValue)
            Wheel = Math.Sign(Mouse.ScrollWheelValue - PrevMouse.ScrollWheelValue);
        TypedChars.Clear();
        TypedChars.AddRange(_pendingChars);
        _pendingChars.Clear();
        ClickConsumed = false;
        Tooltip = null;

        // backspace s auto-opakováním
        BackspaceFired = false;
        if (Kb.IsKeyDown(Keys.Back))
        {
            if (PrevKb.IsKeyUp(Keys.Back)) { BackspaceFired = true; _backRepeat = 0.45f; }
            else
            {
                _backRepeat -= dt;
                if (_backRepeat <= 0) { BackspaceFired = true; _backRepeat = 0.04f; }
            }
        }
    }

    public static bool KeyPressed(Keys k) => Kb.IsKeyDown(k) && PrevKb.IsKeyUp(k);

    public static bool Hover(Rectangle r) => r.Contains(MousePos);

    public static bool Clicked(Rectangle r)
    {
        if (ClickConsumed || !LeftPressed || !r.Contains(MousePos)) return false;
        ClickConsumed = true;
        return true;
    }

    // ---------------- kreslení ----------------

    public static void Rect(Rectangle r, Color c) => Sb.Draw(_pixel, r, c);

    public static void Rect(float x, float y, float w, float h, Color c) =>
        Sb.Draw(_pixel, new Vector2(x, y), null, c, 0, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0);

    public static void Border(Rectangle r, Color c, int t = 1)
    {
        Rect(new Rectangle(r.X, r.Y, r.Width, t), c);
        Rect(new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
        Rect(new Rectangle(r.X, r.Y, t, r.Height), c);
        Rect(new Rectangle(r.Right - t, r.Y, t, r.Height), c);
    }

    public static void Line(Vector2 a, Vector2 b, Color c, float thickness = 2)
    {
        var d = b - a;
        float len = d.Length();
        if (len < 0.01f) return;
        float ang = MathF.Atan2(d.Y, d.X);
        Sb.Draw(_pixel, a, null, c, ang, new Vector2(0, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0);
    }

    public static void Circle(Vector2 center, float radius, Color c)
    {
        float s = radius * 2 / CircleSize;
        Sb.Draw(_circle, center, null, c, 0, new Vector2(CircleSize / 2f), s, SpriteEffects.None, 0);
    }

    public static void RoundRect(Rectangle r, Color c, int radius = 10)
    {
        radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2);
        if (radius <= 1) { Rect(r, c); return; }
        int d = radius * 2;
        Rect(new Rectangle(r.X + radius, r.Y, r.Width - d, r.Height), c);
        Rect(new Rectangle(r.X, r.Y + radius, radius, r.Height - d), c);
        Rect(new Rectangle(r.Right - radius, r.Y + radius, radius, r.Height - d), c);
        int h = CircleSize / 2;
        Sb.Draw(_circle, new Rectangle(r.X, r.Y, radius, radius), new Rectangle(0, 0, h, h), c);
        Sb.Draw(_circle, new Rectangle(r.Right - radius, r.Y, radius, radius), new Rectangle(h, 0, h, h), c);
        Sb.Draw(_circle, new Rectangle(r.X, r.Bottom - radius, radius, radius), new Rectangle(0, h, h, h), c);
        Sb.Draw(_circle, new Rectangle(r.Right - radius, r.Bottom - radius, radius, radius), new Rectangle(h, h, h, h), c);
    }

    public static void Texture(Texture2D t, Rectangle r, Color? c = null) => Sb.Draw(t, r, c ?? Color.White);

    // ---------------- text ----------------

    public static DynamicSpriteFont Font(int size, bool bold = false) => (bold ? _bold : _regular).GetFont(size);

    public static Vector2 Measure(string s, int size, bool bold = false) =>
        string.IsNullOrEmpty(s) ? new Vector2(0, size) : Font(size, bold).MeasureString(s);

    public static void Text(string s, float x, float y, int size, Color c, bool bold = false)
    {
        if (string.IsNullOrEmpty(s)) return;
        Sb.DrawString(Font(size, bold), s, new Vector2(MathF.Round(x), MathF.Round(y)), c);
    }

    public static void TextCentered(string s, Rectangle r, int size, Color c, bool bold = false)
    {
        var m = Measure(s, size, bold);
        Text(s, r.X + (r.Width - m.X) / 2, r.Y + (r.Height - size) / 2f - size * 0.08f, size, c, bold);
    }

    public static void TextRight(string s, float right, float y, int size, Color c, bool bold = false)
    {
        var m = Measure(s, size, bold);
        Text(s, right - m.X, y, size, c, bold);
    }

    /// <summary>Zalomí text do šířky, vrátí výšku.</summary>
    public static float TextWrapped(string s, Rectangle r, int size, Color c, bool bold = false, int maxLines = 99)
    {
        var lines = Wrap(s, r.Width, size, bold);
        float lh = size * 1.3f;
        int n = Math.Min(lines.Count, maxLines);
        for (int i = 0; i < n; i++)
        {
            var line = lines[i];
            if (i == n - 1 && lines.Count > n) line = line.TrimEnd() + "…";
            Text(line, r.X, r.Y + i * lh, size, c, bold);
        }
        return n * lh;
    }

    public static List<string> Wrap(string s, int width, int size, bool bold = false)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(s)) return result;
        foreach (var para in s.Split('\n'))
        {
            var words = para.Split(' ');
            string line = "";
            foreach (var w in words)
            {
                string test = line.Length == 0 ? w : line + " " + w;
                if (Measure(test, size, bold).X > width && line.Length > 0)
                {
                    result.Add(line);
                    line = w;
                }
                else line = test;
            }
            result.Add(line);
        }
        return result;
    }

    public static string Ellipsize(string s, int width, int size, bool bold = false)
    {
        if (Measure(s, size, bold).X <= width) return s;
        while (s.Length > 1 && Measure(s + "…", size, bold).X > width) s = s[..^1];
        return s + "…";
    }

    // ---------------- widgety ----------------

    public static bool Button(Rectangle r, string label, Color? bg = null, Color? fg = null, int size = 18,
        bool enabled = true, int radius = 8)
    {
        var b = bg ?? Theme.Button;
        bool hover = enabled && Hover(r);
        if (!enabled) b = Theme.ButtonDisabled;
        else if (hover) b = Theme.Lighten(b, 0.12f);
        RoundRect(r, b, radius);
        TextCentered(label, r, size, enabled ? fg ?? Theme.Text : Theme.TextDim, true);
        return enabled && Clicked(r);
    }

    public static bool Checkbox(Rectangle r, bool value, Color accent)
    {
        RoundRect(r, value ? accent : Theme.Panel2, 6);
        if (!value) Border(r, Hover(r) ?Theme.TextDim : Theme.Line, 2);
        if (value)
        {
            // fajfka
            var a = new Vector2(r.X + r.Width * 0.22f, r.Y + r.Height * 0.52f);
            var b = new Vector2(r.X + r.Width * 0.42f, r.Y + r.Height * 0.72f);
            var c = new Vector2(r.X + r.Width * 0.78f, r.Y + r.Height * 0.3f);
            Line(a, b, Color.White, 3);
            Line(b - new Vector2(1, 0), c, Color.White, 3);
        }
        return Clicked(r);
    }

    public static void ProgressBar(Rectangle r, float t, Color c)
    {
        RoundRect(r, Theme.Panel2, r.Height / 2);
        int w = (int)(r.Width * Math.Clamp(t, 0, 1));
        if (w > 0) RoundRect(new Rectangle(r.X, r.Y, Math.Max(w, r.Height), r.Height), c, r.Height / 2);
    }

    public static void Panel(Rectangle r, Color? c = null, int radius = 12) => RoundRect(r, c ?? Theme.Panel, radius);

    // ---------------- ořez (scissor) ----------------

    static readonly RasterizerState ScissorState = new() { ScissorTestEnable = true, CullMode = CullMode.None };

    public static void Begin() => Sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
        null, RasterizerState.CullNone);

    public static void BeginClip(Rectangle r)
    {
        Sb.End();
        var vp = Gd.Viewport.Bounds;
        r = Rectangle.Intersect(r, vp);
        Gd.ScissorRectangle = r;
        Sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, ScissorState);
    }

    public static void EndClip()
    {
        Sb.End();
        Begin();
    }

    public static void DrawTooltip()
    {
        if (string.IsNullOrEmpty(Tooltip)) return;
        var m = Measure(Tooltip, 15);
        var p = MousePos;
        var r = new Rectangle(p.X + 14, p.Y + 18, (int)m.X + 16, 26);
        if (r.Right > Width - 4) r.X = Width - 4 - r.Width;
        if (r.Bottom > Height - 4) r.Y = p.Y - 30;
        RoundRect(r, new Color(10, 12, 18, 240), 6);
        Text(Tooltip, r.X + 8, r.Y + 4, 15, Theme.Text);
    }
}
