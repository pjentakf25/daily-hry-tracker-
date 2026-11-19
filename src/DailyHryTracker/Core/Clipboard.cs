using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DailyHryTracker.Core;

/// <summary>Schránka přes systémové nástroje (MonoGame vlastní API nemá).</summary>
public static class Clipboard
{
    public static bool Set(string text)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return Run("pbcopy", "", text) != null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Run("powershell", "-NoProfile -Command \"$input | Set-Clipboard\"", text) != null;
            return Run("wl-copy", "", text) != null || Run("xclip", "-selection clipboard", text) != null;
        }
        catch { return false; }
    }

    public static string Get()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return Run("pbpaste", "", null);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Run("powershell", "-NoProfile -Command Get-Clipboard", null);
            return Run("wl-paste", "-n", null) ?? Run("xclip", "-selection clipboard -o", null);
        }
        catch { return null; }
    }

    static string Run(string exe, string args, string stdin)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardInput = stdin != null,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };
            using var p = Process.Start(psi);
            if (p == null) return null;
            if (stdin != null)
            {
                p.StandardInput.Write(stdin);
                p.StandardInput.Close();
            }
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);
            return p.ExitCode == 0 ? output : null;
        }
        catch { return null; }
    }
}
