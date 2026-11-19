using Microsoft.Xna.Framework;

namespace DailyHryTracker.Core;

public static class Theme
{
    public static readonly Color Bg = new(13, 17, 23);
    public static readonly Color Panel = new(22, 27, 34);
    public static readonly Color Panel2 = new(33, 38, 45);
    public static readonly Color Line = new(48, 54, 61);
    public static readonly Color Text = new(230, 237, 243);
    public static readonly Color TextDim = new(139, 148, 158);
    public static readonly Color Button = new(48, 54, 61);
    public static readonly Color ButtonDisabled = new(30, 34, 40);
    public static readonly Color Accent = new(47, 129, 247);
    public static readonly Color Green = new(46, 160, 67);
    public static readonly Color Yellow = new(210, 166, 32);
    public static readonly Color Red = new(218, 54, 51);
    public static readonly Color Orange = new(240, 136, 62);
    public static readonly Color Purple = new(137, 87, 229);
    public static readonly Color Gold = new(227, 179, 65);

    // GitHub-like heatmap
    public static readonly Color[] Heat =
    {
        new(42, 48, 56), new(14, 68, 41), new(0, 109, 50), new(38, 166, 65), new(57, 211, 83)
    };

    public static Color Lighten(Color c, float t) => Color.Lerp(c, Color.White, t);
    public static Color Darken(Color c, float t) => Color.Lerp(c, Color.Black, t);
    public static Color WithAlpha(Color c, float a) => c * a;
}
