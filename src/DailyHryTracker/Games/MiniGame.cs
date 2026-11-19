using DailyHryTracker.Core;
using DailyHryTracker.Data;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

public class GameInfo
{
    public string Id;
    public string Name;
    public string Tagline;
    public string Help;
    public Color Color;
    public bool NeedsNetwork;
    public Func<MiniGame> Create;
}

/// <summary>Základ pro každou mini-hru. Hra si sama kreslí obsah a na konci zavolá <see cref="Finish"/>.</summary>
public abstract class MiniGame
{
    public GameInfo Info;
    public bool Daily;
    public int Seed;
    public DateTime Date;
    public Random Rng;

    public bool Finished { get; private set; }
    public DailyResult Result { get; private set; }

    /// <summary>Text, který se zobrazí v horní liště po dohrání.</summary>
    public string ResultText = "";

    public void Setup(GameInfo info, bool daily, DateTime date)
    {
        Info = info;
        Daily = daily;
        Date = date;
        Seed = daily ? TextUtil.StableHash(info.Id + "|" + Profile.Key(date)) : Random.Shared.Next();
        Rng = new Random(Seed);
        Init();
    }

    /// <summary>Pořadové číslo denní hry (jako "Wordle #123").</summary>
    public int DayNumber => (int)(Date.Date - new DateTime(2026, 1, 1)).TotalDays + 1;

    protected abstract void Init();
    public abstract void Frame(Rectangle area, float dt);

    protected void Finish(bool won, int score, int attempts, string summary, string resultText)
    {
        if (Finished) return;
        Finished = true;
        ResultText = resultText;
        Result = new DailyResult { Won = won, Score = score, Attempts = attempts, Summary = summary };
    }

    /// <summary>Volá se z obrazovky, když se hráč vzdá.</summary>
    public virtual void GiveUp() => Finish(false, 0, 0, "vzdáno", "Vzdáno.");
    public virtual bool CanGiveUp => !Finished;

    // ---- pomocníci pro hlášky ----
    protected string Toast;
    protected float ToastTime;

    protected void ShowToast(string s, float seconds = 1.8f)
    {
        Toast = s;
        ToastTime = seconds;
    }

    protected void DrawToast(Rectangle area, float dt, int y)
    {
        if (ToastTime <= 0 || string.IsNullOrEmpty(Toast)) return;
        ToastTime -= dt;
        var m = Ui.Measure(Toast, 18, true);
        var r = new Rectangle(area.Center.X - (int)m.X / 2 - 16, y, (int)m.X + 32, 38);
        Ui.RoundRect(r, Theme.Text * Math.Clamp(ToastTime * 3, 0, 1), 8);
        Ui.TextCentered(Toast, r, 18, Theme.Bg, true);
    }
}
