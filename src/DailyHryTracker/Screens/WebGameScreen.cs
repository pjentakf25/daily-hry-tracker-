using DailyHryTracker.Core;
using DailyHryTracker.Data;
using DailyHryTracker.Games;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Screens;

/// <summary>Originální web denní hry vložený přímo do okna aplikace. Křížek vpravo nahoře web zavře a hru odškrtne.</summary>
public class WebGameScreen : Screen
{
    public const int BarHeight = 56;
    readonly GameInfo _game;
    bool _opened;

    public WebGameScreen(GameInfo game) => _game = game;

    static Rectangle WebArea => new(0, BarHeight, Ui.Width, Ui.Height - BarHeight);

    public override void Frame(float dt)
    {
        var web = TrackerApp.I.Web;
        if (!_opened && web != null)
        {
            _opened = true;
            TextField.Focused = null;
            web.Open(_game.Url, WebArea);
        }

        // pod webem (vidět jen během načítání nebo při chybě)
        var area = WebArea;
        Ui.Rect(area, Theme.Bg);
        string msg = web == null ? "Vestavěný prohlížeč není na tomhle systému k dispozici."
            : web.Error ?? "Načítám " + new Uri(_game.Url).Host + "…";
        Ui.TextCentered(msg, new Rectangle(area.X, area.Center.Y - 20, area.Width, 40), 20, web?.Error != null ? Theme.Red : Theme.TextDim);

        DrawBar();
    }

    void DrawBar()
    {
        var bar = new Rectangle(0, 0, Ui.Width, BarHeight);
        Ui.Rect(bar, Theme.Panel);
        Ui.Rect(new Rectangle(0, BarHeight - 1, Ui.Width, 1), Theme.Line);
        Ui.Rect(new Rectangle(0, 0, Ui.Width, 3), _game.Color);

        var web = TrackerApp.I.Web;
        if (Ui.Button(new Rectangle(14, 11, 120, 34), "← Přehled", Theme.Panel2, Theme.Text, 15))
        {
            Leave(false);
            return;
        }

        Ui.Text(_game.Name, 152, 10, 20, Theme.Text, true);
        Ui.Text(new Uri(_game.Url).Host, 152, 33, 13, Theme.TextDim);

        int x = Ui.Width - 70;
        // křížek: zavřít a odškrtnout
        var close = new Rectangle(x, 8, 52, 40);
        bool hover = Ui.Hover(close);
        Ui.RoundRect(close, hover ? Theme.Red : Theme.Panel2, 8);
        var c = close.Center.ToVector2();
        Ui.Line(c + new Vector2(-9, -9), c + new Vector2(9, 9), Theme.Text, 3);
        Ui.Line(c + new Vector2(-9, 9), c + new Vector2(9, -9), Theme.Text, 3);
        bool done = TrackerApp.I.Tracker.IsDone(_game.Id, Tracker.Today);
        if (Ui.Clicked(close))
        {
            Leave(true);
            return;
        }
        // tooltipy by zakryl web, proto vysvětlení přímo v liště
        string hint = done ? "dnes odškrtnuto" : "křížek = zavřít a odškrtnout";
        Ui.TextRight(hint, x - 14, 19, 15, done ? Theme.Green : hover ? Theme.Text : Theme.TextDim, done);

        int bx = x - 14 - (int)Ui.Measure(hint, 15, done).X - 24 - 96;
        if (Ui.Button(new Rectangle(bx - 104, 11, 96, 34), "Zpět", Theme.Panel2, Theme.Text, 15, web != null))
            web?.GoBack();
        if (Ui.Button(new Rectangle(bx, 11, 96, 34), "Obnovit", Theme.Panel2, Theme.Text, 15, web != null))
            web?.Reload();
    }

    void Leave(bool checkOff)
    {
        TrackerApp.I.Web?.Close();
        var t = TrackerApp.I.Tracker;
        if (checkOff && !t.IsDone(_game.Id, Tracker.Today))
        {
            t.ToggleManual(_game.Id, Tracker.Today);
            TrackerApp.I.Notify($"{_game.Name} odškrtnuto");
        }
        TrackerApp.I.BackToMain();
    }
}
