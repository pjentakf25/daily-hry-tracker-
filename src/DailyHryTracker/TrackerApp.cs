using DailyHryTracker.Core;
using DailyHryTracker.Data;
using DailyHryTracker.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker;

public abstract class Screen
{
    public abstract void Frame(float dt);
}

public class TrackerApp : Game
{
    public static TrackerApp I { get; private set; }
    public Tracker Tracker { get; private set; }

    readonly GraphicsDeviceManager _graphics;
    Screen _screen;
    MainScreen _main;
    readonly Queue<AwardDef> _awardQueue = new();
    AwardDef _awardShown;
    float _awardTime;
    string _notice;
    float _noticeTime;

    public TrackerApp()
    {
        I = this;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Ui.Width,
            PreferredBackBufferHeight = Ui.Height,
            SynchronizeWithVerticalRetrace = true,
            PreferMultiSampling = true,
        };
        IsMouseVisible = true;
        Window.Title = "Daily Hry Tracker";
        Window.AllowUserResizing = false;
    }

    protected override void Initialize()
    {
        base.Initialize();
        Ui.Init(GraphicsDevice, Window);

        Tracker = new Tracker(SaveStore.Load());
        Tracker.AwardUnlocked += a => _awardQueue.Enqueue(a);
        FriendSync.Scan(Tracker);
        _awardQueue.Clear(); // ocenění odemčená při startu (např. po aktualizaci) nehlásíme
        Tracker.CheckAwards();
        Tracker.Save();

        // přetažení souboru od kamaráda do okna
        Window.FileDrop += (_, e) =>
        {
            foreach (var f in e.Files)
            {
                var s = FriendSync.Import(Tracker, f);
                Notify(s != null ? $"Kamarád {s.Name} přidán (data k {s.Exported:d. M. yyyy})" : FriendSync.LastError);
            }
        };

        _main = new MainScreen();
        _screen = _main;
        _shotPath = Environment.GetEnvironmentVariable("DHT_SHOT");
        var shotScreen = Environment.GetEnvironmentVariable("DHT_SHOT_SCREEN") ?? "";
        if (shotScreen.StartsWith("game:"))
        {
            var parts = shotScreen[5..].Split(':');
            _screen = new GameScreen(Games.GameCatalog.Get(parts[0]), parts.Length < 2 || parts[1] != "endless");
        }
        else if (shotScreen.Length > 0) _main.SelectTab(shotScreen);
    }

    // --- testovací režim: DHT_SHOT=cesta.png vyrenderuje obrazovku do souboru a ukončí aplikaci ---
    string _shotPath;
    int _frames;

    public void Notify(string text, float seconds = 4)
    {
        _notice = text;
        _noticeTime = seconds;
    }

    public void Open(Screen s) => _screen = s;
    public void BackToMain()
    {
        TextField.Focused = null;
        _screen = _main;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Ui.BeginFrame(dt);
        GraphicsDevice.Clear(Theme.Bg);
        Ui.Begin();

        Microsoft.Xna.Framework.Graphics.RenderTarget2D rt = null;
        if (_shotPath != null && ++_frames == 90)
        {
            Ui.Sb.End();
            rt = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(GraphicsDevice, Ui.Width, Ui.Height);
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Theme.Bg);
            Ui.Begin();
        }

        _screen.Frame(dt);
        DrawAwardPopup(dt);
        DrawNotice(dt);
        Ui.DrawTooltip();

        Ui.Sb.End();
        if (rt != null)
        {
            GraphicsDevice.SetRenderTarget(null);
            using (var fs = File.Create(_shotPath)) rt.SaveAsPng(fs, Ui.Width, Ui.Height);
            Exit();
        }
        base.Draw(gameTime);
    }

    void DrawAwardPopup(float dt)
    {
        if (_awardShown == null && _awardQueue.Count > 0)
        {
            _awardShown = _awardQueue.Dequeue();
            _awardTime = 4.5f;
        }
        if (_awardShown == null) return;
        _awardTime -= dt;
        float slide = Math.Clamp(Math.Min(4.5f - _awardTime, _awardTime) * 4, 0, 1);
        var r = new Rectangle(Ui.Width - (int)(380 * slide) - 10, 74, 370, 84);
        Ui.RoundRect(r, Theme.Panel2, 12);
        Ui.Border(r, _awardShown.Color, 2);
        Medal(new Vector2(r.X + 44, r.Center.Y), 28, _awardShown.Color, true);
        Ui.Text("Nové ocenění!", r.X + 86, r.Y + 14, 15, Theme.Gold, true);
        Ui.Text(Ui.Ellipsize(_awardShown.Name, 270, 20), r.X + 86, r.Y + 34, 20, Theme.Text, true);
        Ui.Text(Ui.Ellipsize(_awardShown.Description, 270, 14), r.X + 86, r.Y + 60, 14, Theme.TextDim);
        if (_awardTime <= 0) _awardShown = null;
    }

    void DrawNotice(float dt)
    {
        if (_noticeTime <= 0 || string.IsNullOrEmpty(_notice)) return;
        _noticeTime -= dt;
        var m = Ui.Measure(_notice, 17, true);
        var r = new Rectangle(Ui.Width / 2 - (int)m.X / 2 - 20, Ui.Height - 70, (int)m.X + 40, 42);
        Ui.RoundRect(r, Theme.Accent * Math.Clamp(_noticeTime * 3, 0, 1), 10);
        Ui.TextCentered(_notice, r, 17, Theme.Text, true);
    }

    /// <summary>Kreslí medaili – sdíleno s obrazovkou ocenění.</summary>
    public static void Medal(Vector2 c, float radius, Color color, bool unlocked)
    {
        var col = unlocked ? color : Theme.Panel2;
        Ui.Line(c + new Vector2(-radius * 0.5f, -radius * 1.1f), c + new Vector2(-radius * 0.15f, -radius * 0.5f), unlocked ? Theme.Red : Theme.Line, radius * 0.35f);
        Ui.Line(c + new Vector2(radius * 0.5f, -radius * 1.1f), c + new Vector2(radius * 0.15f, -radius * 0.5f), unlocked ? Theme.Accent : Theme.Line, radius * 0.35f);
        Ui.Circle(c, radius, Theme.Darken(col, 0.25f));
        Ui.Circle(c, radius * 0.8f, col);
        Ui.Circle(c + new Vector2(-radius * 0.25f, -radius * 0.25f), radius * 0.22f, Color.White * (unlocked ? 0.35f : 0.08f));
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        Tracker?.Save();
        base.OnExiting(sender, args);
    }
}
