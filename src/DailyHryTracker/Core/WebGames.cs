using System.Diagnostics;
using DailyHryTracker.Data;
using DailyHryTracker.Games;
using Photino.NET;

namespace DailyHryTracker;

/// <summary>
/// Otevírá originální web denní hry ve vlastním okně aplikace (vestavěný WebKit / WebView2),
/// ne v Safari ani Chrome. Okno běží jako podproces stejné aplikace, protože MonoGame i webové
/// okno potřebují hlavní vlákno.
/// </summary>
public static class WebGames
{
    public const string Arg = "--web";

    sealed class Running
    {
        public Process Process;
        public GameInfo Game;
    }

    static readonly List<Running> _running = new();
    static readonly Queue<GameInfo> _closed = new();
    static readonly object _lock = new();

    public static bool IsOpen(string gameId)
    {
        lock (_lock) return _running.Any(r => r.Game.Id == gameId);
    }

    /// <summary>Hry, jejichž okno se mezitím zavřelo (aby se aplikace mohla zeptat, jestli je hráč dohrál).</summary>
    public static GameInfo PollClosed()
    {
        lock (_lock) return _closed.Count > 0 ? _closed.Dequeue() : null;
    }

    public static string LastError { get; private set; }

    public static bool Open(GameInfo g)
    {
        if (IsOpen(g.Id)) return true;
        try
        {
            var psi = new ProcessStartInfo { UseShellExecute = false };
            string exe = Environment.ProcessPath ?? "dotnet";
            psi.FileName = exe;
            // spuštěno přes "dotnet DailyHryTracker.dll" → musíme předat i cestu k dll
            if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                psi.ArgumentList.Add(typeof(WebGames).Assembly.Location);
            psi.ArgumentList.Add(Arg);
            psi.ArgumentList.Add(g.Id);

            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var run = new Running { Process = p, Game = g };
            p.Exited += (_, _) =>
            {
                lock (_lock)
                {
                    _running.Remove(run);
                    _closed.Enqueue(g);
                }
            };
            lock (_lock) _running.Add(run);
            if (!p.Start())
            {
                lock (_lock) _running.Remove(run);
                LastError = "Okno hry se nepodařilo otevřít";
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            LastError = "Okno hry se nepodařilo otevřít: " + e.Message;
            return false;
        }
    }

    /// <summary>Při zavření hlavní aplikace zavře i otevřená okna her.</summary>
    public static void CloseAll()
    {
        lock (_lock)
            foreach (var r in _running)
                try { if (!r.Process.HasExited) r.Process.Kill(); } catch { /* už neběží */ }
    }

    // ------------------------- strana podprocesu -------------------------

    public static void RunWindow(string gameId)
    {
        var g = GameCatalog.All.FirstOrDefault(x => x.Id == gameId);
        if (g == null) return;

        var dataDir = Path.Combine(SaveStore.Folder, "Web");
        Directory.CreateDirectory(dataDir);

        var w = new PhotinoWindow()
            .SetLogVerbosity(0)
            .SetTitle($"{g.Name} – Daily Hry Tracker")
            .SetUseOsDefaultSize(false)
            .SetSize(1200, 860)
            .Center()
            .SetResizable(true)
            .SetDevToolsEnabled(false)
            .SetContextMenuEnabled(true)
            .SetTemporaryFilesPath(dataDir)
            .SetJavascriptClipboardAccessEnabled(true)
            .SetMediaAutoplayEnabled(true);

        // WKWebView na macOS se jinak hlásí bez značky Safari a některé weby pak zlobí
        if (OperatingSystem.IsMacOS())
            w.SetUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Safari/605.1.15");

        var icon = Path.Combine(AppContext.BaseDirectory, "Icon.ico");
        if (OperatingSystem.IsWindows() && File.Exists(icon)) w.SetIconFile(icon);

        w.Load(new Uri(g.Url));
        w.WaitForClose();
    }
}
