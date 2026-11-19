using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DailyHryTracker.Games;

public class WordleGame : MiniGame
{
    const int Rows = 6, Len = 5;
    string _answer;
    readonly List<string> _guesses = new();
    readonly List<char[]> _marks = new(); // 'G' zelená, 'Y' žlutá, 'X' šedá
    string _current = "";
    readonly Dictionary<char, char> _keyState = new();
    float _shake;

    public static readonly Color CGreen = new(83, 141, 78);
    public static readonly Color CYellow = new(181, 159, 59);
    public static readonly Color CGray = new(58, 58, 60);

    protected override void Init()
    {
        _answer = WordleWords.Answers[Rng.Next(WordleWords.Answers.Length)].ToUpperInvariant();
    }

    public static char[] Evaluate(string guess, string answer)
    {
        var res = new char[guess.Length];
        var counts = new Dictionary<char, int>();
        for (int i = 0; i < guess.Length; i++)
        {
            if (guess[i] == answer[i]) res[i] = 'G';
            else counts[answer[i]] = counts.GetValueOrDefault(answer[i]) + 1;
        }
        for (int i = 0; i < guess.Length; i++)
        {
            if (res[i] == 'G') continue;
            if (counts.GetValueOrDefault(guess[i]) > 0)
            {
                res[i] = 'Y';
                counts[guess[i]]--;
            }
            else res[i] = 'X';
        }
        return res;
    }

    void Type(char c)
    {
        if (Finished) return;
        if (_current.Length < Len) _current += c;
    }

    void Back()
    {
        if (!Finished && _current.Length > 0) _current = _current[..^1];
    }

    void Submit()
    {
        if (Finished) return;
        if (_current.Length < Len)
        {
            ShowToast("Málo písmen");
            _shake = 0.4f;
            return;
        }
        var marks = Evaluate(_current, _answer);
        _guesses.Add(_current);
        _marks.Add(marks);
        for (int i = 0; i < Len; i++)
        {
            char c = _current[i], m = marks[i];
            var prev = _keyState.GetValueOrDefault(c, ' ');
            if (m == 'G' || (m == 'Y' && prev != 'G') || prev == ' ') _keyState[c] = m;
        }
        string summary = string.Join("/", _marks.Select(r => new string(r)));
        if (_current == _answer)
        {
            string[] praise = { "Génius!", "Skvělé!", "Výborně!", "Pěkně!", "Uf, dobrý!", "Na poslední chvíli!" };
            ShowToast(praise[_guesses.Count - 1], 3);
            Finish(true, Rows + 1 - _guesses.Count, _guesses.Count, summary, $"Uhodnuto na {_guesses.Count}/6");
        }
        else if (_guesses.Count >= Rows)
        {
            ShowToast(_answer, 5);
            Finish(false, 0, 7, summary, $"Slovo bylo {_answer}");
        }
        _current = "";
    }

    public override void GiveUp()
    {
        ShowToast(_answer, 5);
        Finish(false, 0, 7, string.Join("/", _marks.Select(r => new string(r))), $"Slovo bylo {_answer}");
    }

    public override void Frame(Rectangle area, float dt)
    {
        // klávesnice
        if (!Finished && TextField.Focused == null)
        {
            foreach (var ch in Ui.TypedChars)
                if (char.IsAsciiLetter(ch)) Type(char.ToUpperInvariant(ch));
            if (Ui.BackspaceFired) Back();
            if (Ui.KeyPressed(Keys.Enter)) Submit();
        }

        const int tile = 62, gap = 8;
        int gridW = Len * tile + (Len - 1) * gap;
        int gx = area.Center.X - gridW / 2, gy = area.Y + 20;
        if (_shake > 0) _shake -= dt;

        for (int r = 0; r < Rows; r++)
        {
            int ox = r == _guesses.Count && _shake > 0 ? (int)(MathF.Sin(_shake * 60) * 6) : 0;
            for (int c = 0; c < Len; c++)
            {
                var rect = new Rectangle(gx + c * (tile + gap) + ox, gy + r * (tile + gap), tile, tile);
                char letter = ' ';
                Color bg = Theme.Bg, border = Theme.Line;
                if (r < _guesses.Count)
                {
                    letter = _guesses[r][c];
                    bg = MarkColor(_marks[r][c]);
                    border = bg;
                }
                else if (r == _guesses.Count && c < _current.Length)
                {
                    letter = _current[c];
                    border = Theme.TextDim;
                }
                Ui.Rect(rect, bg);
                Ui.Border(rect, border, 2);
                if (letter != ' ') Ui.TextCentered(letter.ToString(), rect, 34, Theme.Text, true);
            }
        }

        // on-screen klávesnice
        string[] rows = { "QWERTYUIOP", "ASDFGHJKL", "<ZXCVBNM>" };
        int ky = gy + Rows * (tile + gap) + 22;
        const int kw = 50, kh = 56, kg = 6;
        for (int ri = 0; ri < rows.Length; ri++)
        {
            var row = rows[ri];
            int total = row.Sum(ch => ch is '<' or '>' ? kw * 3 / 2 + 10 : kw) + (row.Length - 1) * kg;
            int x = area.Center.X - total / 2;
            foreach (var ch in row)
            {
                int w = ch is '<' or '>' ? kw * 3 / 2 + 10 : kw;
                var rect = new Rectangle(x, ky + ri * (kh + kg), w, kh);
                var st = _keyState.GetValueOrDefault(ch, ' ');
                var bg = st == ' ' ? new Color(129, 131, 132) : MarkColor(st);
                string label = ch == '<' ? "ENTER" : ch == '>' ? "SMAZAT" : ch.ToString();
                if (Ui.Button(rect, label, bg, Color.White, ch is '<' or '>' ? 14 : 20, !Finished, 6))
                {
                    if (ch == '<') Submit();
                    else if (ch == '>') Back();
                    else Type(ch);
                }
                x += w + kg;
            }
        }

        DrawToast(area, dt, gy - 10);
    }

    public static Color MarkColor(char m) => m switch
    {
        'G' => CGreen,
        'Y' => CYellow,
        _ => CGray,
    };
}
