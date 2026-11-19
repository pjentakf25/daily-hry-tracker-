using DailyHryTracker.Core;
using DailyHryTracker.Games.Data;
using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

/// <summary>Krillion (vzácné odpovědi = víc bodů) a Noillirk (obrácená pravidla – boduje nejčastější).</summary>
public class KrillionGame : MiniGame
{
    readonly bool _reversed;
    const int Rounds = 7;
    const float RoundTime = 25f;

    static readonly string[] TierNames = { "Plankton", "Krevetka", "Sardinka", "Tuňák", "Delfín", "Jeden z krilionu" };
    static readonly int[] KrillionPoints = { 10, 20, 35, 50, 75, 100 };
    static readonly Color[] TierColors =
    {
        new(120, 130, 140), new(240, 140, 130), new(120, 180, 220), new(60, 120, 200), new(90, 200, 190), new(255, 210, 60)
    };

    class Round
    {
        public KrillionPrompt Prompt;
        public string Answer = "";
        public int Tier = -1; // -1 = neplatná/prázdná
        public int Points;
        public string Matched;
    }

    readonly List<Round> _rounds = new();
    int _index = -1; // -1 = úvod
    float _timer;
    float _feedback; // > 0 = ukazuje výsledek kola
    readonly TextField _input = new() { Placeholder = "Napiš odpověď a stiskni Enter", MaxLength = 40 };

    public KrillionGame(bool reversed) => _reversed = reversed;

    protected override void Init()
    {
        var pool = KrillionData.All.ToList();
        TextUtil.Shuffle(pool, Rng);
        foreach (var p in pool.Take(Rounds)) _rounds.Add(new Round { Prompt = p });
    }

    int PointsFor(int tier) => _reversed ? KrillionPoints[5 - tier] : KrillionPoints[tier];

    static int Tier(int index, int count)
    {
        float p = count <= 1 ? 0 : index / (float)(count - 1);
        return p < 0.12f ? 0 : p < 0.3f ? 1 : p < 0.5f ? 2 : p < 0.7f ? 3 : p < 0.88f ? 4 : 5;
    }

    void Evaluate(Round r)
    {
        var norm = TextUtil.Normalize(r.Answer);
        r.Tier = -1;
        if (norm.Length == 0) return;
        var answers = r.Prompt.Answers;
        int best = -1;
        for (int i = 0; i < answers.Length && best < 0; i++)
            foreach (var variant in answers[i].Split('|'))
            {
                var v = TextUtil.Normalize(variant);
                if (v == norm || (v.Length >= 5 && Levenshtein(v, norm) <= 1))
                {
                    best = i;
                    r.Matched = answers[i].Split('|')[0];
                    break;
                }
            }
        if (best < 0) return;
        r.Tier = Tier(best, answers.Length);
        r.Points = PointsFor(r.Tier);
    }

    static int Levenshtein(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1) return 99;
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        for (int j = 1; j <= b.Length; j++)
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }

    void StartRound()
    {
        _index++;
        _timer = RoundTime;
        _feedback = 0;
        _input.Value = "";
        _input.Focus();
    }

    void SubmitRound()
    {
        var r = _rounds[_index];
        r.Answer = _input.Value;
        Evaluate(r);
        _feedback = 1.6f;
        TextField.Focused = null;
    }

    int Total => _rounds.Sum(r => r.Points);

    void Complete()
    {
        _index = Rounds;
        int total = Total;
        string summary = string.Join("", _rounds.Select(r => r.Tier < 0 ? "-" : r.Tier.ToString())) + $" {total}";
        Finish(total >= 350, total, _rounds.Count(r => r.Tier >= 0), summary, $"{total} bodů z {Rounds * 100}");
    }

    public override void GiveUp()
    {
        TextField.Focused = null;
        Complete();
    }

    public override void Frame(Rectangle area, float dt)
    {
        var center = new Rectangle(area.Center.X - 380, area.Y + 20, 760, area.Height - 40);
        string title = _reversed ? "Noillirk – napiš to, co by napsali všichni" : "Krillion – buď originální";
        Ui.TextCentered(title, new Rectangle(center.X, center.Y, center.Width, 30), 24, Info.Color, true);

        if (_index < 0)
        {
            var box = new Rectangle(center.X, center.Y + 60, center.Width, 300);
            Ui.Panel(box);
            Ui.TextWrapped(Info.Help, new Rectangle(box.X + 30, box.Y + 30, box.Width - 60, 100), 20, Theme.Text);
            int y = box.Y + 130;
            Ui.Text("Bodování:", box.X + 30, y, 18, Theme.TextDim, true);
            for (int t = 0; t < 6; t++)
            {
                var r = new Rectangle(box.X + 30 + t * 118, y + 32, 110, 64);
                Ui.RoundRect(r, TierColors[t] * 0.85f, 8);
                Ui.TextCentered(TierNames[t], new Rectangle(r.X, r.Y + 6, r.Width, 24), t == 5 ? 13 : 15, Theme.Bg, true);
                Ui.TextCentered($"{PointsFor(t)} b", new Rectangle(r.X, r.Y + 32, r.Width, 24), 18, Theme.Bg, true);
            }
            Ui.Text("← nejčastější odpovědi", box.X + 30, y + 104, 15, Theme.TextDim);
            Ui.TextRight("nejvzácnější →", box.Right - 30, y + 104, 15, Theme.TextDim);
            if (Ui.Button(new Rectangle(center.Center.X - 110, box.Bottom + 30, 220, 56), "Začít", Info.Color, Theme.Bg, 22))
                StartRound();
            return;
        }

        if (_index >= Rounds)
        {
            DrawSummary(center);
            return;
        }

        var round = _rounds[_index];
        Ui.TextCentered($"Otázka {_index + 1} / {Rounds}     Body: {Total}", new Rectangle(center.X, center.Y + 40, center.Width, 24), 18, Theme.TextDim);
        var qBox = new Rectangle(center.X, center.Y + 80, center.Width, 120);
        Ui.Panel(qBox);
        Ui.TextCentered(round.Prompt.Question, qBox, 32, Theme.Text, true);

        if (_feedback <= 0)
        {
            _timer -= dt;
            Ui.ProgressBar(new Rectangle(center.X, qBox.Bottom + 20, center.Width, 12), _timer / RoundTime,
                _timer < 6 ? Theme.Red : Info.Color);
            Ui.TextRight($"{Math.Max(0, _timer):0.0} s", center.Right, qBox.Bottom + 38, 16, Theme.TextDim);
            if (!_input.IsFocused) _input.Focus();
            bool enter = _input.Frame(new Rectangle(center.X, qBox.Bottom + 70, center.Width - 150, 56), 24);
            bool click = Ui.Button(new Rectangle(center.Right - 140, qBox.Bottom + 70, 140, 56), "Odeslat", Info.Color, Theme.Bg, 20);
            if (enter || click || _timer <= 0) SubmitRound();
        }
        else
        {
            _feedback -= dt;
            var fb = new Rectangle(center.X, qBox.Bottom + 40, center.Width, 150);
            if (round.Tier < 0)
            {
                Ui.Panel(fb, Theme.Panel2);
                Ui.TextCentered(string.IsNullOrWhiteSpace(round.Answer) ? "Bez odpovědi" : $"„{round.Answer}“ – tuhle odpověď neznám",
                    new Rectangle(fb.X, fb.Y + 30, fb.Width, 40), 26, Theme.TextDim, true);
                Ui.TextCentered("0 bodů", new Rectangle(fb.X, fb.Y + 85, fb.Width, 30), 22, Theme.TextDim);
            }
            else
            {
                Ui.Panel(fb, TierColors[round.Tier] * 0.9f);
                Ui.TextCentered($"{round.Matched}  –  {TierNames[round.Tier]}", new Rectangle(fb.X, fb.Y + 30, fb.Width, 40), 28, Theme.Bg, true);
                Ui.TextCentered($"+{round.Points} bodů", new Rectangle(fb.X, fb.Y + 85, fb.Width, 30), 24, Theme.Bg, true);
            }
            if (_feedback <= 0 || Ui.Clicked(fb))
            {
                if (_index + 1 >= Rounds) Complete();
                else StartRound();
            }
        }
    }

    void DrawSummary(Rectangle center)
    {
        int y = center.Y + 50;
        Ui.TextCentered($"Celkem {Total} bodů", new Rectangle(center.X, y, center.Width, 40), 34, Theme.Text, true);
        y += 60;
        foreach (var r in _rounds)
        {
            var row = new Rectangle(center.X, y, center.Width, 56);
            Ui.Panel(row, Theme.Panel, 8);
            Ui.Text(r.Prompt.Question, row.X + 16, row.Y + 8, 16, Theme.TextDim);
            string ans = string.IsNullOrWhiteSpace(r.Answer) ? "—" : r.Answer;
            Ui.Text(ans, row.X + 16, row.Y + 28, 19, Theme.Text, true);
            if (r.Tier >= 0)
            {
                var badge = new Rectangle(row.Right - 260, row.Y + 12, 160, 32);
                Ui.RoundRect(badge, TierColors[r.Tier], 6);
                Ui.TextCentered(TierNames[r.Tier], badge, 15, Theme.Bg, true);
            }
            Ui.TextRight($"{r.Points} b", row.Right - 16, row.Y + 16, 22, r.Tier >= 0 ? Theme.Text : Theme.TextDim, true);
            if (Ui.Hover(row))
            {
                var top = string.Join(", ", r.Prompt.Answers.Take(3).Select(a => a.Split('|')[0]));
                var rare = string.Join(", ", r.Prompt.Answers.TakeLast(3).Select(a => a.Split('|')[0]));
                Ui.Tooltip = $"Nejčastější: {top}   |   Nejvzácnější: {rare}";
            }
            y += 64;
        }
    }
}
