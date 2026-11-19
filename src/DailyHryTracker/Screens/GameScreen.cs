using DailyHryTracker.Core;
using DailyHryTracker.Data;
using DailyHryTracker.Games;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker.Screens;

/// <summary>Obal kolem mini-hry: horní lišta, zápis výsledku, další kolo v nekonečném režimu.</summary>
public class GameScreen : Screen
{
    readonly GameInfo _info;
    readonly bool _daily;
    MiniGame _game;
    bool _recorded;
    bool _confirmGiveUp;
    int _endlessRound = 1;

    public GameScreen(GameInfo info, bool daily)
    {
        _info = info;
        _daily = daily;
        StartNew();
    }

    void StartNew()
    {
        TextField.Focused = null;
        _game = _info.Create();
        _game.Setup(_info, _daily, Tracker.Today);
        _recorded = false;
        _confirmGiveUp = false;
    }

    public override void Frame(float dt)
    {
        var t = TrackerApp.I.Tracker;

        // horní lišta
        var bar = new Rectangle(0, 0, Ui.Width, 64);
        Ui.Rect(bar, Theme.Panel);
        Ui.Rect(new Rectangle(0, 62, Ui.Width, 2), _info.Color);
        if (Ui.Button(new Rectangle(14, 12, 110, 40), "← Zpět", null, null, 17) ||
            Ui.KeyPressed(Keys.Escape))
        {
            TrackerApp.I.BackToMain();
            return;
        }
        string mode = _daily ? $"Denní #{_game.DayNumber}" : $"Nekonečná · kolo {_endlessRound}";
        Ui.Text(_info.Name, 140, 10, 24, Theme.Text, true);
        Ui.Text(mode, 140, 38, 14, _daily ? Theme.Gold : Theme.Purple, true);
        if (Ui.Hover(new Rectangle(140, 8, 300, 50))) Ui.Tooltip = _info.Help;

        if (_game.Finished)
        {
            if (!_recorded)
            {
                _recorded = true;
                if (_daily) t.RecordDaily(_info.Id, _game.Result, _game.Date);
                else t.RecordEndless(_info.Id, _game.Result.Won, _game.Result.Score);
            }
            var res = new Rectangle(460, 12, 470, 40);
            Ui.RoundRect(res, _game.Result.Won ? Theme.Green : Theme.Panel2, 8);
            Ui.TextCentered(Ui.Ellipsize(_game.ResultText, 450, 17, true), res, 17, Theme.Text, true);
            if (_daily)
            {
                if (Ui.Button(new Rectangle(Ui.Width - 320, 12, 180, 40), "Hrát nekonečnou", Theme.Purple, Theme.Text, 16))
                    TrackerApp.I.Open(new GameScreen(_info, false));
                if (Ui.Button(new Rectangle(Ui.Width - 130, 12, 116, 40), "Přehled", Theme.Accent, Theme.Text, 16))
                    TrackerApp.I.BackToMain();
            }
            else if (Ui.Button(new Rectangle(Ui.Width - 200, 12, 186, 40), "Další kolo →", Theme.Purple, Theme.Text, 17) ||
                     (Ui.KeyPressed(Keys.Enter) && TextField.Focused == null))
            {
                _endlessRound++;
                StartNew();
            }
        }
        else if (_game.CanGiveUp)
        {
            var gr = new Rectangle(Ui.Width - 150, 12, 136, 40);
            if (!_confirmGiveUp)
            {
                if (Ui.Button(gr, "Vzdát se", null, Theme.TextDim, 16)) _confirmGiveUp = true;
            }
            else
            {
                if (Ui.Button(gr, "Opravdu?", Theme.Red, Theme.Text, 16)) _game.GiveUp();
                if (!Ui.Hover(gr) && Ui.LeftPressed) _confirmGiveUp = false;
            }
            if (_daily)
                Ui.TextRight("Výsledek se zapíše do dnešního dne", Ui.Width - 166, 24, 14, Theme.TextDim);
        }

        _game.Frame(new Rectangle(0, 70, Ui.Width, Ui.Height - 70), dt);
    }
}
