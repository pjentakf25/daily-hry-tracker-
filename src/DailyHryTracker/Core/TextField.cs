using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker.Core;

/// <summary>Jednořádkové textové pole. Focus se drží globálně – aktivní je vždy max. jedno.</summary>
public class TextField
{
    public static TextField Focused;
    public string Value = "";
    public string Placeholder = "";
    public int MaxLength = 200;
    public Func<char, bool> Filter;
    public bool UpperCase;
    public bool Submitted;

    public bool IsFocused => Focused == this;
    public void Focus() => Focused = this;

    /// <summary>Vykreslí pole a zpracuje vstup. Vrací true, pokud uživatel stiskl Enter.</summary>
    public bool Frame(Rectangle r, int size = 18, bool enabled = true)
    {
        Submitted = false;
        if (enabled && Ui.LeftPressed && !Ui.ClickConsumed)
        {
            if (r.Contains(Ui.MousePos)) { Focused = this; Ui.ClickConsumed = true; }
            else if (Focused == this) Focused = null;
        }
        bool focus = enabled && Focused == this;
        Ui.RoundRect(r, Theme.Panel2, 8);
        Ui.Border(r, focus ? Theme.Accent : Theme.Line, 2);

        if (focus)
        {
            HandleInput();
        }

        var inner = new Rectangle(r.X + 10, r.Y, r.Width - 20, r.Height);
        Ui.BeginClip(inner);
        float ty = r.Y + (r.Height - size) / 2f - 1;
        if (Value.Length == 0 && !focus)
            Ui.Text(Placeholder, inner.X, ty, size, Theme.TextDim);
        else
        {
            float w = Ui.Measure(Value, size).X;
            float x = inner.X;
            if (w > inner.Width - 6) x = inner.Right - 6 - w;
            Ui.Text(Value, x, ty, size, Theme.Text);
            if (focus && (int)(Ui.Time * 2) % 2 == 0)
                Ui.Rect(x + w + 1, r.Y + 8, 2, r.Height - 16, Theme.Text);
            if (Value.Length == 0 && focus)
                Ui.Text(Placeholder, inner.X + 6, ty, size, Theme.TextDim * 0.6f);
        }
        Ui.EndClip();
        return Submitted;
    }

    void HandleInput()
    {
        foreach (var ch in Ui.TypedChars)
        {
            if (Ui.CtrlDown) continue;
            if (Value.Length >= MaxLength) break;
            if (Filter != null && !Filter(ch)) continue;
            Value += UpperCase ? char.ToUpperInvariant(ch) : ch;
        }
        if (Ui.BackspaceFired && Value.Length > 0)
        {
            Value = Ui.CtrlDown ? "" : Value[..^1];
        }
        if (Ui.CtrlDown && Ui.KeyPressed(Keys.V))
        {
            var clip = Clipboard.Get();
            if (!string.IsNullOrEmpty(clip))
            {
                clip = clip.Replace("\r", "").Replace("\n", " ").Trim();
                foreach (var ch in clip)
                {
                    if (Value.Length >= MaxLength) break;
                    if (Filter != null && !Filter(ch)) continue;
                    Value += UpperCase ? char.ToUpperInvariant(ch) : ch;
                }
            }
        }
        if (Ui.KeyPressed(Keys.Enter)) Submitted = true;
    }
}
