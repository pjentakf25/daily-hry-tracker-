using System.Globalization;
using System.Text;

namespace DailyHryTracker.Core;

public static class TextUtil
{
    /// <summary>Malá písmena, bez diakritiky, bez okrajových mezer a interpunkce.</summary>
    public static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '-' ) sb.Append(' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Deterministický hash řetězce (string.GetHashCode je v .NET náhodný mezi spuštěními).</summary>
    public static int StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (var ch in s) { h ^= ch; h *= 16777619; }
            return (int)(h & 0x7fffffff);
        }
    }

    public static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static string Plural(int n, string one, string few, string many) =>
        n == 1 ? one : n is >= 2 and <= 4 ? few : many;
}
