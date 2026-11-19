using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker.Core;

/// <summary>Textové pole s našeptávačem. Vybraná položka se vrátí z <see cref="Frame"/>.</summary>
public class AutoComplete<T> where T : class
{
    public readonly TextField Field = new() { MaxLength = 60 };
    readonly Func<string, List<T>> _suggest;
    readonly Func<T, string> _label;
    int _highlight;

    public AutoComplete(Func<string, List<T>> suggest, Func<T, string> label, string placeholder)
    {
        _suggest = suggest;
        _label = label;
        Field.Placeholder = placeholder;
    }

    /// <summary>Vrací vybranou položku (kliknutím nebo Enterem), jinak null. Pokud Enter nic nenajde, vrátí null a nastaví Unmatched.</summary>
    public T Frame(Rectangle r, bool enabled, out bool unmatched)
    {
        unmatched = false;
        var list = enabled && Field.Value.Length > 0 ? _suggest(Field.Value) : new List<T>();
        if (_highlight >= list.Count) _highlight = 0;
        if (Field.IsFocused && list.Count > 0)
        {
            if (Ui.KeyPressed(Keys.Down)) _highlight = (_highlight + 1) % list.Count;
            if (Ui.KeyPressed(Keys.Up)) _highlight = (_highlight - 1 + list.Count) % list.Count;
        }

        // našeptávač se kreslí nad ostatním obsahem, proto kliky na něj vyhodnotíme dřív než pole
        T picked = null;
        int itemH = 36;
        var dropRects = new List<Rectangle>();
        for (int i = 0; i < list.Count; i++)
            dropRects.Add(new Rectangle(r.X, r.Bottom + 4 + i * itemH, r.Width, itemH));
        if (Field.IsFocused)
            for (int i = 0; i < list.Count; i++)
                if (Ui.Clicked(dropRects[i])) picked = list[i];

        bool enter = Field.Frame(r, 20, enabled);
        if (enter && picked == null)
        {
            if (list.Count > 0) picked = list[_highlight];
            else if (Field.Value.Trim().Length > 0) unmatched = true;
        }

        if (Field.IsFocused && list.Count > 0 && picked == null)
        {
            var bg = new Rectangle(r.X, r.Bottom + 2, r.Width, list.Count * itemH + 4);
            Ui.RoundRect(bg, Theme.Panel2, 8);
            Ui.Border(bg, Theme.Line, 1);
            for (int i = 0; i < list.Count; i++)
            {
                var ir = dropRects[i];
                if (i == _highlight || Ui.Hover(ir)) Ui.Rect(new Rectangle(ir.X + 2, ir.Y, ir.Width - 4, ir.Height), Theme.Line);
                Ui.Text(Ui.Ellipsize(_label(list[i]), ir.Width - 24, 18), ir.X + 12, ir.Y + 8, 18, Theme.Text);
            }
        }
        if (picked != null)
        {
            Field.Value = "";
            _highlight = 0;
        }
        return picked;
    }
}
