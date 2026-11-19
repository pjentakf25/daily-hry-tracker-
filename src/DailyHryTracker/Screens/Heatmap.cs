using DailyHryTracker.Core;
using DailyHryTracker.Data;
using DailyHryTracker.Games;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Screens;

/// <summary>Mřížka aktivity jako na GitHubu: sloupce = týdny, řádky = dny (Po–Ne).</summary>
public static class Heatmap
{
    const int Weeks = 53;
    static readonly string[] Months = { "led", "úno", "bře", "dub", "kvě", "čvn", "čvc", "srp", "zář", "říj", "lis", "pro" };

    public static int Level(int count) => count <= 0 ? 0 : count <= 2 ? 1 : count <= 4 ? 2 : count <= 7 ? 3 : 4;

    public static void Draw(Rectangle r, Func<DateTime, int> countOn, string title)
    {
        var today = Tracker.Today;
        int dow = ((int)today.DayOfWeek + 6) % 7; // Po = 0
        var start = today.AddDays(-dow - (Weeks - 1) * 7);
        int labelW = 30;
        int cell = Math.Min((r.Width - labelW) / Weeks - 3, 14);
        int step = cell + 3;
        int top = r.Y + 22;

        int total = 0, active = 0;
        string[] days = { "Po", "", "St", "", "Pá", "", "" };
        for (int d = 0; d < 7; d++)
            if (days[d] != "") Ui.Text(days[d], r.X, top + d * step - 1, 12, Theme.TextDim);

        int lastMonth = -1;
        for (int w = 0; w < Weeks; w++)
        {
            for (int d = 0; d < 7; d++)
            {
                var date = start.AddDays(w * 7 + d);
                if (date > today) continue;
                if (d == 0 && date.Month != lastMonth)
                {
                    lastMonth = date.Month;
                    if (w < Weeks - 2) Ui.Text(Months[date.Month - 1], r.X + labelW + w * step, r.Y, 12, Theme.TextDim);
                }
                int cnt = countOn(date);
                total += cnt;
                if (cnt > 0) active++;
                var cr = new Rectangle(r.X + labelW + w * step, top + d * step, cell, cell);
                Ui.RoundRect(cr, Theme.Heat[Level(cnt)], 3);
                if (date == today) Ui.Border(cr, Theme.TextDim, 1);
                if (Ui.Hover(new Rectangle(cr.X - 1, cr.Y - 1, step, step)))
                    Ui.Tooltip = $"{date:d. M. yyyy}: {(cnt == 0 ? "žádná hra" : $"{cnt} {TextUtil.Plural(cnt, "hra", "hry", "her")} z {GameCatalog.All.Count}")}";
            }
        }

        int by = top + 7 * step + 8;
        Ui.Text($"{total} dohraných her za poslední rok · {active} aktivních dní · {title}", r.X + labelW, by, 13, Theme.TextDim);
        int lx = r.X + labelW + Weeks * step - 5 * step - 80;
        Ui.Text("Méně", lx, by, 12, Theme.TextDim);
        for (int i = 0; i < 5; i++)
            Ui.RoundRect(new Rectangle(lx + 38 + i * step, by + 1, cell, cell), Theme.Heat[i], 3);
        Ui.Text("Více", lx + 42 + 5 * step, by, 12, Theme.TextDim);
    }

    /// <summary>Malý pruh posledních N dní (pro žebříček přátel).</summary>
    public static void Strip(Rectangle r, int days, Func<DateTime, int> countOn)
    {
        int cell = Math.Max(4, r.Width / days - 2);
        for (int i = 0; i < days; i++)
        {
            var d = Tracker.Today.AddDays(i - days + 1);
            var cr = new Rectangle(r.X + i * (cell + 2), r.Y + (r.Height - cell) / 2, cell, cell);
            int cnt = countOn(d);
            Ui.RoundRect(cr, Theme.Heat[Level(cnt)], 2);
            if (Ui.Hover(cr)) Ui.Tooltip = $"{d:d. M.}: {cnt}";
        }
    }
}
